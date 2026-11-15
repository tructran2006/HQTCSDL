using System.Data;
using CineSeat.Web.Data;
using CineSeat.Web.Exceptions;
using CineSeat.Web.Models;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace CineSeat.Web.Services
{
    public class CineSeatService : ICineSeatService
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly ILogger<CineSeatService> _logger;

        public CineSeatService(IDbConnectionFactory connectionFactory, ILogger<CineSeatService> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        #region Phim & Suất chiếu

        public async Task<List<Movie>> GetMoviesAsync(string? search = null, string? genre = null)
        {
            var movies = new List<Movie>();
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var cmd = conn.CreateCommand();

            var sql = "SELECT movie_id, title, genre, duration_min, age_rating, synopsis, poster_file, is_active FROM movies WHERE is_active = 1";

            if (!string.IsNullOrWhiteSpace(search))
            {
                sql += " AND (LOWER(title) LIKE :p_search OR LOWER(synopsis) LIKE :p_search)";
                cmd.Parameters.Add("p_search", OracleDbType.NVarchar2, $"%{search.Trim().ToLower()}%", ParameterDirection.Input);
            }

            if (!string.IsNullOrWhiteSpace(genre))
            {
                sql += " AND LOWER(genre) LIKE :p_genre";
                cmd.Parameters.Add("p_genre", OracleDbType.NVarchar2, $"%{genre.Trim().ToLower()}%", ParameterDirection.Input);
            }

            sql += " ORDER BY movie_id";
            cmd.CommandText = sql;

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                movies.Add(new Movie
                {
                    MovieId = Convert.ToInt32(reader["movie_id"]),
                    Title = reader["title"]?.ToString() ?? string.Empty,
                    Genre = reader["genre"]?.ToString() ?? string.Empty,
                    DurationMin = Convert.ToInt32(reader["duration_min"]),
                    AgeRating = reader["age_rating"]?.ToString() ?? "P",
                    Synopsis = reader["synopsis"]?.ToString(),
                    PosterFile = reader["poster_file"]?.ToString(),
                    IsActive = Convert.ToInt32(reader["is_active"]) == 1
                });
            }

            return movies;
        }

        public async Task<Movie?> GetMovieByIdAsync(int movieId)
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT movie_id, title, genre, duration_min, age_rating, synopsis, poster_file, is_active FROM movies WHERE movie_id = :p_id";
            cmd.Parameters.Add("p_id", OracleDbType.Decimal, movieId, ParameterDirection.Input);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Movie
                {
                    MovieId = Convert.ToInt32(reader["movie_id"]),
                    Title = reader["title"]?.ToString() ?? string.Empty,
                    Genre = reader["genre"]?.ToString() ?? string.Empty,
                    DurationMin = Convert.ToInt32(reader["duration_min"]),
                    AgeRating = reader["age_rating"]?.ToString() ?? "P",
                    Synopsis = reader["synopsis"]?.ToString(),
                    PosterFile = reader["poster_file"]?.ToString(),
                    IsActive = Convert.ToInt32(reader["is_active"]) == 1
                };
            }
            return null;
        }

        public async Task<List<Showtime>> GetShowtimesByMovieIdAsync(int movieId)
        {
            var showtimes = new List<Showtime>();
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"SELECT showtime_id, movie_id, movie_title, start_at, end_at, base_price,
                                       room_name, cinema_name, address, available_seats
                                FROM v_showtime_catalog
                                WHERE movie_id = :p_movie_id AND start_at > LOCALTIMESTAMP
                                ORDER BY start_at";
            cmd.Parameters.Add("p_movie_id", OracleDbType.Decimal, movieId, ParameterDirection.Input);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                showtimes.Add(new Showtime
                {
                    ShowtimeId = Convert.ToInt32(reader["showtime_id"]),
                    MovieId = Convert.ToInt32(reader["movie_id"]),
                    MovieTitle = reader["movie_title"]?.ToString() ?? string.Empty,
                    StartAt = Convert.ToDateTime(reader["start_at"]),
                    EndAt = Convert.ToDateTime(reader["end_at"]),
                    BasePrice = Convert.ToDecimal(reader["base_price"]),
                    RoomName = reader["room_name"]?.ToString() ?? string.Empty,
                    CinemaName = reader["cinema_name"]?.ToString() ?? string.Empty,
                    Address = reader["address"]?.ToString() ?? string.Empty,
                    AvailableSeats = Convert.ToInt32(reader["available_seats"]),
                    IsActive = true
                });
            }

            return showtimes;
        }

        public async Task<Showtime?> GetShowtimeByIdAsync(int showtimeId)
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"SELECT s.showtime_id, s.movie_id, m.title AS movie_title, s.start_at, s.end_at,
                                       s.base_price, r.room_name, c.cinema_name, c.address,
                                       (SELECT COUNT(*) FROM showtime_seats ss WHERE ss.showtime_id = s.showtime_id AND ss.status = 'AVAILABLE') AS available_seats
                                FROM showtimes s
                                JOIN movies m ON m.movie_id = s.movie_id
                                JOIN rooms r ON r.room_id = s.room_id
                                JOIN cinemas c ON c.cinema_id = r.cinema_id
                                WHERE s.showtime_id = :p_id";
            cmd.Parameters.Add("p_id", OracleDbType.Decimal, showtimeId, ParameterDirection.Input);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Showtime
                {
                    ShowtimeId = Convert.ToInt32(reader["showtime_id"]),
                    MovieId = Convert.ToInt32(reader["movie_id"]),
                    MovieTitle = reader["movie_title"]?.ToString() ?? string.Empty,
                    StartAt = Convert.ToDateTime(reader["start_at"]),
                    EndAt = Convert.ToDateTime(reader["end_at"]),
                    BasePrice = Convert.ToDecimal(reader["base_price"]),
                    RoomName = reader["room_name"]?.ToString() ?? string.Empty,
                    CinemaName = reader["cinema_name"]?.ToString() ?? string.Empty,
                    Address = reader["address"]?.ToString() ?? string.Empty,
                    AvailableSeats = Convert.ToInt32(reader["available_seats"]),
                    IsActive = true
                };
            }
            return null;
        }

        public async Task<List<ShowtimeSeat>> GetShowtimeSeatsAsync(int showtimeId)
        {
            // Trước khi lấy danh sách ghế, thu hồi các đơn quá hạn 5 phút
            await ExpireHoldsAsync();

            var seats = new List<ShowtimeSeat>();
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"SELECT ss.showtime_id, ss.seat_id, s.row_label, s.seat_number, s.seat_type,
                                       ss.status, ss.booking_id, ss.held_until
                                FROM showtime_seats ss
                                JOIN seats s ON s.seat_id = ss.seat_id
                                WHERE ss.showtime_id = :p_showtime_id
                                ORDER BY s.row_label, s.seat_number";
            cmd.Parameters.Add("p_showtime_id", OracleDbType.Decimal, showtimeId, ParameterDirection.Input);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                seats.Add(new ShowtimeSeat
                {
                    ShowtimeId = Convert.ToInt32(reader["showtime_id"]),
                    SeatId = Convert.ToInt32(reader["seat_id"]),
                    RowLabel = reader["row_label"]?.ToString() ?? string.Empty,
                    SeatNumber = Convert.ToInt32(reader["seat_number"]),
                    SeatType = reader["seat_type"]?.ToString() ?? "STANDARD",
                    Status = reader["status"]?.ToString() ?? "AVAILABLE",
                    BookingId = reader["booking_id"] != DBNull.Value ? Convert.ToInt32(reader["booking_id"]) : null,
                    HeldUntil = reader["held_until"] != DBNull.Value ? Convert.ToDateTime(reader["held_until"]) : null
                });
            }

            return seats;
        }

        #endregion

        #region Người dùng & Khuyến mãi

        public async Task<List<AppUser>> GetUsersAsync()
        {
            var users = new List<AppUser>();
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT user_id, full_name, email, phone, created_at FROM app_users ORDER BY user_id";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                users.Add(new AppUser
                {
                    UserId = Convert.ToInt32(reader["user_id"]),
                    FullName = reader["full_name"]?.ToString() ?? string.Empty,
                    Email = reader["email"]?.ToString() ?? string.Empty,
                    Phone = reader["phone"]?.ToString(),
                    CreatedAt = Convert.ToDateTime(reader["created_at"])
                });
            }
            return users;
        }

        public async Task<AppUser?> GetUserByIdAsync(int userId)
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT user_id, full_name, email, phone, created_at FROM app_users WHERE user_id = :p_id";
            cmd.Parameters.Add("p_id", OracleDbType.Decimal, userId, ParameterDirection.Input);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new AppUser
                {
                    UserId = Convert.ToInt32(reader["user_id"]),
                    FullName = reader["full_name"]?.ToString() ?? string.Empty,
                    Email = reader["email"]?.ToString() ?? string.Empty,
                    Phone = reader["phone"]?.ToString(),
                    CreatedAt = Convert.ToDateTime(reader["created_at"])
                };
            }
            return null;
        }

        public async Task<List<Promotion>> GetActivePromotionsAsync()
        {
            var promos = new List<Promotion>();
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT promotion_id, promo_code, discount_pct, valid_until FROM promotions WHERE is_active = 1 AND valid_until >= TRUNC(SYSDATE) ORDER BY discount_pct DESC";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                promos.Add(new Promotion
                {
                    PromotionId = Convert.ToInt32(reader["promotion_id"]),
                    PromoCode = reader["promo_code"]?.ToString() ?? string.Empty,
                    DiscountPct = Convert.ToDecimal(reader["discount_pct"]),
                    ValidUntil = Convert.ToDateTime(reader["valid_until"]),
                    IsActive = true
                });
            }
            return promos;
        }

        #endregion

        #region PL/SQL Package PKG_BOOKING & PR_EXPIRE_HOLDS

        public async Task<decimal> CalculateTotalPriceAsync(int showtimeId, int seatCount, string? promoCode)
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var cmd = conn.CreateCommand();
            cmd.BindByName = true;
            cmd.CommandText = "BEGIN :result := pkg_booking.fn_total_price(:p_showtime_id, :p_seat_count, :p_promo_code); END;";

            var pResult = cmd.Parameters.Add("result", OracleDbType.Decimal, ParameterDirection.Output);
            cmd.Parameters.Add("p_showtime_id", OracleDbType.Decimal, showtimeId, ParameterDirection.Input);
            cmd.Parameters.Add("p_seat_count", OracleDbType.Decimal, seatCount, ParameterDirection.Input);

            if (string.IsNullOrWhiteSpace(promoCode))
            {
                cmd.Parameters.Add("p_promo_code", OracleDbType.Varchar2, DBNull.Value, ParameterDirection.Input);
            }
            else
            {
                cmd.Parameters.Add("p_promo_code", OracleDbType.Varchar2, promoCode.Trim(), ParameterDirection.Input);
            }

            try
            {
                await cmd.ExecuteNonQueryAsync();
                var oracleDecimal = (OracleDecimal)pResult.Value;
                return oracleDecimal.Value;
            }
            catch (OracleException oex)
            {
                _logger.LogWarning("OracleException in fn_total_price: {Code} - {Msg}", oex.Number, oex.Message);
                throw CineSeatException.FromOracleException(oex);
            }
        }

        public async Task<(int bookingId, decimal totalAmount, DateTime expiresAt)> HoldSeatsAsync(
            int userId, int showtimeId, string seatIdsJson, string? promoCode)
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var trans = conn.BeginTransaction();
            using var cmd = conn.CreateCommand();
            cmd.Transaction = trans;
            cmd.BindByName = true;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "pkg_booking.pr_hold";

            cmd.Parameters.Add("p_user_id", OracleDbType.Decimal, userId, ParameterDirection.Input);
            cmd.Parameters.Add("p_showtime_id", OracleDbType.Decimal, showtimeId, ParameterDirection.Input);
            cmd.Parameters.Add("p_seats_json", OracleDbType.Clob, seatIdsJson, ParameterDirection.Input);

            if (string.IsNullOrWhiteSpace(promoCode))
            {
                cmd.Parameters.Add("p_promo_code", OracleDbType.Varchar2, DBNull.Value, ParameterDirection.Input);
            }
            else
            {
                cmd.Parameters.Add("p_promo_code", OracleDbType.Varchar2, promoCode.Trim(), ParameterDirection.Input);
            }

            var pBookingId = cmd.Parameters.Add("p_booking_id", OracleDbType.Decimal, ParameterDirection.Output);
            var pTotal = cmd.Parameters.Add("p_total", OracleDbType.Decimal, ParameterDirection.Output);
            var pExpires = cmd.Parameters.Add("p_expires", OracleDbType.TimeStamp, ParameterDirection.Output);

            try
            {
                await cmd.ExecuteNonQueryAsync();
                await trans.CommitAsync();

                int bookingId = ((OracleDecimal)pBookingId.Value).ToInt32();
                decimal total = ((OracleDecimal)pTotal.Value).Value;
                DateTime expires = ((OracleTimeStamp)pExpires.Value).Value;

                _logger.LogInformation("HoldSeatsAsync success: bookingId={BookingId}, total={Total}, expires={Expires}", bookingId, total, expires);
                return (bookingId, total, expires);
            }
            catch (OracleException oex)
            {
                await trans.RollbackAsync();
                _logger.LogWarning("HoldSeatsAsync failed: ORA-{Code} - {Msg}", oex.Number, oex.Message);
                throw CineSeatException.FromOracleException(oex);
            }
            catch (Exception ex)
            {
                await trans.RollbackAsync();
                _logger.LogError(ex, "Unexpected error in HoldSeatsAsync");
                throw;
            }
        }

        public async Task ConfirmBookingAsync(int bookingId, string method)
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var trans = conn.BeginTransaction();
            using var cmd = conn.CreateCommand();
            cmd.Transaction = trans;
            cmd.BindByName = true;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "pkg_booking.pr_confirm";

            cmd.Parameters.Add("p_booking_id", OracleDbType.Decimal, bookingId, ParameterDirection.Input);
            cmd.Parameters.Add("p_method", OracleDbType.Varchar2, method, ParameterDirection.Input);

            try
            {
                await cmd.ExecuteNonQueryAsync();
                await trans.CommitAsync();
                _logger.LogInformation("ConfirmBookingAsync success: bookingId={BookingId}, method={Method}", bookingId, method);
            }
            catch (OracleException oex)
            {
                await trans.RollbackAsync();
                _logger.LogWarning("ConfirmBookingAsync failed: ORA-{Code} - {Msg}", oex.Number, oex.Message);
                throw CineSeatException.FromOracleException(oex);
            }
            catch (Exception ex)
            {
                await trans.RollbackAsync();
                _logger.LogError(ex, "Unexpected error in ConfirmBookingAsync");
                throw;
            }
        }

        public async Task CancelBookingAsync(int bookingId)
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var trans = conn.BeginTransaction();
            using var cmd = conn.CreateCommand();
            cmd.Transaction = trans;
            cmd.BindByName = true;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "pkg_booking.pr_cancel";

            cmd.Parameters.Add("p_booking_id", OracleDbType.Decimal, bookingId, ParameterDirection.Input);

            try
            {
                await cmd.ExecuteNonQueryAsync();
                await trans.CommitAsync();
                _logger.LogInformation("CancelBookingAsync success: bookingId={BookingId}", bookingId);
            }
            catch (OracleException oex)
            {
                await trans.RollbackAsync();
                _logger.LogWarning("CancelBookingAsync failed: ORA-{Code} - {Msg}", oex.Number, oex.Message);
                throw CineSeatException.FromOracleException(oex);
            }
            catch (Exception ex)
            {
                await trans.RollbackAsync();
                _logger.LogError(ex, "Unexpected error in CancelBookingAsync");
                throw;
            }
        }

        public async Task ExpireHoldsAsync()
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var trans = conn.BeginTransaction();
            using var cmd = conn.CreateCommand();
            cmd.Transaction = trans;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "pr_expire_holds";

            try
            {
                await cmd.ExecuteNonQueryAsync();
                await trans.CommitAsync();
            }
            catch (OracleException oex)
            {
                await trans.RollbackAsync();
                _logger.LogWarning("ExpireHoldsAsync failed: ORA-{Code} - {Msg}", oex.Number, oex.Message);
                throw CineSeatException.FromOracleException(oex);
            }
            catch (Exception ex)
            {
                await trans.RollbackAsync();
                _logger.LogError(ex, "Unexpected error in ExpireHoldsAsync");
                throw;
            }
        }

        #endregion

        #region Đơn hàng & Báo cáo & Dashboard

        public async Task<Booking?> GetBookingByIdAsync(int bookingId)
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"SELECT b.booking_id, b.user_id, u.full_name, b.showtime_id, m.title AS movie_title,
                                       r.room_name, c.cinema_name, s.start_at, b.promotion_id, p.promo_code,
                                       b.status, b.total_amount, b.created_at, b.expires_at, b.paid_at,
                                       (SELECT COUNT(*) FROM booking_seats bs WHERE bs.booking_id = b.booking_id) AS seat_count
                                FROM bookings b
                                JOIN app_users u ON u.user_id = b.user_id
                                JOIN showtimes s ON s.showtime_id = b.showtime_id
                                JOIN movies m ON m.movie_id = s.movie_id
                                JOIN rooms r ON r.room_id = s.room_id
                                JOIN cinemas c ON c.cinema_id = r.cinema_id
                                LEFT JOIN promotions p ON p.promotion_id = b.promotion_id
                                WHERE b.booking_id = :p_booking_id";
            cmd.Parameters.Add("p_booking_id", OracleDbType.Decimal, bookingId, ParameterDirection.Input);

            Booking? booking = null;
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    booking = new Booking
                    {
                        BookingId = Convert.ToInt32(reader["booking_id"]),
                        UserId = Convert.ToInt32(reader["user_id"]),
                        FullName = reader["full_name"]?.ToString() ?? string.Empty,
                        ShowtimeId = Convert.ToInt32(reader["showtime_id"]),
                        MovieTitle = reader["movie_title"]?.ToString() ?? string.Empty,
                        RoomName = reader["room_name"]?.ToString(),
                        CinemaName = reader["cinema_name"]?.ToString(),
                        StartAt = Convert.ToDateTime(reader["start_at"]),
                        PromotionId = reader["promotion_id"] != DBNull.Value ? Convert.ToInt32(reader["promotion_id"]) : null,
                        PromoCode = reader["promo_code"]?.ToString(),
                        Status = reader["status"]?.ToString() ?? "HELD",
                        TotalAmount = Convert.ToDecimal(reader["total_amount"]),
                        CreatedAt = Convert.ToDateTime(reader["created_at"]),
                        ExpiresAt = Convert.ToDateTime(reader["expires_at"]),
                        PaidAt = reader["paid_at"] != DBNull.Value ? Convert.ToDateTime(reader["paid_at"]) : null,
                        SeatCount = Convert.ToInt32(reader["seat_count"])
                    };
                }
            }

            if (booking != null)
            {
                // Lấy chi tiết ghế đã chọn
                using var seatCmd = conn.CreateCommand();
                seatCmd.CommandText = @"SELECT bs.booking_id, bs.seat_id, bs.unit_price, s.row_label, s.seat_number, s.seat_type
                                        FROM booking_seats bs
                                        JOIN seats s ON s.seat_id = bs.seat_id
                                        WHERE bs.booking_id = :p_booking_id
                                        ORDER BY s.row_label, s.seat_number";
                seatCmd.Parameters.Add("p_booking_id", OracleDbType.Decimal, bookingId, ParameterDirection.Input);

                using var seatReader = await seatCmd.ExecuteReaderAsync();
                while (await seatReader.ReadAsync())
                {
                    booking.Seats.Add(new BookingSeat
                    {
                        BookingId = Convert.ToInt32(seatReader["booking_id"]),
                        SeatId = Convert.ToInt32(seatReader["seat_id"]),
                        UnitPrice = Convert.ToDecimal(seatReader["unit_price"]),
                        RowLabel = seatReader["row_label"]?.ToString() ?? string.Empty,
                        SeatNumber = Convert.ToInt32(seatReader["seat_number"]),
                        SeatType = seatReader["seat_type"]?.ToString() ?? "STANDARD"
                    });
                }
            }

            return booking;
        }

        public async Task<List<Booking>> GetBookingHistoryByUserIdAsync(int userId)
        {
            var list = new List<Booking>();
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"SELECT b.booking_id, b.user_id, u.full_name, b.showtime_id, m.title AS movie_title,
                                       r.room_name, c.cinema_name, s.start_at,
                                       b.status, b.total_amount, b.created_at, b.expires_at, b.paid_at,
                                       (SELECT COUNT(*) FROM booking_seats bs WHERE bs.booking_id = b.booking_id) AS seat_count
                                FROM bookings b
                                JOIN app_users u ON u.user_id = b.user_id
                                JOIN showtimes s ON s.showtime_id = b.showtime_id
                                JOIN movies m ON m.movie_id = s.movie_id
                                JOIN rooms r ON r.room_id = s.room_id
                                JOIN cinemas c ON c.cinema_id = r.cinema_id
                                WHERE b.user_id = :p_user_id
                                ORDER BY b.created_at DESC";
            cmd.Parameters.Add("p_user_id", OracleDbType.Decimal, userId, ParameterDirection.Input);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new Booking
                {
                    BookingId = Convert.ToInt32(reader["booking_id"]),
                    UserId = Convert.ToInt32(reader["user_id"]),
                    FullName = reader["full_name"]?.ToString() ?? string.Empty,
                    ShowtimeId = Convert.ToInt32(reader["showtime_id"]),
                    MovieTitle = reader["movie_title"]?.ToString() ?? string.Empty,
                    RoomName = reader["room_name"]?.ToString(),
                    CinemaName = reader["cinema_name"]?.ToString(),
                    StartAt = Convert.ToDateTime(reader["start_at"]),
                    Status = reader["status"]?.ToString() ?? "HELD",
                    TotalAmount = Convert.ToDecimal(reader["total_amount"]),
                    CreatedAt = Convert.ToDateTime(reader["created_at"]),
                    ExpiresAt = Convert.ToDateTime(reader["expires_at"]),
                    PaidAt = reader["paid_at"] != DBNull.Value ? Convert.ToDateTime(reader["paid_at"]) : null,
                    SeatCount = Convert.ToInt32(reader["seat_count"])
                });
            }

            // Gắn danh sách mã ghế
            foreach (var b in list)
            {
                using var seatCmd = conn.CreateCommand();
                seatCmd.CommandText = @"SELECT bs.booking_id, bs.seat_id, bs.unit_price, s.row_label, s.seat_number, s.seat_type
                                        FROM booking_seats bs
                                        JOIN seats s ON s.seat_id = bs.seat_id
                                        WHERE bs.booking_id = :p_b_id
                                        ORDER BY s.row_label, s.seat_number";
                seatCmd.Parameters.Add("p_b_id", OracleDbType.Decimal, b.BookingId, ParameterDirection.Input);
                using var seatReader = await seatCmd.ExecuteReaderAsync();
                while (await seatReader.ReadAsync())
                {
                    b.Seats.Add(new BookingSeat
                    {
                        BookingId = Convert.ToInt32(seatReader["booking_id"]),
                        SeatId = Convert.ToInt32(seatReader["seat_id"]),
                        UnitPrice = Convert.ToDecimal(seatReader["unit_price"]),
                        RowLabel = seatReader["row_label"]?.ToString() ?? string.Empty,
                        SeatNumber = Convert.ToInt32(seatReader["seat_number"]),
                        SeatType = seatReader["seat_type"]?.ToString() ?? "STANDARD"
                    });
                }
            }

            return list;
        }

        public async Task<AdminDashboardViewModel> GetAdminDashboardStatsAsync()
        {
            var vm = new AdminDashboardViewModel();
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();

            // 1. Thống kê theo trạng thái và tổng doanh thu
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT 
                                        NVL(SUM(CASE WHEN status = 'PAID' THEN total_amount ELSE 0 END), 0) AS total_revenue,
                                        NVL(SUM(CASE WHEN status = 'PAID' THEN 1 ELSE 0 END), 0) AS count_paid,
                                        NVL(SUM(CASE WHEN status = 'HELD' THEN 1 ELSE 0 END), 0) AS count_held,
                                        NVL(SUM(CASE WHEN status = 'CANCELLED' THEN 1 ELSE 0 END), 0) AS count_cancelled,
                                        NVL(SUM(CASE WHEN status = 'EXPIRED' THEN 1 ELSE 0 END), 0) AS count_expired
                                    FROM bookings";

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    vm.TotalRevenue = Convert.ToDecimal(reader["total_revenue"]);
                    vm.PaidCount = Convert.ToInt32(reader["count_paid"]);
                    vm.HeldCount = Convert.ToInt32(reader["count_held"]);
                    vm.CancelledCount = Convert.ToInt32(reader["count_cancelled"]);
                    vm.ExpiredCount = Convert.ToInt32(reader["count_expired"]);
                }
            }

            // 2. Nhật ký Audit gần đây từ bảng BOOKING_AUDIT
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT audit_id, booking_id, old_status, new_status, changed_at, description
                                    FROM booking_audit
                                    ORDER BY changed_at DESC, audit_id DESC
                                    FETCH FIRST 30 ROWS ONLY";

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    vm.RecentAudits.Add(new BookingAudit
                    {
                        AuditId = Convert.ToInt32(reader["audit_id"]),
                        BookingId = Convert.ToInt32(reader["booking_id"]),
                        OldStatus = reader["old_status"]?.ToString(),
                        NewStatus = reader["new_status"]?.ToString(),
                        ChangedAt = Convert.ToDateTime(reader["changed_at"]),
                        Description = reader["description"]?.ToString()
                    });
                }
            }

            // 3. Danh sách 10 đơn hàng gần đây
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT b.booking_id, b.user_id, u.full_name, b.showtime_id, m.title AS movie_title,
                                           b.status, b.total_amount, b.created_at, b.expires_at, b.paid_at,
                                           (SELECT COUNT(*) FROM booking_seats bs WHERE bs.booking_id = b.booking_id) AS seat_count
                                    FROM bookings b
                                    JOIN app_users u ON u.user_id = b.user_id
                                    JOIN showtimes s ON s.showtime_id = b.showtime_id
                                    JOIN movies m ON m.movie_id = s.movie_id
                                    ORDER BY b.created_at DESC
                                    FETCH FIRST 10 ROWS ONLY";

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    vm.RecentBookings.Add(new Booking
                    {
                        BookingId = Convert.ToInt32(reader["booking_id"]),
                        UserId = Convert.ToInt32(reader["user_id"]),
                        FullName = reader["full_name"]?.ToString() ?? string.Empty,
                        ShowtimeId = Convert.ToInt32(reader["showtime_id"]),
                        MovieTitle = reader["movie_title"]?.ToString() ?? string.Empty,
                        Status = reader["status"]?.ToString() ?? "HELD",
                        TotalAmount = Convert.ToDecimal(reader["total_amount"]),
                        CreatedAt = Convert.ToDateTime(reader["created_at"]),
                        ExpiresAt = Convert.ToDateTime(reader["expires_at"]),
                        PaidAt = reader["paid_at"] != DBNull.Value ? Convert.ToDateTime(reader["paid_at"]) : null,
                        SeatCount = Convert.ToInt32(reader["seat_count"])
                    });
                }
            }

            return vm;
        }

        #endregion
    }
}
