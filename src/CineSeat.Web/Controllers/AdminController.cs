using CineSeat.Web.Exceptions;
using CineSeat.Web.Models;
using CineSeat.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineSeat.Web.Controllers
{
    [Authorize(Roles = "ADMIN")]
    public class AdminController : Controller
    {
        private readonly ICineSeatService _cineSeatService;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ICineSeatService cineSeatService, 
            IWebHostEnvironment env,
            ILogger<AdminController> logger)
        {
            _cineSeatService = cineSeatService;
            _env = env;
            _logger = logger;
        }

        // =====================================================================
        // DASHBOARD
        // =====================================================================

        // GET: /Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var stats = await _cineSeatService.GetAdminDashboardStatsAsync();
            return View(stats);
        }

        // =====================================================================
        // QUẢN LÝ PHIM (MOVIES - CRUD + UPLOAD POSTER)
        // =====================================================================

        // GET: /Admin/Movies
        public async Task<IActionResult> Movies()
        {
            var movies = await _cineSeatService.GetAllMoviesAdminAsync();
            var vm = new AdminMoviesViewModel { Movies = movies };
            return View(vm);
        }

        // POST: /Admin/AddMovie
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMovie(CreateMovieInputModel input)
        {
            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                TempData["ErrorMessage"] = firstError ?? "Dữ liệu nhập không hợp lệ.";
                return RedirectToAction(nameof(Movies));
            }

            try
            {
                // Xử lý upload ảnh poster nếu có
                string? savedFileName = null;
                if (input.PosterUpload != null && input.PosterUpload.Length > 0)
                {
                    savedFileName = await ProcessPosterUploadAsync(input.PosterUpload, input.Title);
                    if (savedFileName == null)
                    {
                        return RedirectToAction(nameof(Movies));
                    }
                    input.PosterFile = savedFileName;
                }

                int newId = await _cineSeatService.AddMovieAsync(input);
                TempData["SuccessMessage"] = $"Đã thêm phim mới [{input.Title}] thành công (Mã phim: #{newId}).";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thêm phim mới");
                TempData["ErrorMessage"] = "Không thể thêm phim mới. Vui lòng kiểm tra lại.";
            }

            return RedirectToAction(nameof(Movies));
        }

        // POST: /Admin/EditMovie
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMovie(UpdateMovieInputModel input)
        {
            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                TempData["ErrorMessage"] = firstError ?? "Dữ liệu cập nhật phim không hợp lệ.";
                return RedirectToAction(nameof(Movies));
            }

            try
            {
                // Kiểm tra xem phim đã công chiếu chưa để tránh lỗi dữ liệu lịch chiếu và vé
                bool hasPremiered = await _cineSeatService.HasMoviePremieredAsync(input.MovieId);
                if (hasPremiered)
                {
                    TempData["ErrorMessage"] = $"Không thể chỉnh sửa phim [{input.Title}] vì phim đã công chiếu (đã có suất chiếu trong hệ thống) nhằm bảo đảm an toàn dữ liệu.";
                    return RedirectToAction(nameof(Movies));
                }

                // Nếu có upload ảnh mới từ thiết bị
                if (input.PosterUpload != null && input.PosterUpload.Length > 0)
                {
                    string? savedFileName = await ProcessPosterUploadAsync(input.PosterUpload, input.Title, input.MovieId);
                    if (savedFileName == null)
                    {
                        return RedirectToAction(nameof(Movies));
                    }
                    input.PosterFile = savedFileName;
                }

                await _cineSeatService.UpdateMovieAsync(input);
                TempData["SuccessMessage"] = $"Đã cập nhật thông tin phim [{input.Title}] thành công.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật phim #{Id}", input.MovieId);
                TempData["ErrorMessage"] = "Không thể cập nhật phim. Vui lòng thử lại.";
            }

            return RedirectToAction(nameof(Movies));
        }

        // POST: /Admin/DeleteMovie/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMovie(int id)
        {
            try
            {
                bool hardDeleted = await _cineSeatService.DeleteMovieAsync(id);
                if (hardDeleted)
                {
                    TempData["SuccessMessage"] = $"Đã xóa vĩnh viễn phim #{id} khỏi hệ thống do chưa có suất chiếu nào.";
                }
                else
                {
                    TempData["SuccessMessage"] = $"Phim #{id} đã có suất chiếu trong hệ thống nên đã được tự động chuyển sang trạng thái [NGỪNG CHIẾU] an toàn.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa phim #{Id}", id);
                TempData["ErrorMessage"] = "Không thể xóa phim. Vui lòng thử lại.";
            }

            return RedirectToAction(nameof(Movies));
        }

        // POST: /Admin/ToggleMovie/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleMovie(int id)
        {
            try
            {
                await _cineSeatService.ToggleMovieActiveAsync(id);
                TempData["SuccessMessage"] = $"Đã đổi trạng thái hoạt động của phim #{id}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đổi trạng thái phim #{Id}", id);
                TempData["ErrorMessage"] = "Không thể cập nhật trạng thái phim.";
            }

            return RedirectToAction(nameof(Movies));
        }

        // =====================================================================
        // QUẢN LÝ SUẤT CHIẾU (SHOWTIMES - CRUD + OVERLAP VALIDATION)
        // =====================================================================

        // GET: /Admin/Showtimes
        public async Task<IActionResult> Showtimes()
        {
            var showtimes = await _cineSeatService.GetAllShowtimesAdminAsync();
            var activeMovies = await _cineSeatService.GetMoviesAsync();
            var availableRooms = await _cineSeatService.GetRoomsAsync();

            var vm = new AdminShowtimesViewModel
            {
                Showtimes = showtimes,
                ActiveMovies = activeMovies,
                AvailableRooms = availableRooms
            };

            return View(vm);
        }

        // POST: /Admin/CreateShowtime
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateShowtime(CreateShowtimeInputModel input)
        {
            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                TempData["ErrorMessage"] = firstError ?? "Thông tin suất chiếu không hợp lệ.";
                return RedirectToAction(nameof(Showtimes));
            }

            if (input.StartAt <= DateTime.Now)
            {
                TempData["ErrorMessage"] = "Thời gian bắt đầu suất chiếu phải ở tương lai.";
                return RedirectToAction(nameof(Showtimes));
            }

            // Tự động tính EndAt nếu chưa điền hoặc EndAt <= StartAt
            if (input.EndAt <= input.StartAt)
            {
                var movie = await _cineSeatService.GetMovieByIdAsync(input.MovieId);
                int duration = movie?.DurationMin ?? 120;
                input.EndAt = input.StartAt.AddMinutes(duration + 15); // Cộng 15 phút dọn rạp
            }

            // KIỂM TRA TRÙNG LỊCH CHIẾU CÙNG PHÒNG
            bool isOverlap = await _cineSeatService.CheckShowtimeOverlapAsync(input.RoomId, input.StartAt, input.EndAt);
            if (isOverlap)
            {
                TempData["ErrorMessage"] = "Phòng chiếu này đã có suất chiếu khác trong khung giờ đã chọn. Vui lòng chọn khung giờ khác hoặc phòng chiếu khác.";
                return RedirectToAction(nameof(Showtimes));
            }

            try
            {
                int newShowtimeId = await _cineSeatService.CreateShowtimeAsync(input);
                TempData["SuccessMessage"] = $"Tạo suất chiếu #{newShowtimeId} thành công! Đã tự động tạo 40 ghế cho suất chiếu.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo suất chiếu");
                TempData["ErrorMessage"] = "Không thể tạo suất chiếu mới. Vui lòng kiểm tra lại.";
            }

            return RedirectToAction(nameof(Showtimes));
        }

        // POST: /Admin/EditShowtime
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditShowtime(UpdateShowtimeInputModel input)
        {
            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                TempData["ErrorMessage"] = firstError ?? "Thông tin cập nhật suất chiếu không hợp lệ.";
                return RedirectToAction(nameof(Showtimes));
            }

            if (input.EndAt <= input.StartAt)
            {
                TempData["ErrorMessage"] = "Thời gian kết thúc phải lớn hơn thời gian bắt đầu.";
                return RedirectToAction(nameof(Showtimes));
            }

            var currentSt = await _cineSeatService.GetShowtimeByIdAsync(input.ShowtimeId);
            if (currentSt == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy suất chiếu cần sửa.";
                return RedirectToAction(nameof(Showtimes));
            }

            // KIỂM TRA TRÙNG LỊCH CHIẾU
            bool isOverlap = await _cineSeatService.CheckShowtimeOverlapAsync(currentSt.RoomId, input.StartAt, input.EndAt, input.ShowtimeId);
            if (isOverlap)
            {
                TempData["ErrorMessage"] = "Phòng chiếu này đã có suất chiếu khác trong khung giờ đã chọn.";
                return RedirectToAction(nameof(Showtimes));
            }

            try
            {
                await _cineSeatService.UpdateShowtimeAsync(input);
                TempData["SuccessMessage"] = $"Đã cập nhật thông tin suất chiếu #{input.ShowtimeId} thành công.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi sửa suất chiếu #{Id}", input.ShowtimeId);
                TempData["ErrorMessage"] = "Không thể cập nhật suất chiếu.";
            }

            return RedirectToAction(nameof(Showtimes));
        }

        // POST: /Admin/DeleteShowtime/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteShowtime(int id)
        {
            try
            {
                await _cineSeatService.DeleteShowtimeAsync(id);
                TempData["SuccessMessage"] = $"Đã xóa suất chiếu #{id} thành công.";
            }
            catch (InvalidOperationException iex)
            {
                TempData["ErrorMessage"] = iex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa suất chiếu #{Id}", id);
                TempData["ErrorMessage"] = "Không thể xóa suất chiếu này.";
            }

            return RedirectToAction(nameof(Showtimes));
        }

        // =====================================================================
        // QUẢN LÝ CỤM RẠP & PHÒNG CHIẾU (CINEMAS & ROOMS - FULL CRUD)
        // =====================================================================

        // GET: /Admin/Cinemas
        public async Task<IActionResult> Cinemas()
        {
            var cinemas = await _cineSeatService.GetCinemasAsync();
            var rooms = await _cineSeatService.GetRoomsAsync();

            var vm = new AdminCinemasViewModel
            {
                Cinemas = cinemas,
                Rooms = rooms
            };

            return View(vm);
        }

        // POST: /Admin/AddCinema
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCinema(CreateCinemaInputModel input)
        {
            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                TempData["ErrorMessage"] = firstError ?? "Dữ liệu cụm rạp không hợp lệ.";
                return RedirectToAction(nameof(Cinemas));
            }

            try
            {
                int newId = await _cineSeatService.AddCinemaAsync(input);
                TempData["SuccessMessage"] = $"Đã thêm cụm rạp [{input.CinemaName}] thành công (Mã rạp: #{newId}).";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thêm cụm rạp");
                TempData["ErrorMessage"] = "Không thể thêm cụm rạp. Vui lòng thử lại.";
            }

            return RedirectToAction(nameof(Cinemas));
        }

        // POST: /Admin/EditCinema
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCinema(UpdateCinemaInputModel input)
        {
            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                TempData["ErrorMessage"] = firstError ?? "Dữ liệu cập nhật cụm rạp không hợp lệ.";
                return RedirectToAction(nameof(Cinemas));
            }

            try
            {
                await _cineSeatService.UpdateCinemaAsync(input);
                TempData["SuccessMessage"] = $"Đã cập nhật cụm rạp [{input.CinemaName}] thành công.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi sửa cụm rạp #{Id}", input.CinemaId);
                TempData["ErrorMessage"] = "Không thể cập nhật cụm rạp.";
            }

            return RedirectToAction(nameof(Cinemas));
        }

        // POST: /Admin/DeleteCinema/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCinema(int id)
        {
            try
            {
                await _cineSeatService.DeleteCinemaAsync(id);
                TempData["SuccessMessage"] = $"Đã xóa cụm rạp #{id} thành công.";
            }
            catch (InvalidOperationException iex)
            {
                TempData["ErrorMessage"] = iex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa cụm rạp #{Id}", id);
                TempData["ErrorMessage"] = "Không thể xóa cụm rạp.";
            }

            return RedirectToAction(nameof(Cinemas));
        }

        // POST: /Admin/AddRoom
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddRoom(CreateRoomInputModel input)
        {
            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                TempData["ErrorMessage"] = firstError ?? "Dữ liệu phòng chiếu không hợp lệ.";
                return RedirectToAction(nameof(Cinemas));
            }

            try
            {
                int newRoomId = await _cineSeatService.AddRoomAsync(input);
                TempData["SuccessMessage"] = $"Đã thêm phòng chiếu [{input.RoomName}] và tự động khởi tạo 40 ghế vật lý (A-E) thành công!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thêm phòng chiếu");
                TempData["ErrorMessage"] = "Không thể thêm phòng chiếu. Có thể tên phòng đã trùng lặp trong rạp này.";
            }

            return RedirectToAction(nameof(Cinemas));
        }

        // POST: /Admin/DeleteRoom/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRoom(int id)
        {
            try
            {
                await _cineSeatService.DeleteRoomAsync(id);
                TempData["SuccessMessage"] = $"Đã xóa phòng chiếu #{id} và các ghế vật lý thành công.";
            }
            catch (InvalidOperationException iex)
            {
                TempData["ErrorMessage"] = iex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa phòng chiếu #{Id}", id);
                TempData["ErrorMessage"] = "Không thể xóa phòng chiếu.";
            }

            return RedirectToAction(nameof(Cinemas));
        }

        // =====================================================================
        // QUẢN LÝ MÃ KHUYẾN MÃI (PROMOTIONS - FULL CRUD)
        // =====================================================================

        // GET: /Admin/Promotions
        public async Task<IActionResult> Promotions()
        {
            var promotions = await _cineSeatService.GetAllPromotionsAdminAsync();
            var vm = new AdminPromotionsViewModel { Promotions = promotions };
            return View(vm);
        }

        // POST: /Admin/AddPromotion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPromotion(CreatePromotionInputModel input)
        {
            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                TempData["ErrorMessage"] = firstError ?? "Dữ liệu mã khuyến mãi không hợp lệ.";
                return RedirectToAction(nameof(Promotions));
            }

            if (input.ValidUntil < DateTime.Today)
            {
                TempData["ErrorMessage"] = "Ngày hết hạn không được nhỏ hơn ngày hiện tại.";
                return RedirectToAction(nameof(Promotions));
            }

            try
            {
                input.PromoCode = input.PromoCode.Trim().ToUpperInvariant();
                int newId = await _cineSeatService.AddPromotionAsync(input);
                TempData["SuccessMessage"] = $"Đã thêm mã khuyến mãi [{input.PromoCode}] (Giảm {input.DiscountPct}%) thành công!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thêm mã khuyến mãi");
                TempData["ErrorMessage"] = "Không thể thêm mã khuyến mãi. Có thể mã đã tồn tại.";
            }

            return RedirectToAction(nameof(Promotions));
        }

        // POST: /Admin/EditPromotion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPromotion(UpdatePromotionInputModel input)
        {
            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                TempData["ErrorMessage"] = firstError ?? "Dữ liệu cập nhật khuyến mãi không hợp lệ.";
                return RedirectToAction(nameof(Promotions));
            }

            try
            {
                input.PromoCode = input.PromoCode.Trim().ToUpperInvariant();
                await _cineSeatService.UpdatePromotionAsync(input);
                TempData["SuccessMessage"] = $"Đã cập nhật mã khuyến mãi [{input.PromoCode}] thành công.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi sửa mã khuyến mãi #{Id}", input.PromotionId);
                TempData["ErrorMessage"] = "Không thể cập nhật mã khuyến mãi.";
            }

            return RedirectToAction(nameof(Promotions));
        }

        // POST: /Admin/TogglePromotion/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePromotion(int id)
        {
            try
            {
                await _cineSeatService.TogglePromotionActiveAsync(id);
                TempData["SuccessMessage"] = $"Đã thay đổi trạng thái áp dụng của mã khuyến mãi #{id}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đổi trạng thái khuyến mãi #{Id}", id);
                TempData["ErrorMessage"] = "Không thể cập nhật trạng thái khuyến mãi.";
            }

            return RedirectToAction(nameof(Promotions));
        }

        // POST: /Admin/DeletePromotion/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePromotion(int id)
        {
            try
            {
                await _cineSeatService.DeletePromotionAsync(id);
                TempData["SuccessMessage"] = $"Đã xử lý xóa / vô hiệu hóa mã khuyến mãi #{id} thành công.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa mã khuyến mãi #{Id}", id);
                TempData["ErrorMessage"] = "Không thể xóa mã khuyến mãi.";
            }

            return RedirectToAction(nameof(Promotions));
        }

        // =====================================================================
        // QUẢN LÝ ĐƠN ĐẶT VÉ (BOOKINGS)
        // =====================================================================

        // GET: /Admin/Bookings
        public async Task<IActionResult> Bookings(string? status)
        {
            var statusFilter = string.IsNullOrWhiteSpace(status) ? "ALL" : status.Trim().ToUpper();
            var bookings = await _cineSeatService.GetAllBookingsAsync(statusFilter);

            var vm = new AdminBookingsViewModel
            {
                Bookings = bookings,
                StatusFilter = statusFilter
            };

            return View(vm);
        }

        // POST: /Admin/TriggerExpire
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TriggerExpire(string? returnUrl)
        {
            try
            {
                await _cineSeatService.ExpireHoldsAsync();
                TempData["SuccessMessage"] = "Đã quét và giải phóng toàn bộ các ghế giữ quá hạn 5 phút thành công!";
            }
            catch (CineSeatException cex)
            {
                TempData["ErrorMessage"] = cex.FriendlyMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi chạy quét giải phóng ghế quá hạn");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi thực hiện giải phóng ghế.";
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Dashboard));
        }

        // =====================================================================
        // HELPER: XỬ LÝ UPLOAD ẢNH POSTER TỪ THIẾT BỊ
        // =====================================================================
        private async Task<string?> ProcessPosterUploadAsync(IFormFile file, string movieTitle, int movieId = 0)
        {
            // Kiểm tra dung lượng (tối đa 5MB)
            if (file.Length > 5 * 1024 * 1024)
            {
                TempData["ErrorMessage"] = "Dung lượng ảnh tải lên vượt quá giới hạn 5MB.";
                return null;
            }

            // Kiểm tra phần mở rộng
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowedExtensions.Contains(ext))
            {
                TempData["ErrorMessage"] = "Định dạng file không hỗ trợ. Vui lòng chọn ảnh định dạng .jpg, .jpeg, .png hoặc .webp.";
                return null;
            }

            try
            {
                string imagesFolder = Path.Combine(_env.WebRootPath, "images");
                if (!Directory.Exists(imagesFolder))
                {
                    Directory.CreateDirectory(imagesFolder);
                }

                // Tên file lưu: clean movie title hoặc poster-{id}
                string cleanTitle = movieTitle.Replace(":", "").Replace("/", " ").Replace("\\", " ").Replace("?", "").Replace("*", "").Trim();
                string targetFileName = movieId > 0 
                    ? $"poster-{movieId}{ext}" 
                    : $"{cleanTitle}{ext}";

                string fullPath = Path.Combine(imagesFolder, targetFileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                _logger.LogInformation("Upload ảnh poster thành công: {FileName}", targetFileName);
                return targetFileName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lưu ảnh poster upload");
                TempData["ErrorMessage"] = "Không thể lưu file ảnh tải lên.";
                return null;
            }
        }
    }
}
