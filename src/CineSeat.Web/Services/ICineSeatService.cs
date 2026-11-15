using CineSeat.Web.Models;

namespace CineSeat.Web.Services
{
    public interface ICineSeatService
    {
        // Phim & Suất chiếu
        Task<List<Movie>> GetMoviesAsync(string? search = null, string? genre = null);
        Task<Movie?> GetMovieByIdAsync(int movieId);
        Task<List<Showtime>> GetShowtimesByMovieIdAsync(int movieId);
        Task<Showtime?> GetShowtimeByIdAsync(int showtimeId);
        Task<List<ShowtimeSeat>> GetShowtimeSeatsAsync(int showtimeId);

        // Người dùng & Khuyến mãi
        Task<List<AppUser>> GetUsersAsync();
        Task<AppUser?> GetUserByIdAsync(int userId);
        Task<List<Promotion>> GetActivePromotionsAsync();

        // Nghiệp vụ PL/SQL PKG_BOOKING & PR_EXPIRE_HOLDS
        Task<decimal> CalculateTotalPriceAsync(int showtimeId, int seatCount, string? promoCode);
        Task<(int bookingId, decimal totalAmount, DateTime expiresAt)> HoldSeatsAsync(int userId, int showtimeId, string seatIdsJson, string? promoCode);
        Task ConfirmBookingAsync(int bookingId, string method);
        Task CancelBookingAsync(int bookingId);
        Task ExpireHoldsAsync();

        // Đơn hàng & Lịch sử & Báo cáo
        Task<Booking?> GetBookingByIdAsync(int bookingId);
        Task<List<Booking>> GetBookingHistoryByUserIdAsync(int userId);
        Task<AdminDashboardViewModel> GetAdminDashboardStatsAsync();
    }
}
