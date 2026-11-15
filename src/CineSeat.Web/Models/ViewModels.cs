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

    public class AdminDashboardViewModel
    {
        public decimal TotalRevenue { get; set; }
        public int PaidCount { get; set; }
        public int HeldCount { get; set; }
        public int CancelledCount { get; set; }
        public int ExpiredCount { get; set; }
        public int TotalBookings => PaidCount + HeldCount + CancelledCount + ExpiredCount;
        public List<BookingAudit> RecentAudits { get; set; } = new();
        public List<Booking> RecentBookings { get; set; } = new();
    }
}
