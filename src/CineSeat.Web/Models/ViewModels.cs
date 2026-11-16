using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace CineSeat.Web.Models
{
    public class SeatMapViewModel
    {
        public Showtime Showtime { get; set; } = new();
        public Movie Movie { get; set; } = new();
        public List<ShowtimeSeat> Seats { get; set; } = new();
        public List<Promotion> AvailablePromotions { get; set; } = new();
        public int CurrentUserId { get; set; }
        public string CurrentUserName { get; set; } = string.Empty;
        public Dictionary<string, List<ShowtimeSeat>> Rows =>
            Seats.GroupBy(s => s.RowLabel)
                 .OrderBy(g => g.Key)
                 .ToDictionary(g => g.Key, g => g.OrderBy(s => s.SeatNumber).ToList());
    }

    public class PriceCalcRequest
    {
        public int ShowtimeId { get; set; }
        public int SeatCount { get; set; }
        public string? PromoCode { get; set; }
    }

    public class PriceCalcResult
    {
        public bool Success { get; set; }
        public decimal TotalPrice { get; set; }
        public string? FormattedPrice { get; set; }
        public string? Message { get; set; }
        public int ErrorCode { get; set; }
    }

    public class HoldSeatsRequest
    {
        public int ShowtimeId { get; set; }
        public string SeatIdsJson { get; set; } = "[]";
        public string? PromoCode { get; set; }
    }

    public class HoldSeatsResult
    {
        public bool Success { get; set; }
        public int BookingId { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string? ErrorMessage { get; set; }
        public int ErrorCode { get; set; }
    }

    public class CheckoutViewModel
    {
        public Booking Booking { get; set; } = new();
        public Showtime Showtime { get; set; } = new();
        public Movie Movie { get; set; } = new();
        public int RemainingSeconds => Math.Max(0, (int)(Booking.ExpiresAt - DateTime.Now).TotalSeconds);
        public bool IsExpired => RemainingSeconds <= 0;
    }

    public class MovieDetailsViewModel
    {
        public Movie Movie { get; set; } = new();
        public List<Showtime> Showtimes { get; set; } = new();
        public Dictionary<DateTime, List<Showtime>> GroupedByDate =>
            Showtimes.GroupBy(s => s.StartAt.Date)
                     .OrderBy(g => g.Key)
                     .ToDictionary(g => g.Key, g => g.OrderBy(s => s.CinemaName).ThenBy(s => s.StartAt).ToList());
    }

    public class CinemaShowtimesViewModel
    {
        public List<Cinema> Cinemas { get; set; } = new();
        public int SelectedCinemaId { get; set; }
        public Cinema? SelectedCinema { get; set; }
        public List<Showtime> Showtimes { get; set; } = new();
        public DateTime? SelectedDate { get; set; }

        public List<DateTime> AvailableDates =>
            Showtimes.Select(s => s.StartAt.Date)
                     .Distinct()
                     .OrderBy(d => d)
                     .ToList();

        public List<Showtime> FilteredShowtimes =>
            SelectedDate.HasValue
                ? Showtimes.Where(s => s.StartAt.Date == SelectedDate.Value.Date).OrderBy(s => s.StartAt).ToList()
                : Showtimes.OrderBy(s => s.StartAt).ToList();

        public Dictionary<DateTime, List<Showtime>> GroupedByDate =>
            FilteredShowtimes.GroupBy(s => s.StartAt.Date)
                             .OrderBy(g => g.Key)
                             .ToDictionary(g => g.Key, g => g.OrderBy(s => s.MovieTitle).ThenBy(s => s.StartAt).ToList());
    }

    public class MovieRevenueStat
    {
        public string MovieTitle { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int TicketsSold { get; set; }
    }

    public class AdminDashboardViewModel
    {
        public decimal TotalRevenue { get; set; }
        public int TotalTicketsSold { get; set; }
        public int PaidCount { get; set; }
        public int HeldCount { get; set; }
        public int CancelledCount { get; set; }
        public int ExpiredCount { get; set; }
        public int TotalBookings => PaidCount + HeldCount + CancelledCount + ExpiredCount;
        public int ActiveMoviesCount { get; set; }
        public int ActiveShowtimesCount { get; set; }
        public List<MovieRevenueStat> MovieRevenues { get; set; } = new();
        public List<BookingAudit> RecentAudits { get; set; } = new();
        public List<Booking> RecentBookings { get; set; } = new();
    }

    public class AdminMoviesViewModel
    {
        public List<Movie> Movies { get; set; } = new();
    }

    public class CreateMovieInputModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên phim.")]
        [StringLength(160, ErrorMessage = "Tên phim không vượt quá 160 ký tự.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập thể loại phim.")]
        [StringLength(80, ErrorMessage = "Thể loại phim không vượt quá 80 ký tự.")]
        public string Genre { get; set; } = string.Empty;

        [Range(30, 300, ErrorMessage = "Thời lượng phim phải từ 30 đến 300 phút.")]
        public int DurationMin { get; set; }

        public string AgeRating { get; set; } = "P";

        [StringLength(900, ErrorMessage = "Tóm tắt phim không vượt quá 900 ký tự.")]
        public string? Synopsis { get; set; }

        public string? PosterFile { get; set; }

        public IFormFile? PosterUpload { get; set; }
    }

    public class UpdateMovieInputModel
    {
        [Required]
        public int MovieId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên phim.")]
        [StringLength(160, ErrorMessage = "Tên phim không vượt quá 160 ký tự.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập thể loại phim.")]
        [StringLength(80, ErrorMessage = "Thể loại phim không vượt quá 80 ký tự.")]
        public string Genre { get; set; } = string.Empty;

        [Range(30, 300, ErrorMessage = "Thời lượng phim phải từ 30 đến 300 phút.")]
        public int DurationMin { get; set; }

        public string AgeRating { get; set; } = "P";

        [StringLength(900, ErrorMessage = "Tóm tắt phim không vượt quá 900 ký tự.")]
        public string? Synopsis { get; set; }

        public string? PosterFile { get; set; }

        public IFormFile? PosterUpload { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class AdminShowtimesViewModel
    {
        public List<Showtime> Showtimes { get; set; } = new();
        public List<Movie> ActiveMovies { get; set; } = new();
        public List<Room> AvailableRooms { get; set; } = new();
    }

    public class CreateShowtimeInputModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn phim.")]
        public int MovieId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn phòng chiếu.")]
        public int RoomId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn thời gian bắt đầu.")]
        public DateTime StartAt { get; set; }

        public DateTime EndAt { get; set; }

        [Range(10000, 1000000, ErrorMessage = "Giá vé cơ bản phải từ 10.000 ₫ đến 1.000.000 ₫.")]
        public decimal BasePrice { get; set; }
    }

    public class UpdateShowtimeInputModel
    {
        [Required]
        public int ShowtimeId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn thời gian bắt đầu.")]
        public DateTime StartAt { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn thời gian kết thúc.")]
        public DateTime EndAt { get; set; }

        [Range(10000, 1000000, ErrorMessage = "Giá vé cơ bản phải từ 10.000 ₫ đến 1.000.000 ₫.")]
        public decimal BasePrice { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class AdminCinemasViewModel
    {
        public List<Cinema> Cinemas { get; set; } = new();
        public List<Room> Rooms { get; set; } = new();
    }

    public class CreateCinemaInputModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên cụm rạp.")]
        [StringLength(120, ErrorMessage = "Tên cụm rạp không quá 120 ký tự.")]
        public string CinemaName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ cụm rạp.")]
        [StringLength(260, ErrorMessage = "Địa chỉ không quá 260 ký tự.")]
        public string Address { get; set; } = string.Empty;
    }

    public class UpdateCinemaInputModel
    {
        [Required]
        public int CinemaId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên cụm rạp.")]
        [StringLength(120, ErrorMessage = "Tên cụm rạp không quá 120 ký tự.")]
        public string CinemaName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ cụm rạp.")]
        [StringLength(260, ErrorMessage = "Địa chỉ không quá 260 ký tự.")]
        public string Address { get; set; } = string.Empty;
    }

    public class CreateRoomInputModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn cụm rạp.")]
        public int CinemaId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên phòng chiếu.")]
        [StringLength(50, ErrorMessage = "Tên phòng chiếu không quá 50 ký tự.")]
        public string RoomName { get; set; } = string.Empty;
    }

    public class PromotionAdminModel
    {
        public int PromotionId { get; set; }
        public string PromoCode { get; set; } = string.Empty;
        public decimal DiscountPct { get; set; }
        public DateTime ValidUntil { get; set; }
        public bool IsActive { get; set; }
        public int UsedCount { get; set; }
    }

    public class AdminPromotionsViewModel
    {
        public List<PromotionAdminModel> Promotions { get; set; } = new();
    }

    public class CreatePromotionInputModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mã khuyến mãi.")]
        [RegularExpression(@"^[A-Za-z0-9_-]{3,30}$", ErrorMessage = "Mã khuyến mãi phải từ 3 đến 30 ký tự chữ và số.")]
        public string PromoCode { get; set; } = string.Empty;

        [Range(1, 50, ErrorMessage = "Tỷ lệ giảm giá phải từ 1% đến 50%.")]
        public decimal DiscountPct { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày hết hạn.")]
        public DateTime ValidUntil { get; set; } = DateTime.Today.AddMonths(1);
    }

    public class UpdatePromotionInputModel
    {
        [Required]
        public int PromotionId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã khuyến mãi.")]
        [RegularExpression(@"^[A-Za-z0-9_-]{3,30}$", ErrorMessage = "Mã khuyến mãi phải từ 3 đến 30 ký tự chữ và số.")]
        public string PromoCode { get; set; } = string.Empty;

        [Range(1, 50, ErrorMessage = "Tỷ lệ giảm giá phải từ 1% đến 50%.")]
        public decimal DiscountPct { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày hết hạn.")]
        public DateTime ValidUntil { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class AdminBookingsViewModel
    {
        public List<Booking> Bookings { get; set; } = new();
        public string StatusFilter { get; set; } = "ALL";
    }
}
