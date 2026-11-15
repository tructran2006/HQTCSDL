using CineSeat.Web.Exceptions;
using CineSeat.Web.Models;
using CineSeat.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace CineSeat.Web.Controllers
{
    public class AdminController : Controller
    {
        private readonly ICineSeatService _cineSeatService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ICineSeatService cineSeatService, ILogger<AdminController> logger)
        {
            _cineSeatService = cineSeatService;
            _logger = logger;
        }

        // GET: /Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var stats = await _cineSeatService.GetAdminDashboardStatsAsync();
            return View(stats);
        }

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
            if (string.IsNullOrWhiteSpace(input.Title) || string.IsNullOrWhiteSpace(input.Genre) || input.DurationMin <= 0)
            {
                TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ tên phim, thể loại và thời lượng hợp lệ (> 0).";
                return RedirectToAction(nameof(Movies));
            }

            try
            {
                int newId = await _cineSeatService.AddMovieAsync(input);
                TempData["SuccessMessage"] = $"Đã thêm phim mới [{input.Title}] thành công (Mã phim: #{newId}).";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thêm phim");
                TempData["ErrorMessage"] = "Không thể thêm phim mới. Vui lòng kiểm tra lại.";
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
                TempData["SuccessMessage"] = $"Đã cập nhật trạng thái hoạt động của phim #{id}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đổi trạng thái phim");
                TempData["ErrorMessage"] = "Không thể cập nhật trạng thái phim.";
            }

            return RedirectToAction(nameof(Movies));
        }

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
            if (input.MovieId <= 0 || input.RoomId <= 0 || input.BasePrice <= 0 || input.StartAt <= DateTime.Now)
            {
                TempData["ErrorMessage"] = "Thông tin suất chiếu không hợp lệ hoặc thời gian bắt đầu phải lớn hơn thời điểm hiện tại.";
                return RedirectToAction(nameof(Showtimes));
            }

            if (input.EndAt <= input.StartAt)
            {
                input.EndAt = input.StartAt.AddMinutes(120);
            }

            try
            {
                int newShowtimeId = await _cineSeatService.CreateShowtimeAsync(input);
                TempData["SuccessMessage"] = $"Tạo suất chiếu #{newShowtimeId} thành công! Đã tự động sinh 40 ghế AVAILABLE trong bảng SHOWTIME_SEATS.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo suất chiếu");
                TempData["ErrorMessage"] = "Không thể tạo suất chiếu mới. Vui lòng kiểm tra lại thời gian hoặc phòng chiếu.";
            }

            return RedirectToAction(nameof(Showtimes));
        }

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
                TempData["SuccessMessage"] = "Đã thực thi thủ tục PR_EXPIRE_HOLDS thành công! Toàn bộ đơn giữ chỗ quá hạn 5 phút đã được chuyển thành EXPIRED và giải phóng ghế.";
            }
            catch (CineSeatException cex)
            {
                TempData["ErrorMessage"] = cex.FriendlyMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi chạy PR_EXPIRE_HOLDS");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi thực thi PR_EXPIRE_HOLDS.";
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Dashboard));
        }
    }
}
