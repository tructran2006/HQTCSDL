using CineSeat.Web.Exceptions;
using CineSeat.Web.Models;
using CineSeat.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace CineSeat.Web.Controllers
{
    public class BookingsController : Controller
    {
        private readonly ICineSeatService _cineSeatService;
        private readonly IUserContext _userContext;
        private readonly ILogger<BookingsController> _logger;

        public BookingsController(ICineSeatService cineSeatService, IUserContext userContext, ILogger<BookingsController> logger)
        {
            _cineSeatService = cineSeatService;
            _userContext = userContext;
            _logger = logger;
        }

        // GET: Bookings/SelectSeats/{showtimeId}
        public async Task<IActionResult> SelectSeats(int id)
        {
            var showtime = await _cineSeatService.GetShowtimeByIdAsync(id);
            if (showtime == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy suất chiếu.";
                return RedirectToAction("Index", "Movies");
            }

            var movie = await _cineSeatService.GetMovieByIdAsync(showtime.MovieId);
            var seats = await _cineSeatService.GetShowtimeSeatsAsync(id);
            var promotions = await _cineSeatService.GetActivePromotionsAsync();
            var currentUser = await _userContext.GetCurrentUserAsync();

            var viewModel = new SeatMapViewModel
            {
                Showtime = showtime,
                Movie = movie ?? new Movie { Title = showtime.MovieTitle },
                Seats = seats,
                AvailablePromotions = promotions,
                CurrentUserId = currentUser.UserId,
                CurrentUserName = currentUser.FullName
            };

            return View(viewModel);
        }

        // POST: Bookings/CalculatePrice (AJAX)
        [HttpPost]
        public async Task<IActionResult> CalculatePrice([FromBody] PriceCalcRequest request)
        {
            if (request.SeatCount < 1 || request.SeatCount > 8)
            {
                return Json(new PriceCalcResult
                {
                    Success = false,
                    ErrorCode = 20001,
                    Message = "Mỗi đơn chỉ được đặt từ 1 đến 8 ghế (ORA-20001)."
                });
            }

            try
            {
                decimal total = await _cineSeatService.CalculateTotalPriceAsync(
                    request.ShowtimeId, request.SeatCount, request.PromoCode);

                return Json(new PriceCalcResult
                {
                    Success = true,
                    TotalPrice = total,
                    FormattedPrice = total.ToString("#,##0") + " ₫",
                    Message = "Tính giá vé thành công!"
                });
            }
            catch (CineSeatException cex)
            {
                return Json(new PriceCalcResult
                {
                    Success = false,
                    ErrorCode = cex.ErrorCode,
                    Message = cex.FriendlyMessage
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tính giá vé");
                return Json(new PriceCalcResult
                {
                    Success = false,
                    ErrorCode = 500,
                    Message = "Đã xảy ra lỗi trong quá trình tính giá vé."
                });
            }
        }

        // POST: Bookings/Hold
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Hold(HoldSeatsRequest request)
        {
            int currentUser = _userContext.CurrentUserId;

            if (string.IsNullOrWhiteSpace(request.SeatIdsJson) || request.SeatIdsJson == "[]")
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất 1 ghế trước khi tiếp tục.";
                return RedirectToAction(nameof(SelectSeats), new { id = request.ShowtimeId });
            }

            try
            {
                var (bookingId, totalAmount, expiresAt) = await _cineSeatService.HoldSeatsAsync(
                    currentUser, request.ShowtimeId, request.SeatIdsJson, request.PromoCode);

                TempData["SuccessMessage"] = $"Giữ ghế thành công! Vui lòng thanh toán trong vòng 5 phút (hạn chót: {expiresAt:HH:mm:ss}).";
                return RedirectToAction(nameof(Checkout), new { id = bookingId });
            }
            catch (CineSeatException cex)
            {
                TempData["ErrorMessage"] = cex.FriendlyMessage;
                return RedirectToAction(nameof(SelectSeats), new { id = request.ShowtimeId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi giữ ghế");
                TempData["ErrorMessage"] = "Có lỗi xảy ra trong quá trình giữ ghế. Vui lòng thử lại.";
                return RedirectToAction(nameof(SelectSeats), new { id = request.ShowtimeId });
            }
        }

        // GET: Bookings/Checkout/{bookingId}
        public async Task<IActionResult> Checkout(int id)
        {
            var booking = await _cineSeatService.GetBookingByIdAsync(id);
            if (booking == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("Index", "Movies");
            }

            var showtime = await _cineSeatService.GetShowtimeByIdAsync(booking.ShowtimeId);
            var movie = await _cineSeatService.GetMovieByIdAsync(showtime?.MovieId ?? 0);

            var viewModel = new CheckoutViewModel
            {
                Booking = booking,
                Showtime = showtime ?? new Showtime(),
                Movie = movie ?? new Movie { Title = booking.MovieTitle }
            };

            return View(viewModel);
        }

        // POST: Bookings/ConfirmPayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment(int bookingId, string method)
        {
            try
            {
                await _cineSeatService.ConfirmBookingAsync(bookingId, method);
                TempData["SuccessMessage"] = $"Thanh toán mô phỏng thành công bằng phương thức [{method}]! Vé của bạn đã được xuất.";
                return RedirectToAction(nameof(Detail), new { id = bookingId });
            }
            catch (CineSeatException cex)
            {
                TempData["ErrorMessage"] = cex.FriendlyMessage;
                return RedirectToAction(nameof(Checkout), new { id = bookingId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi thanh toán");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi trong quá trình thanh toán.";
                return RedirectToAction(nameof(Checkout), new { id = bookingId });
            }
        }

        // POST: Bookings/Cancel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(int bookingId)
        {
            try
            {
                await _cineSeatService.CancelBookingAsync(bookingId);
                TempData["SuccessMessage"] = "Đã hủy giữ ghế thành công! Ghế đã được hoàn trả lại về trạng thái trống (AVAILABLE).";
                return RedirectToAction(nameof(History));
            }
            catch (CineSeatException cex)
            {
                TempData["ErrorMessage"] = cex.FriendlyMessage;
                return RedirectToAction(nameof(Detail), new { id = bookingId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi hủy đơn giữ ghế");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi hủy giữ ghế.";
                return RedirectToAction(nameof(History));
            }
        }

        // GET: Bookings/History
        public async Task<IActionResult> History()
        {
            var currentUser = await _userContext.GetCurrentUserAsync();
            var bookings = await _cineSeatService.GetBookingHistoryByUserIdAsync(currentUser.UserId);

            ViewBag.CurrentUser = currentUser;
            return View(bookings);
        }

        // GET: Bookings/Detail/{bookingId}
        public async Task<IActionResult> Detail(int id)
        {
            var booking = await _cineSeatService.GetBookingByIdAsync(id);
            if (booking == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin đơn hàng.";
                return RedirectToAction(nameof(History));
            }

            var showtime = await _cineSeatService.GetShowtimeByIdAsync(booking.ShowtimeId);
            var movie = await _cineSeatService.GetMovieByIdAsync(showtime?.MovieId ?? 0);

            ViewBag.Showtime = showtime;
            ViewBag.Movie = movie;

            return View(booking);
        }
    }
}
