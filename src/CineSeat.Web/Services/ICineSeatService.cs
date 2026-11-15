using CineSeat.Web.Models;

namespace CineSeat.Web.Services
{
    public interface ICineSeatService
    {
        // Phim & Rạp & Suất chiếu
        Task<List<Movie>> GetMoviesAsync(string? search = null, string? genre = null, string? ageRating = null);
        Task<Movie?> GetMovieByIdAsync(int movieId);
        Task<List<Showtime>> GetShowtimesByMovieIdAsync(int movieId);
        Task<Showtime?> GetShowtimeByIdAsync(int showtimeId);
        Task<List<ShowtimeSeat>> GetShowtimeSeatsAsync(int showtimeId);
        Task<List<Cinema>> GetCinemasAsync();
        Task<List<Room>> GetRoomsAsync();
        Task<List<Showtime>> GetShowtimesByCinemaIdAsync(int cinemaId);

        // Người dùng & Khuyến mãi
        Task<List<AppUser>> GetUsersAsync();
        Task<AppUser?> GetUserByIdAsync(int userId);
        Task<AppUser?> GetUserByEmailAsync(string email);
        Task<AppUser> RegisterUserAsync(string fullName, string email, string phone);
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
        Task<List<Booking>> GetAllBookingsAsync(string? statusFilter = null);
        Task<AdminDashboardViewModel> GetAdminDashboardStatsAsync();

        // Quản trị Admin
        Task<List<Movie>> GetAllMoviesAdminAsync();
        Task<List<Showtime>> GetAllShowtimesAdminAsync();
        Task<int> AddMovieAsync(CreateMovieInputModel input);
        Task ToggleMovieActiveAsync(int movieId);
        Task<int> CreateShowtimeAsync(CreateShowtimeInputModel input);
    }
}
