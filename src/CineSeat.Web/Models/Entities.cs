namespace CineSeat.Web.Models
{
    public class Movie
    {
        public int MovieId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public int DurationMin { get; set; }
        public string AgeRating { get; set; } = "P";
        public string? Synopsis { get; set; }
        public string? PosterFile { get; set; }
        public bool IsActive { get; set; } = true;
        public int ShowtimeCount { get; set; }
        public bool HasPremiered { get; set; }
        public bool CanEdit => !HasPremiered && ShowtimeCount == 0;
    }

    public class Cinema
    {
        public int CinemaId { get; set; }
        public string CinemaName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int RoomCount { get; set; }
    }

    public class Room
    {
        public int RoomId { get; set; }
        public int CinemaId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public string CinemaName { get; set; } = string.Empty;
        public int SeatCount { get; set; } = 40;
    }

    public class Seat
    {
        public int SeatId { get; set; }
        public int RoomId { get; set; }
        public string RowLabel { get; set; } = string.Empty;
        public int SeatNumber { get; set; }
        public string SeatType { get; set; } = "STANDARD"; // STANDARD or VIP
    }

    public class Showtime
    {
        public int ShowtimeId { get; set; }
        public int MovieId { get; set; }
        public string MovieTitle { get; set; } = string.Empty;
        public string? PosterFile { get; set; }
        public string AgeRating { get; set; } = "P";
        public int DurationMin { get; set; }
        public int RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public int CinemaId { get; set; }
        public string CinemaName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public decimal BasePrice { get; set; }
        public int AvailableSeats { get; set; }
        public int TotalSeats { get; set; } = 40;
        public bool IsActive { get; set; } = true;
    }

    public class ShowtimeSeat
    {
        public int ShowtimeId { get; set; }
        public int SeatId { get; set; }
        public string RowLabel { get; set; } = string.Empty;
        public int SeatNumber { get; set; }
        public string SeatType { get; set; } = "STANDARD"; // STANDARD, VIP
        public string Status { get; set; } = "AVAILABLE"; // AVAILABLE, HELD, SOLD
        public int? BookingId { get; set; }
        public DateTime? HeldUntil { get; set; }
        public string SeatCode => $"{RowLabel}{SeatNumber}";
    }

    public class AppUser
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class Promotion
    {
        public int PromotionId { get; set; }
        public string PromoCode { get; set; } = string.Empty;
        public decimal DiscountPct { get; set; }
        public DateTime ValidUntil { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class Booking
    {
        public int BookingId { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public int ShowtimeId { get; set; }
        public string MovieTitle { get; set; } = string.Empty;
        public string? PosterFile { get; set; }
        public string? RoomName { get; set; }
        public string? CinemaName { get; set; }
        public DateTime? StartAt { get; set; }
        public DateTime? EndAt { get; set; }
        public int? PromotionId { get; set; }
        public string? PromoCode { get; set; }
        public string Status { get; set; } = "HELD"; // HELD, PAID, CANCELLED, EXPIRED
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public int SeatCount { get; set; }
        public List<BookingSeat> Seats { get; set; } = new();
    }

    public class BookingSeat
    {
        public int BookingId { get; set; }
        public int SeatId { get; set; }
        public decimal UnitPrice { get; set; }
        public string RowLabel { get; set; } = string.Empty;
        public int SeatNumber { get; set; }
        public string SeatType { get; set; } = "STANDARD";
        public string SeatCode => $"{RowLabel}{SeatNumber}";
    }

    public class BookingAudit
    {
        public int AuditId { get; set; }
        public int BookingId { get; set; }
        public string? OldStatus { get; set; }
        public string? NewStatus { get; set; }
        public DateTime ChangedAt { get; set; }
        public string? Description { get; set; }
        public string? CustomerName { get; set; }
        public string? MovieTitle { get; set; }
    }

    public class Payment
    {
        public int PaymentId { get; set; }
        public int BookingId { get; set; }
        public decimal Amount { get; set; }
        public string Method { get; set; } = "DEMO_QR";
        public string PaymentStatus { get; set; } = "PAID";
        public DateTime CreatedAt { get; set; }
    }
}
