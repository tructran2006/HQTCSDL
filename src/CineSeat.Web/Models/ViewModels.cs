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
        public Dictionary<DateTime, List<Showtime>> GroupedByDate =>
            Showtimes.GroupBy(s => s.StartAt.Date)
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
        public string Title { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public int DurationMin { get; set; }
        public string AgeRating { get; set; } = "P";
        public string? Synopsis { get; set; }
        public string? PosterFile { get; set; }
    }

    public class AdminShowtimesViewModel
    {
        public List<Showtime> Showtimes { get; set; } = new();
        public List<Movie> ActiveMovies { get; set; } = new();
        public List<Room> AvailableRooms { get; set; } = new();
    }

    public class CreateShowtimeInputModel
    {
        public int MovieId { get; set; }
        public int RoomId { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public decimal BasePrice { get; set; }
    }

    public class AdminBookingsViewModel
    {
        public List<Booking> Bookings { get; set; } = new();
        public string StatusFilter { get; set; } = "ALL";
    }
}
