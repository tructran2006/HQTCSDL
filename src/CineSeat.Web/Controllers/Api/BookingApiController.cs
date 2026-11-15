using CineSeat.Web.Exceptions;
using CineSeat.Web.Models;
using CineSeat.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace CineSeat.Web.Controllers.Api
{
    [ApiController]
    [Route("api/bookings")]
    public class BookingApiController : ControllerBase
    {
        private readonly ICineSeatService _cineSeatService;

        public BookingApiController(ICineSeatService cineSeatService)
        {
            _cineSeatService = cineSeatService;
        }

        [HttpPost("calculate-price")]
        public async Task<IActionResult> CalculatePrice([FromBody] PriceCalcRequest request)
        {
            try
            {
                var total = await _cineSeatService.CalculateTotalPriceAsync(request.ShowtimeId, request.SeatCount, request.PromoCode);
                return Ok(new { success = true, total_price = total });
            }
            catch (CineSeatException cex)
            {
                return StatusCode(StatusCodes.Status409Conflict, new { success = false, error_code = cex.ErrorCode, message = cex.FriendlyMessage });
            }
        }

        [HttpPost("hold")]
        public async Task<IActionResult> Hold([FromBody] HoldRequestDto request)
        {
            try
            {
                var (bookingId, totalAmount, expiresAt) = await _cineSeatService.HoldSeatsAsync(
                    request.UserId, request.ShowtimeId, request.SeatIdsJson, request.PromoCode);

                return Ok(new
                {
                    success = true,
                    booking_id = bookingId,
                    total_amount = totalAmount,
                    expires_at = expiresAt
                });
            }
            catch (CineSeatException cex)
            {
                return StatusCode(StatusCodes.Status409Conflict, new
                {
                    success = false,
                    error_code = cex.ErrorCode,
                    message = cex.FriendlyMessage
                });
            }
        }

        [HttpPost("confirm")]
        public async Task<IActionResult> Confirm([FromBody] ConfirmRequestDto request)
        {
            try
            {
                await _cineSeatService.ConfirmBookingAsync(request.BookingId, request.Method ?? "DEMO_QR");
                return Ok(new { success = true, message = "Thanh toan thanh cong." });
            }
            catch (CineSeatException cex)
            {
                return StatusCode(StatusCodes.Status409Conflict, new
                {
                    success = false,
                    error_code = cex.ErrorCode,
                    message = cex.FriendlyMessage
                });
            }
        }

        [HttpPost("cancel")]
        public async Task<IActionResult> Cancel([FromBody] CancelRequestDto request)
        {
            try
            {
                await _cineSeatService.CancelBookingAsync(request.BookingId);
                return Ok(new { success = true, message = "Huy giu ghe thanh cong." });
            }
            catch (CineSeatException cex)
            {
                return StatusCode(StatusCodes.Status409Conflict, new
                {
                    success = false,
                    error_code = cex.ErrorCode,
                    message = cex.FriendlyMessage
                });
            }
        }

        [HttpPost("expire-holds")]
        public async Task<IActionResult> ExpireHolds()
        {
            try
            {
                await _cineSeatService.ExpireHoldsAsync();
                return Ok(new { success = true, message = "Da thu hoi cac don giu qua han." });
            }
            catch (CineSeatException cex)
            {
                return StatusCode(StatusCodes.Status409Conflict, new
                {
                    success = false,
                    error_code = cex.ErrorCode,
                    message = cex.FriendlyMessage
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBooking(int id)
        {
            var booking = await _cineSeatService.GetBookingByIdAsync(id);
            if (booking == null) return NotFound(new { success = false, message = "Khong tim thay don." });
            return Ok(new { success = true, data = booking });
        }

        [HttpGet("/api/showtimes/{id}/seats")]
        public async Task<IActionResult> GetShowtimeSeats(int id)
        {
            var seats = await _cineSeatService.GetShowtimeSeatsAsync(id);
            return Ok(new { success = true, data = seats });
        }
    }

    public class HoldRequestDto
    {
        public int UserId { get; set; } = 1;
        public int ShowtimeId { get; set; }
        public string SeatIdsJson { get; set; } = "[]";
        public string? PromoCode { get; set; }
    }

    public class ConfirmRequestDto
    {
        public int BookingId { get; set; }
        public string Method { get; set; } = "DEMO_QR";
    }

    public class CancelRequestDto
    {
        public int BookingId { get; set; }
    }
}
