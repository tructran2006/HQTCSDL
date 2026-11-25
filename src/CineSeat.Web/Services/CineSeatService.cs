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

        #region Phim & Rạp & Suất chiếu

        public async Task<List<Movie>> GetMoviesAsync(string? search = null, string? genre = null, string? ageRating = null)
        {
            var movies = new List<Movie>();
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var cmd = conn.CreateCommand();

            var sql = @"SELECT m.movie_id, m.title, m.genre, m.duration_min, m.age_rating, m.synopsis, m.poster_file, m.is_active,
                               (SELECT COUNT(*) FROM showtimes s WHERE s.movie_id = m.movie_id AND s.is_active = 1 AND s.start_at > LOCALTIMESTAMP) AS showtime_count
                        FROM movies m
                        WHERE m.is_active = 1";

            if (!string.IsNullOrWhiteSpace(search))
            {
                sql += " AND (LOWER(m.title) LIKE :p_search OR LOWER(m.synopsis) LIKE :p_search)";
                cmd.Parameters.Add("p_search", OracleDbType.NVarchar2, $"%{search.Trim().ToLower()}%", ParameterDirection.Input);
            }

            if (!string.IsNullOrWhiteSpace(genre))
            {
                sql += " AND LOWER(m.genre) LIKE :p_genre";
                cmd.Parameters.Add("p_genre", OracleDbType.NVarchar2, $"%{genre.Trim().ToLower()}%", ParameterDirection.Input);
            }

            if (!string.IsNullOrWhiteSpace(ageRating) && ageRating != "ALL")
            {
                sql += " AND UPPER(m.age_rating) = :p_rating";
                cmd.Parameters.Add("p_rating", OracleDbType.Varchar2, ageRating.Trim().ToUpper(), ParameterDirection.Input);
            }

            sql += " ORDER BY m.movie_id";
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
                    IsActive = Convert.ToInt32(reader["is_active"]) == 1,
                    ShowtimeCount = Convert.ToInt32(reader["showtime_count"])
                });
            }

            return movies;
        }

        public async Task<Movie?> GetMovieByIdAsync(int movieId)
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT m.movie_id, m.title, m.genre, m.duration_min, m.age_rating, m.synopsis, m.poster_file, m.is_active,
                                       (SELECT COUNT(*) FROM showtimes s WHERE s.movie_id = m.movie_id AND s.is_active = 1 AND s.start_at > LOCALTIMESTAMP) AS showtime_count
                                FROM movies m
                                WHERE m.movie_id = :p_id";
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
                    IsActive = Convert.ToInt32(reader["is_active"]) == 1,
                    ShowtimeCount = Convert.ToInt32(reader["showtime_count"])
                };
            }
            return null;
        }

        public async Task<List<Showtime>> GetShowtimesByMovieIdAsync(int movieId)
        {
            var showtimes = new List<Showtime>();
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"SELECT s.showtime_id, s.movie_id, m.title AS movie_title, m.poster_file, m.age_rating, m.duration_min,
                                       r.room_id, r.room_name, c.cinema_id, c.cinema_name, c.address,
                                       s.start_at, s.end_at, s.base_price,
                                       (SELECT COUNT(*) FROM showtime_seats ss WHERE ss.showtime_id = s.showtime_id AND ss.status = 'AVAILABLE') AS available_seats
                                FROM showtimes s
                                JOIN movies m ON m.movie_id = s.movie_id
                                JOIN rooms r ON r.room_id = s.room_id
                                JOIN cinemas c ON c.cinema_id = r.cinema_id
                                WHERE s.movie_id = :p_movie_id AND s.is_active = 1 AND m.is_active = 1 AND s.start_at > LOCALTIMESTAMP
                                ORDER BY s.start_at";
            cmd.Parameters.Add("p_movie_id", OracleDbType.Decimal, movieId, ParameterDirection.Input);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                showtimes.Add(new Showtime
                {
                    ShowtimeId = Convert.ToInt32(reader["showtime_id"]),
                    MovieId = Convert.ToInt32(reader["movie_id"]),
                    MovieTitle = reader["movie_title"]?.ToString() ?? string.Empty,
                    PosterFile = reader["poster_file"]?.ToString(),
                    AgeRating = reader["age_rating"]?.ToString() ?? "P",
                    DurationMin = Convert.ToInt32(reader["duration_min"]),
                    RoomId = Convert.ToInt32(reader["room_id"]),
                    RoomName = reader["room_name"]?.ToString() ?? string.Empty,
                    CinemaId = Convert.ToInt32(reader["cinema_id"]),
                    CinemaName = reader["cinema_name"]?.ToString() ?? string.Empty,
                    Address = reader["address"]?.ToString() ?? string.Empty,
                    StartAt = Convert.ToDateTime(reader["start_at"]),
                    EndAt = Convert.ToDateTime(reader["end_at"]),
                    BasePrice = Convert.ToDecimal(reader["base_price"]),
                    AvailableSeats = Convert.ToInt32(reader["available_seats"]),
                    TotalSeats = 40,
                    IsActive = true
                });
            }

            return showtimes;
        }

        public async Task<Showtime?> GetShowtimeByIdAsync(int showtimeId)
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"SELECT s.showtime_id, s.movie_id, m.title AS movie_title, m.poster_file, m.age_rating, m.duration_min,
                                       r.room_id, r.room_name, c.cinema_id, c.cinema_name, c.address,
                                       s.start_at, s.end_at, s.base_price,
                                       (SELECT COUNT(*) FROM showtime_seats ss WHERE ss.showtime_id = s.showtime_id AND ss.status = 'AVAILABLE') AS available_seats
                                FROM showtimes s
                                JOIN movies m ON m.movie_id = s.movie_id
                                JOIN rooms r ON r.room_id = s.room_id
                                JOIN cinemas c ON c.cinema_id = r.cinema_id
                                WHERE s.showtime_id = :p_showtime_id";
            cmd.Parameters.Add("p_showtime_id", OracleDbType.Decimal, showtimeId, ParameterDirection.Input);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Showtime
                {
                    ShowtimeId = Convert.ToInt32(reader["showtime_id"]),
                    MovieId = Convert.ToInt32(reader["movie_id"]),
                    MovieTitle = reader["movie_title"]?.ToString() ?? string.Empty,
                    PosterFile = reader["poster_file"]?.ToString(),
                    AgeRating = reader["age_rating"]?.ToString() ?? "P",
                    DurationMin = Convert.ToInt32(reader["duration_min"]),
                    RoomId = Convert.ToInt32(reader["room_id"]),
                    RoomName = reader["room_name"]?.ToString() ?? string.Empty,
                    CinemaId = Convert.ToInt32(reader["cinema_id"]),
                    CinemaName = reader["cinema_name"]?.ToString() ?? string.Empty,
                    Address = reader["address"]?.ToString() ?? string.Empty,
                    StartAt = Convert.ToDateTime(reader["start_at"]),
                    EndAt = Convert.ToDateTime(reader["end_at"]),
                    BasePrice = Convert.ToDecimal(reader["base_price"]),
                    AvailableSeats = Convert.ToInt32(reader["available_seats"]),
                    TotalSeats = 40,
                    IsActive = true
                };
            }
            return null;
        }

        public async Task<List<ShowtimeSeat>> GetShowtimeSeatsAsync(int showtimeId)
        {
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
                    BookingId = reader["booking_id"] == DBNull.Value ? null : Convert.ToInt32(reader["booking_id"]),
                    HeldUntil = reader["held_until"] == DBNull.Value ? null : Convert.ToDateTime(reader["held_until"])
                });
            }

            return seats;
        }

        public async Task<List<Cinema>> GetCinemasAsync()
        {
            var cinemas = new List<Cinema>();
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT c.cinema_id, c.cinema_name, c.address,
                                       (SELECT COUNT(*) FROM rooms r WHERE r.cinema_id = c.cinema_id) AS room_count
                                FROM cinemas c
                                ORDER BY c.cinema_id";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                cinemas.Add(new Cinema
                {
                    CinemaId = Convert.ToInt32(reader["cinema_id"]),
                    CinemaName = reader["cinema_name"]?.ToString() ?? string.Empty,
                    Address = reader["address"]?.ToString() ?? string.Empty,
                    RoomCount = Convert.ToInt32(reader["room_count"])
                });
            }
            return cinemas;
        }

        public async Task<List<Room>> GetRoomsAsync()
        {
            var rooms = new List<Room>();
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT r.room_id, r.cinema_id, r.room_name, c.cinema_name
                                FROM rooms r
                                JOIN cinemas c ON c.cinema_id = r.cinema_id
                                ORDER BY r.cinema_id, r.room_id";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                rooms.Add(new Room
                {
                    RoomId = Convert.ToInt32(reader["room_id"]),
                    CinemaId = Convert.ToInt32(reader["cinema_id"]),
                    RoomName = reader["room_name"]?.ToString() ?? string.Empty,
                    CinemaName = reader["cinema_name"]?.ToString() ?? string.Empty,
                    SeatCount = 40
                });
            }
            return rooms;
        }

        public async Task<List<Showtime>> GetShowtimesByCinemaIdAsync(int cinemaId)
        {
            var showtimes = new List<Showtime>();
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"SELECT s.showtime_id, s.movie_id, m.title AS movie_title, m.poster_file, m.age_rating, m.duration_min,
                                       r.room_id, r.room_name, c.cinema_id, c.cinema_name, c.address,
                                       s.start_at, s.end_at, s.base_price,
                                       (SELECT COUNT(*) FROM showtime_seats ss WHERE ss.showtime_id = s.showtime_id AND ss.status = 'AVAILABLE') AS available_seats
                                FROM showtimes s
                                JOIN movies m ON m.movie_id = s.movie_id
                                JOIN rooms r ON r.room_id = s.room_id
                                JOIN cinemas c ON c.cinema_id = r.cinema_id
                                WHERE c.cinema_id = :p_cinema_id AND s.is_active = 1 AND m.is_active = 1 AND s.start_at > LOCALTIMESTAMP
                                ORDER BY s.start_at";
            cmd.Parameters.Add("p_cinema_id", OracleDbType.Decimal, cinemaId, ParameterDirection.Input);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                showtimes.Add(new Showtime
                {
                    ShowtimeId = Convert.ToInt32(reader["showtime_id"]),
                    MovieId = Convert.ToInt32(reader["movie_id"]),
                    MovieTitle = reader["movie_title"]?.ToString() ?? string.Empty,
                    PosterFile = reader["poster_file"]?.ToString(),
                    AgeRating = reader["age_rating"]?.ToString() ?? "P",
                    DurationMin = Convert.ToInt32(reader["duration_min"]),
                    RoomId = Convert.ToInt32(reader["room_id"]),
                    RoomName = reader["room_name"]?.ToString() ?? string.Empty,
                    CinemaId = Convert.ToInt32(reader["cinema_id"]),
                    CinemaName = reader["cinema_name"]?.ToString() ?? string.Empty,
                    Address = reader["address"]?.ToString() ?? string.Empty,
                    StartAt = Convert.ToDateTime(reader["start_at"]),
                    EndAt = Convert.ToDateTime(reader["end_at"]),
                    BasePrice = Convert.ToDecimal(reader["base_price"]),
                    AvailableSeats = Convert.ToInt32(reader["available_seats"]),
                    TotalSeats = 40,
                    IsActive = true
                });
            }

            return showtimes;
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

        public async Task<AppUser?> GetUserByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;

            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT user_id, full_name, email, phone, created_at FROM app_users WHERE LOWER(TRIM(email)) = LOWER(TRIM(:p_email))";
            cmd.Parameters.Add("p_email", OracleDbType.Varchar2, email.Trim(), ParameterDirection.Input);

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

        public async Task<AppUser> RegisterUserAsync(string fullName, string email, string phone)
        {
            if (string.IsNullOrWhiteSpace(fullName)) throw new ArgumentException("Họ và tên không được để trống.", nameof(fullName));
            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email không được để trống.", nameof(email));
            if (string.IsNullOrWhiteSpace(phone)) throw new ArgumentException("Số điện thoại không được để trống.", nameof(phone));

            var existing = await GetUserByEmailAsync(email);
            if (existing != null)
            {
                throw new InvalidOperationException($"Email '{email.Trim()}' đã tồn tại trong hệ thống. Vui lòng đăng nhập hoặc sử dụng email khác.");
            }

            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var tx = conn.BeginTransaction();
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT INTO app_users(user_id, full_name, email, phone) 
                                    VALUES ((SELECT NVL(MAX(user_id), 0) + 1 FROM app_users), :p_full_name, :p_email, :p_phone)";
                cmd.Parameters.Add("p_full_name", OracleDbType.NVarchar2, fullName.Trim(), ParameterDirection.Input);
                cmd.Parameters.Add("p_email", OracleDbType.Varchar2, email.Trim().ToLowerInvariant(), ParameterDirection.Input);
                cmd.Parameters.Add("p_phone", OracleDbType.Varchar2, phone.Trim(), ParameterDirection.Input);

                await cmd.ExecuteNonQueryAsync();
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }

            var newUser = await GetUserByEmailAsync(email);
            return newUser ?? throw new InvalidOperationException("Không thể tạo tài khoản người dùng.");
        }

        public async Task<List<Promotion>> GetActivePromotionsAsync()
        {
            var promos = new List<Promotion>();
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT promotion_id, promo_code, discount_pct, valid_until, is_active
                                FROM promotions
                                WHERE is_active = 1 AND valid_until >= TRUNC(SYSDATE)
                                ORDER BY discount_pct DESC";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                promos.Add(new Promotion
                {
                    PromotionId = Convert.ToInt32(reader["promotion_id"]),
                    PromoCode = reader["promo_code"]?.ToString() ?? string.Empty,
                    DiscountPct = Convert.ToDecimal(reader["discount_pct"]),
                    ValidUntil = Convert.ToDateTime(reader["valid_until"]),
                    IsActive = Convert.ToInt32(reader["is_active"]) == 1
                });
            }
            return promos;
        }

        #endregion

        #region Nghiệp vụ PL/SQL PKG_BOOKING & PR_EXPIRE_HOLDS

        public async Task<decimal> CalculateTotalPriceAsync(int showtimeId, int seatCount, string? promoCode)
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "pkg_booking.fn_total_price";
            cmd.CommandType = CommandType.StoredProcedure;

            var returnParam = new OracleParameter("result", OracleDbType.Decimal, ParameterDirection.ReturnValue);
            cmd.Parameters.Add(returnParam);

            cmd.Parameters.Add("p_showtime_id", OracleDbType.Decimal, showtimeId, ParameterDirection.Input);
            cmd.Parameters.Add("p_seat_count", OracleDbType.Decimal, seatCount, ParameterDirection.Input);
            cmd.Parameters.Add("p_promo_code", OracleDbType.Varchar2, string.IsNullOrWhiteSpace(promoCode) ? DBNull.Value : promoCode.Trim(), ParameterDirection.Input);

            try
            {
                await cmd.ExecuteNonQueryAsync();
                var rawValue = returnParam.Value;
                if (rawValue is OracleDecimal dec)
                {
                    return dec.Value;
                }
                return Convert.ToDecimal(rawValue);
            }
            catch (OracleException oex)
            {
                _logger.LogWarning("Oracle error calculating price: {Code} - {Msg}", oex.Number, oex.Message);
                throw CineSeatException.FromOracleException(oex);
            }
        }

        public async Task<(int bookingId, decimal totalAmount, DateTime expiresAt)> HoldSeatsAsync(
            int userId, int showtimeId, string seatIdsJson, string? promoCode)
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var transaction = conn.BeginTransaction(IsolationLevel.ReadCommitted);
            using var cmd = conn.CreateCommand();
            cmd.Transaction = transaction;

            cmd.CommandText = "pkg_booking.pr_hold";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("p_user_id", OracleDbType.Decimal, userId, ParameterDirection.Input);
            cmd.Parameters.Add("p_showtime_id", OracleDbType.Decimal, showtimeId, ParameterDirection.Input);
            cmd.Parameters.Add("p_seats_json", OracleDbType.Clob, seatIdsJson, ParameterDirection.Input);
            cmd.Parameters.Add("p_promo_code", OracleDbType.Varchar2, string.IsNullOrWhiteSpace(promoCode) ? DBNull.Value : promoCode.Trim(), ParameterDirection.Input);

            var pBookingId = new OracleParameter("p_booking_id", OracleDbType.Decimal, ParameterDirection.Output);
            var pTotal = new OracleParameter("p_total", OracleDbType.Decimal, ParameterDirection.Output);
            var pExpires = new OracleParameter("p_expires", OracleDbType.TimeStamp, ParameterDirection.Output);

            cmd.Parameters.Add(pBookingId);
            cmd.Parameters.Add(pTotal);
            cmd.Parameters.Add(pExpires);

            try
            {
                await cmd.ExecuteNonQueryAsync();
                transaction.Commit();

                int bookingId = Convert.ToInt32(pBookingId.Value.ToString());
                decimal total = Convert.ToDecimal(pTotal.Value.ToString());
                DateTime expires = Convert.ToDateTime(pExpires.Value.ToString());

                return (bookingId, total, expires);
            }
            catch (OracleException oex)
            {
                try { transaction.Rollback(); } catch { }
                _logger.LogWarning("Oracle error in pr_hold: {Code} - {Msg}", oex.Number, oex.Message);
                throw CineSeatException.FromOracleException(oex);
            }
            catch (Exception ex)
            {
                try { transaction.Rollback(); } catch { }
                _logger.LogError(ex, "Unexpected error in pr_hold");
                throw;
            }
        }

        public async Task ConfirmBookingAsync(int bookingId, string method)
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var transaction = conn.BeginTransaction(IsolationLevel.ReadCommitted);
            using var cmd = conn.CreateCommand();
            cmd.Transaction = transaction;

            cmd.CommandText = "pkg_booking.pr_confirm";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("p_booking_id", OracleDbType.Decimal, bookingId, ParameterDirection.Input);
            cmd.Parameters.Add("p_method", OracleDbType.Varchar2, method, ParameterDirection.Input);

            try
            {
                await cmd.ExecuteNonQueryAsync();
                transaction.Commit();
            }
            catch (OracleException oex)
            {
                try { transaction.Rollback(); } catch { }
                _logger.LogWarning("Oracle error in pr_confirm: {Code} - {Msg}", oex.Number, oex.Message);
                throw CineSeatException.FromOracleException(oex);
            }
            catch (Exception ex)
            {
                try { transaction.Rollback(); } catch { }
                _logger.LogError(ex, "Unexpected error in pr_confirm");
                throw;
            }
        }

        public async Task CancelBookingAsync(int bookingId)
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var transaction = conn.BeginTransaction(IsolationLevel.ReadCommitted);
            using var cmd = conn.CreateCommand();
            cmd.Transaction = transaction;

            cmd.CommandText = "pkg_booking.pr_cancel";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("p_booking_id", OracleDbType.Decimal, bookingId, ParameterDirection.Input);

            try
            {
                await cmd.ExecuteNonQueryAsync();
                transaction.Commit();
            }
            catch (OracleException oex)
            {
                try { transaction.Rollback(); } catch { }
                _logger.LogWarning("Oracle error in pr_cancel: {Code} - {Msg}", oex.Number, oex.Message);
                throw CineSeatException.FromOracleException(oex);
            }
            catch (Exception ex)
            {
                try { transaction.Rollback(); } catch { }
                _logger.LogError(ex, "Unexpected error in pr_cancel");
                throw;
            }
        }

        public async Task ExpireHoldsAsync()
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "pr_expire_holds";
            cmd.CommandType = CommandType.StoredProcedure;

            try
            {
                await cmd.ExecuteNonQueryAsync();
            }
            catch (OracleException oex)
            {
                _logger.LogError(oex, "Oracle error in pr_expire_holds: {Code}", oex.Number);
                throw CineSeatException.FromOracleException(oex);
            }
        }

        #endregion

        #region Đơn hàng & Lịch sử & Báo cáo

        public async Task<Booking?> GetBookingByIdAsync(int bookingId)
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"SELECT b.booking_id, b.user_id, u.full_name, u.email, u.phone,
                                       b.showtime_id, m.title AS movie_title, m.poster_file,
                                       r.room_name, c.cinema_name, s.start_at, s.end_at,
                                       b.promotion_id, p.promo_code,
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
                        Email = reader["email"]?.ToString(),
                        Phone = reader["phone"]?.ToString(),
                        ShowtimeId = Convert.ToInt32(reader["showtime_id"]),
                        MovieTitle = reader["movie_title"]?.ToString() ?? string.Empty,
                        PosterFile = reader["poster_file"]?.ToString(),
                        RoomName = reader["room_name"]?.ToString(),
                        CinemaName = reader["cinema_name"]?.ToString(),
                        StartAt = Convert.ToDateTime(reader["start_at"]),
                        EndAt = Convert.ToDateTime(reader["end_at"]),
                        PromotionId = reader["promotion_id"] == DBNull.Value ? null : Convert.ToInt32(reader["promotion_id"]),
                        PromoCode = reader["promo_code"]?.ToString(),
                        Status = reader["status"]?.ToString() ?? "HELD",
                        TotalAmount = Convert.ToDecimal(reader["total_amount"]),
                        CreatedAt = Convert.ToDateTime(reader["created_at"]),
                        ExpiresAt = Convert.ToDateTime(reader["expires_at"]),
                        PaidAt = reader["paid_at"] == DBNull.Value ? null : Convert.ToDateTime(reader["paid_at"]),
                        SeatCount = Convert.ToInt32(reader["seat_count"])
                    };
                }
            }

            if (booking != null)
            {
                // Load danh sách ghế
                using var cmdSeats = conn.CreateCommand();
                cmdSeats.CommandText = @"SELECT bs.booking_id, bs.seat_id, bs.unit_price, s.row_label, s.seat_number, s.seat_type
                                         FROM booking_seats bs
                                         JOIN seats s ON s.seat_id = bs.seat_id
                                         WHERE bs.booking_id = :p_booking_id
                                         ORDER BY s.row_label, s.seat_number";
                cmdSeats.Parameters.Add("p_booking_id", OracleDbType.Decimal, bookingId, ParameterDirection.Input);

                using var seatReader = await cmdSeats.ExecuteReaderAsync();
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

            cmd.CommandText = @"SELECT b.booking_id, b.user_id, u.full_name,
                                       b.showtime_id, m.title AS movie_title, m.poster_file,
                                       r.room_name, c.cinema_name, s.start_at, s.end_at,
                                       b.promotion_id, p.promo_code,
                                       b.status, b.total_amount, b.created_at, b.expires_at, b.paid_at,
                                       (SELECT COUNT(*) FROM booking_seats bs WHERE bs.booking_id = b.booking_id) AS seat_count
                                FROM bookings b
                                JOIN app_users u ON u.user_id = b.user_id
                                JOIN showtimes s ON s.showtime_id = b.showtime_id
                                JOIN movies m ON m.movie_id = s.movie_id
                                JOIN rooms r ON r.room_id = s.room_id
                                JOIN cinemas c ON c.cinema_id = r.cinema_id
                                LEFT JOIN promotions p ON p.promotion_id = b.promotion_id
                                WHERE b.user_id = :p_user_id
                                ORDER BY b.booking_id DESC";
            cmd.Parameters.Add("p_user_id", OracleDbType.Decimal, userId, ParameterDirection.Input);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var b = new Booking
                {
                    BookingId = Convert.ToInt32(reader["booking_id"]),
                    UserId = Convert.ToInt32(reader["user_id"]),
                    FullName = reader["full_name"]?.ToString() ?? string.Empty,
                    ShowtimeId = Convert.ToInt32(reader["showtime_id"]),
                    MovieTitle = reader["movie_title"]?.ToString() ?? string.Empty,
                    PosterFile = reader["poster_file"]?.ToString(),
                    RoomName = reader["room_name"]?.ToString(),
                    CinemaName = reader["cinema_name"]?.ToString(),
                    StartAt = Convert.ToDateTime(reader["start_at"]),
                    EndAt = Convert.ToDateTime(reader["end_at"]),
                    PromotionId = reader["promotion_id"] == DBNull.Value ? null : Convert.ToInt32(reader["promotion_id"]),
                    PromoCode = reader["promo_code"]?.ToString(),
                    Status = reader["status"]?.ToString() ?? "HELD",
                    TotalAmount = Convert.ToDecimal(reader["total_amount"]),
                    CreatedAt = Convert.ToDateTime(reader["created_at"]),
                    ExpiresAt = Convert.ToDateTime(reader["expires_at"]),
                    PaidAt = reader["paid_at"] == DBNull.Value ? null : Convert.ToDateTime(reader["paid_at"]),
                    SeatCount = Convert.ToInt32(reader["seat_count"])
                };
                list.Add(b);
            }

            // Tải chi tiết ghế cho từng đơn
            foreach (var b in list)
            {
                using var cmdSeats = conn.CreateCommand();
                cmdSeats.CommandText = @"SELECT bs.booking_id, bs.seat_id, bs.unit_price, s.row_label, s.seat_number, s.seat_type
                                         FROM booking_seats bs
                                         JOIN seats s ON s.seat_id = bs.seat_id
                                         WHERE bs.booking_id = :p_booking_id
                                         ORDER BY s.row_label, s.seat_number";
                cmdSeats.Parameters.Add("p_booking_id", OracleDbType.Decimal, b.BookingId, ParameterDirection.Input);

                using var seatReader = await cmdSeats.ExecuteReaderAsync();
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

        public async Task<List<Booking>> GetAllBookingsAsync(string? statusFilter = null)
        {
            var list = new List<Booking>();
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var cmd = conn.CreateCommand();

            var sql = @"SELECT b.booking_id, b.user_id, u.full_name, u.email,
                               b.showtime_id, m.title AS movie_title, m.poster_file,
                               r.room_name, c.cinema_name, s.start_at, s.end_at,
                               b.promotion_id, p.promo_code,
                               b.status, b.total_amount, b.created_at, b.expires_at, b.paid_at,
                               (SELECT COUNT(*) FROM booking_seats bs WHERE bs.booking_id = b.booking_id) AS seat_count
                        FROM bookings b
                        JOIN app_users u ON u.user_id = b.user_id
                        JOIN showtimes s ON s.showtime_id = b.showtime_id
                        JOIN movies m ON m.movie_id = s.movie_id
                        JOIN rooms r ON r.room_id = s.room_id
                        JOIN cinemas c ON c.cinema_id = r.cinema_id
                        LEFT JOIN promotions p ON p.promotion_id = b.promotion_id";

            if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "ALL")
            {
                sql += " WHERE b.status = :p_status";
                cmd.Parameters.Add("p_status", OracleDbType.Varchar2, statusFilter.Trim().ToUpper(), ParameterDirection.Input);
            }

            sql += " ORDER BY b.booking_id DESC";
            cmd.CommandText = sql;

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new Booking
                {
                    BookingId = Convert.ToInt32(reader["booking_id"]),
                    UserId = Convert.ToInt32(reader["user_id"]),
                    FullName = reader["full_name"]?.ToString() ?? string.Empty,
                    Email = reader["email"]?.ToString(),
                    ShowtimeId = Convert.ToInt32(reader["showtime_id"]),
                    MovieTitle = reader["movie_title"]?.ToString() ?? string.Empty,
                    PosterFile = reader["poster_file"]?.ToString(),
                    RoomName = reader["room_name"]?.ToString(),
                    CinemaName = reader["cinema_name"]?.ToString(),
                    StartAt = Convert.ToDateTime(reader["start_at"]),
                    EndAt = Convert.ToDateTime(reader["end_at"]),
                    PromotionId = reader["promotion_id"] == DBNull.Value ? null : Convert.ToInt32(reader["promotion_id"]),
                    PromoCode = reader["promo_code"]?.ToString(),
                    Status = reader["status"]?.ToString() ?? "HELD",
                    TotalAmount = Convert.ToDecimal(reader["total_amount"]),
                    CreatedAt = Convert.ToDateTime(reader["created_at"]),
                    ExpiresAt = Convert.ToDateTime(reader["expires_at"]),
                    PaidAt = reader["paid_at"] == DBNull.Value ? null : Convert.ToDateTime(reader["paid_at"]),
                    SeatCount = Convert.ToInt32(reader["seat_count"])
                });
            }

            return list;
        }

        public async Task<AdminDashboardViewModel> GetAdminDashboardStatsAsync()
        {
            var vm = new AdminDashboardViewModel();
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();

            // 1. KPI 1: Tổng doanh thu vé PAID
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT NVL(SUM(total_amount), 0) FROM bookings WHERE status = 'PAID'";
                vm.TotalRevenue = Convert.ToDecimal(await cmd.ExecuteScalarAsync());
            }

            // 2. KPI 2: Tổng số vé đã bán (SOLD seats)
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM showtime_seats WHERE status = 'SOLD'";
                vm.TotalTicketsSold = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            // 3. KPI 3: Thống kê số lượng đơn theo từng trạng thái
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT status, COUNT(*) AS cnt FROM bookings GROUP BY status";
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string status = reader["status"]?.ToString() ?? "";
                    int cnt = Convert.ToInt32(reader["cnt"]);
                    switch (status.ToUpperInvariant())
                    {
                        case "PAID": vm.PaidCount = cnt; break;
                        case "HELD": vm.HeldCount = cnt; break;
                        case "CANCELLED": vm.CancelledCount = cnt; break;
                        case "EXPIRED": vm.ExpiredCount = cnt; break;
                    }
                }
            }

            // 4. KPI 4: Tổng số phim & suất chiếu đang hoạt động
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM movies WHERE is_active = 1";
                vm.ActiveMoviesCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM showtimes WHERE is_active = 1 AND start_at > LOCALTIMESTAMP";
                vm.ActiveShowtimesCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            // 5. Chart 1: Doanh thu & số vé theo từng phim (Top movies)
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT m.title,
                                           NVL(SUM(b.total_amount), 0) AS revenue,
                                           NVL(COUNT(bs.seat_id), 0) AS tickets_sold
                                    FROM movies m
                                    LEFT JOIN showtimes s ON s.movie_id = m.movie_id
                                    LEFT JOIN bookings b ON b.showtime_id = s.showtime_id AND b.status = 'PAID'
                                    LEFT JOIN booking_seats bs ON bs.booking_id = b.booking_id
                                    WHERE m.is_active = 1
                                    GROUP BY m.movie_id, m.title
                                    ORDER BY revenue DESC";
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    vm.MovieRevenues.Add(new MovieRevenueStat
                    {
                        MovieTitle = reader["title"]?.ToString() ?? string.Empty,
                        Revenue = Convert.ToDecimal(reader["revenue"]),
                        TicketsSold = Convert.ToInt32(reader["tickets_sold"])
                    });
                }
            }

            // 6. Nhật ký kiểm toán BOOKING_AUDIT (20 bản ghi mới nhất)
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT a.audit_id, a.booking_id, a.old_status, a.new_status, a.changed_at, a.description,
                                           u.full_name AS customer_name, m.title AS movie_title
                                    FROM booking_audit a
                                    LEFT JOIN bookings b ON b.booking_id = a.booking_id
                                    LEFT JOIN app_users u ON u.user_id = b.user_id
                                    LEFT JOIN showtimes s ON s.showtime_id = b.showtime_id
                                    LEFT JOIN movies m ON m.movie_id = s.movie_id
                                    ORDER BY a.audit_id DESC
                                    FETCH FIRST 20 ROWS ONLY";

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    vm.RecentAudits.Add(new BookingAudit
                    {
                        AuditId = Convert.ToInt32(reader["audit_id"]),
                        BookingId = Convert.ToInt32(reader["booking_id"]),
                        OldStatus = reader["old_status"] == DBNull.Value ? null : reader["old_status"].ToString(),
                        NewStatus = reader["new_status"]?.ToString(),
                        ChangedAt = Convert.ToDateTime(reader["changed_at"]),
                        Description = reader["description"]?.ToString(),
                        CustomerName = reader["customer_name"]?.ToString(),
                        MovieTitle = reader["movie_title"]?.ToString()
                    });
                }
            }

            // 7. Danh sách 10 đơn đặt vé gần nhất
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT b.booking_id, b.user_id, u.full_name,
                                           b.showtime_id, m.title AS movie_title,
                                           r.room_name, c.cinema_name, s.start_at,
                                           b.status, b.total_amount, b.created_at,
                                           (SELECT COUNT(*) FROM booking_seats bs WHERE bs.booking_id = b.booking_id) AS seat_count
                                    FROM bookings b
                                    JOIN app_users u ON u.user_id = b.user_id
                                    JOIN showtimes s ON s.showtime_id = b.showtime_id
                                    JOIN movies m ON m.movie_id = s.movie_id
                                    JOIN rooms r ON r.room_id = s.room_id
                                    JOIN cinemas c ON c.cinema_id = r.cinema_id
                                    ORDER BY b.booking_id DESC
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
                        RoomName = reader["room_name"]?.ToString(),
                        CinemaName = reader["cinema_name"]?.ToString(),
                        StartAt = Convert.ToDateTime(reader["start_at"]),
                        Status = reader["status"]?.ToString() ?? "HELD",
                        TotalAmount = Convert.ToDecimal(reader["total_amount"]),
                        CreatedAt = Convert.ToDateTime(reader["created_at"]),
                        SeatCount = Convert.ToInt32(reader["seat_count"])
                    });
                }
            }

            return vm;
        }

        #endregion

        #region Quản trị Admin

        public async Task<List<Movie>> GetAllMoviesAdminAsync()
        {
            var movies = new List<Movie>();
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"SELECT m.movie_id, m.title, m.genre, m.duration_min, m.age_rating, m.synopsis, m.poster_file, m.is_active,
                                       (SELECT COUNT(*) FROM showtimes s WHERE s.movie_id = m.movie_id) AS showtime_count,
                                       CASE 
                                           WHEN EXISTS (
                                               SELECT 1 FROM showtimes s 
                                               WHERE s.movie_id = m.movie_id AND s.start_at <= LOCALTIMESTAMP
                                           ) THEN 1 
                                           ELSE 0 
                                       END AS has_premiered
                                FROM movies m
                                ORDER BY m.movie_id DESC";

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
                    IsActive = Convert.ToInt32(reader["is_active"]) == 1,
                    ShowtimeCount = Convert.ToInt32(reader["showtime_count"]),
                    HasPremiered = Convert.ToInt32(reader["has_premiered"]) == 1
                });
            }
            return movies;
        }

        public async Task<List<Showtime>> GetAllShowtimesAdminAsync()
        {
            var showtimes = new List<Showtime>();
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"SELECT s.showtime_id, s.movie_id, m.title AS movie_title, m.poster_file, m.age_rating, m.duration_min,
                                       r.room_id, r.room_name, c.cinema_id, c.cinema_name, c.address,
                                       s.start_at, s.end_at, s.base_price, s.is_active,
                                       (SELECT COUNT(*) FROM showtime_seats ss WHERE ss.showtime_id = s.showtime_id AND ss.status = 'AVAILABLE') AS available_seats
                                FROM showtimes s
                                JOIN movies m ON m.movie_id = s.movie_id
                                JOIN rooms r ON r.room_id = s.room_id
                                JOIN cinemas c ON c.cinema_id = r.cinema_id
                                ORDER BY s.start_at DESC";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                showtimes.Add(new Showtime
                {
                    ShowtimeId = Convert.ToInt32(reader["showtime_id"]),
                    MovieId = Convert.ToInt32(reader["movie_id"]),
                    MovieTitle = reader["movie_title"]?.ToString() ?? string.Empty,
                    PosterFile = reader["poster_file"]?.ToString(),
                    AgeRating = reader["age_rating"]?.ToString() ?? "P",
                    DurationMin = Convert.ToInt32(reader["duration_min"]),
                    RoomId = Convert.ToInt32(reader["room_id"]),
                    RoomName = reader["room_name"]?.ToString() ?? string.Empty,
                    CinemaId = Convert.ToInt32(reader["cinema_id"]),
                    CinemaName = reader["cinema_name"]?.ToString() ?? string.Empty,
                    Address = reader["address"]?.ToString() ?? string.Empty,
                    StartAt = Convert.ToDateTime(reader["start_at"]),
                    EndAt = Convert.ToDateTime(reader["end_at"]),
                    BasePrice = Convert.ToDecimal(reader["base_price"]),
                    AvailableSeats = Convert.ToInt32(reader["available_seats"]),
                    TotalSeats = 40,
                    IsActive = Convert.ToInt32(reader["is_active"]) == 1
                });
            }
            return showtimes;
        }

        public async Task<int> AddMovieAsync(CreateMovieInputModel input)
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var transaction = conn.BeginTransaction(IsolationLevel.ReadCommitted);
            using var cmd = conn.CreateCommand();
            cmd.Transaction = transaction;

            cmd.CommandText = @"INSERT INTO movies(title, genre, duration_min, age_rating, synopsis, poster_file, is_active)
                                VALUES(:p_title, :p_genre, :p_duration, :p_rating, :p_synopsis, :p_poster, 1)
                                RETURNING movie_id INTO :p_new_id";

            cmd.Parameters.Add("p_title", OracleDbType.NVarchar2, input.Title.Trim(), ParameterDirection.Input);
            cmd.Parameters.Add("p_genre", OracleDbType.NVarchar2, input.Genre.Trim(), ParameterDirection.Input);
            cmd.Parameters.Add("p_duration", OracleDbType.Decimal, input.DurationMin, ParameterDirection.Input);
            cmd.Parameters.Add("p_rating", OracleDbType.Varchar2, input.AgeRating.Trim().ToUpper(), ParameterDirection.Input);
            cmd.Parameters.Add("p_synopsis", OracleDbType.NVarchar2, string.IsNullOrWhiteSpace(input.Synopsis) ? DBNull.Value : input.Synopsis.Trim(), ParameterDirection.Input);
            cmd.Parameters.Add("p_poster", OracleDbType.Varchar2, string.IsNullOrWhiteSpace(input.PosterFile) ? "default-poster.jpg" : input.PosterFile.Trim(), ParameterDirection.Input);

            var pNewId = new OracleParameter("p_new_id", OracleDbType.Decimal, ParameterDirection.Output);
            cmd.Parameters.Add(pNewId);

            try
            {
                await cmd.ExecuteNonQueryAsync();
                transaction.Commit();
                return Convert.ToInt32(pNewId.Value.ToString());
            }
            catch (Exception ex)
            {
                try { transaction.Rollback(); } catch { }
                _logger.LogError(ex, "Error adding movie");
                throw;
            }
        }

        public async Task ToggleMovieActiveAsync(int movieId)
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var transaction = conn.BeginTransaction(IsolationLevel.ReadCommitted);
            using var cmd = conn.CreateCommand();
            cmd.Transaction = transaction;

            cmd.CommandText = @"UPDATE movies
                                SET is_active = CASE WHEN is_active = 1 THEN 0 ELSE 1 END
                                WHERE movie_id = :p_id";
            cmd.Parameters.Add("p_id", OracleDbType.Decimal, movieId, ParameterDirection.Input);

            try
            {
                await cmd.ExecuteNonQueryAsync();
                transaction.Commit();
            }
            catch (Exception ex)
            {
                try { transaction.Rollback(); } catch { }
                _logger.LogError(ex, "Error toggling movie active");
                throw;
            }
        }

        public async Task<bool> HasMoviePremieredAsync(int movieId)
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var cmd = conn.CreateCommand();
            // Phim coi là đã công chiếu / đã lên lịch chiếu nếu đã có suất chiếu trong hệ thống
            cmd.CommandText = @"SELECT COUNT(*) FROM showtimes WHERE movie_id = :p_id";
            cmd.Parameters.Add("p_id", OracleDbType.Decimal, movieId, ParameterDirection.Input);
            int count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return count > 0;
        }

        public async Task<int> CreateShowtimeAsync(CreateShowtimeInputModel input)
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var transaction = conn.BeginTransaction(IsolationLevel.ReadCommitted);
            try
            {
                // Bước 1: Tạo bản ghi SHOWTIMES
                int newShowtimeId = 0;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = transaction;
                    cmd.CommandText = @"INSERT INTO showtimes(movie_id, room_id, start_at, end_at, base_price, is_active)
                                        VALUES(:p_movie_id, :p_room_id, :p_start, :p_end, :p_price, 1)
                                        RETURNING showtime_id INTO :p_new_id";

                    cmd.Parameters.Add("p_movie_id", OracleDbType.Decimal, input.MovieId, ParameterDirection.Input);
                    cmd.Parameters.Add("p_room_id", OracleDbType.Decimal, input.RoomId, ParameterDirection.Input);
                    cmd.Parameters.Add("p_start", OracleDbType.TimeStamp, input.StartAt, ParameterDirection.Input);
                    cmd.Parameters.Add("p_end", OracleDbType.TimeStamp, input.EndAt, ParameterDirection.Input);
                    cmd.Parameters.Add("p_price", OracleDbType.Decimal, input.BasePrice, ParameterDirection.Input);

                    var pNewId = new OracleParameter("p_new_id", OracleDbType.Decimal, ParameterDirection.Output);
                    cmd.Parameters.Add(pNewId);

                    await cmd.ExecuteNonQueryAsync();
                    newShowtimeId = Convert.ToInt32(pNewId.Value.ToString());
                }

                // Bước 2: TỰ ĐỘNG SINH 40 GHẾ AVAILABLE VÀO BẢNG SHOWTIME_SEATS THEO ROOM_ID
                using (var cmdSeats = conn.CreateCommand())
                {
                    cmdSeats.Transaction = transaction;
                    cmdSeats.CommandText = @"INSERT INTO showtime_seats(showtime_id, seat_id, status)
                                             SELECT :p_showtime_id, seat_id, 'AVAILABLE'
                                             FROM seats
                                             WHERE room_id = :p_room_id";

                    cmdSeats.Parameters.Add("p_showtime_id", OracleDbType.Decimal, newShowtimeId, ParameterDirection.Input);
                    cmdSeats.Parameters.Add("p_room_id", OracleDbType.Decimal, input.RoomId, ParameterDirection.Input);

                    await cmdSeats.ExecuteNonQueryAsync();
                }

                transaction.Commit();
                return newShowtimeId;
            }
            catch (Exception ex)
            {
                try { transaction.Rollback(); } catch { }
                _logger.LogError(ex, "Error creating showtime and populating seats");
                throw;
            }
        }

        public async Task UpdateMovieAsync(UpdateMovieInputModel input)
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var transaction = conn.BeginTransaction(IsolationLevel.ReadCommitted);
            using var cmd = conn.CreateCommand();
            cmd.Transaction = transaction;

            cmd.CommandText = @"UPDATE movies
                                SET title = :p_title,
                                    genre = :p_genre,
                                    duration_min = :p_duration,
                                    age_rating = :p_rating,
                                    synopsis = :p_synopsis,
                                    poster_file = :p_poster,
                                    is_active = :p_active
                                WHERE movie_id = :p_id";

            cmd.Parameters.Add("p_title", OracleDbType.NVarchar2, input.Title.Trim(), ParameterDirection.Input);
            cmd.Parameters.Add("p_genre", OracleDbType.NVarchar2, input.Genre.Trim(), ParameterDirection.Input);
            cmd.Parameters.Add("p_duration", OracleDbType.Decimal, input.DurationMin, ParameterDirection.Input);
            cmd.Parameters.Add("p_rating", OracleDbType.Varchar2, input.AgeRating.Trim().ToUpper(), ParameterDirection.Input);
            cmd.Parameters.Add("p_synopsis", OracleDbType.NVarchar2, string.IsNullOrWhiteSpace(input.Synopsis) ? DBNull.Value : input.Synopsis.Trim(), ParameterDirection.Input);
            cmd.Parameters.Add("p_poster", OracleDbType.Varchar2, string.IsNullOrWhiteSpace(input.PosterFile) ? "default-poster.jpg" : input.PosterFile.Trim(), ParameterDirection.Input);
            cmd.Parameters.Add("p_active", OracleDbType.Decimal, input.IsActive ? 1 : 0, ParameterDirection.Input);
            cmd.Parameters.Add("p_id", OracleDbType.Decimal, input.MovieId, ParameterDirection.Input);

            try
            {
                await cmd.ExecuteNonQueryAsync();
                transaction.Commit();
            }
            catch (Exception ex)
            {
                try { transaction.Rollback(); } catch { }
                _logger.LogError(ex, "Error updating movie #{Id}", input.MovieId);
                throw;
            }
        }

        public async Task<bool> DeleteMovieAsync(int movieId)
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var transaction = conn.BeginTransaction(IsolationLevel.ReadCommitted);

            // Kiểm tra xem phim đã có suất chiếu nào chưa
            int showtimeCount = 0;
            using (var cmdCheck = conn.CreateCommand())
            {
                cmdCheck.Transaction = transaction;
                cmdCheck.CommandText = "SELECT COUNT(*) FROM showtimes WHERE movie_id = :p_id";
                cmdCheck.Parameters.Add("p_id", OracleDbType.Decimal, movieId, ParameterDirection.Input);
                showtimeCount = Convert.ToInt32(await cmdCheck.ExecuteScalarAsync());
            }

            if (showtimeCount > 0)
            {
                // Nếu đã có suất chiếu, chuyển sang trạng thái ngừng chiếu (soft delete) để không vướng ràng buộc FK
                using (var cmdSoft = conn.CreateCommand())
                {
                    cmdSoft.Transaction = transaction;
                    cmdSoft.CommandText = "UPDATE movies SET is_active = 0 WHERE movie_id = :p_id";
                    cmdSoft.Parameters.Add("p_id", OracleDbType.Decimal, movieId, ParameterDirection.Input);
                    await cmdSoft.ExecuteNonQueryAsync();
                }
                transaction.Commit();
                return false; // Soft deleted
            }
            else
            {
                // Nếu chưa có suất chiếu nào, xóa hẳn khỏi CSDL
                using (var cmdDel = conn.CreateCommand())
                {
                    cmdDel.Transaction = transaction;
                    cmdDel.CommandText = "DELETE FROM movies WHERE movie_id = :p_id";
                    cmdDel.Parameters.Add("p_id", OracleDbType.Decimal, movieId, ParameterDirection.Input);
                    await cmdDel.ExecuteNonQueryAsync();
                }
                transaction.Commit();
                return true; // Hard deleted
            }
        }

        public async Task<bool> CheckShowtimeOverlapAsync(int roomId, DateTime startAt, DateTime endAt, int excludeShowtimeId = 0)
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"SELECT COUNT(*) FROM showtimes
                                WHERE room_id = :p_room_id
                                  AND is_active = 1
                                  AND (:p_exclude_id = 0 OR showtime_id != :p_exclude_id)
                                  AND start_at < :p_end_at
                                  AND end_at > :p_start_at";

            cmd.Parameters.Add("p_room_id", OracleDbType.Decimal, roomId, ParameterDirection.Input);
            cmd.Parameters.Add("p_exclude_id", OracleDbType.Decimal, excludeShowtimeId, ParameterDirection.Input);
            cmd.Parameters.Add("p_end_at", OracleDbType.TimeStamp, endAt, ParameterDirection.Input);
            cmd.Parameters.Add("p_start_at", OracleDbType.TimeStamp, startAt, ParameterDirection.Input);

            int count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return count > 0;
        }

        public async Task UpdateShowtimeAsync(UpdateShowtimeInputModel input)
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var transaction = conn.BeginTransaction(IsolationLevel.ReadCommitted);
            using var cmd = conn.CreateCommand();
            cmd.Transaction = transaction;

            cmd.CommandText = @"UPDATE showtimes
                                SET start_at = :p_start,
                                    end_at = :p_end,
                                    base_price = :p_price,
                                    is_active = :p_active
                                WHERE showtime_id = :p_id";

            cmd.Parameters.Add("p_start", OracleDbType.TimeStamp, input.StartAt, ParameterDirection.Input);
            cmd.Parameters.Add("p_end", OracleDbType.TimeStamp, input.EndAt, ParameterDirection.Input);
            cmd.Parameters.Add("p_price", OracleDbType.Decimal, input.BasePrice, ParameterDirection.Input);
            cmd.Parameters.Add("p_active", OracleDbType.Decimal, input.IsActive ? 1 : 0, ParameterDirection.Input);
            cmd.Parameters.Add("p_id", OracleDbType.Decimal, input.ShowtimeId, ParameterDirection.Input);

            try
            {
                await cmd.ExecuteNonQueryAsync();
                transaction.Commit();
            }
            catch (Exception ex)
            {
                try { transaction.Rollback(); } catch { }
                _logger.LogError(ex, "Error updating showtime #{Id}", input.ShowtimeId);
                throw;
            }
        }

        public async Task DeleteShowtimeAsync(int showtimeId)
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var transaction = conn.BeginTransaction(IsolationLevel.ReadCommitted);

            // Kiểm tra xem đã có ghế HELD hoặc SOLD chưa
            using (var cmdCheck = conn.CreateCommand())
            {
                cmdCheck.Transaction = transaction;
                cmdCheck.CommandText = "SELECT COUNT(*) FROM showtime_seats WHERE showtime_id = :p_id AND status IN ('HELD', 'SOLD')";
                cmdCheck.Parameters.Add("p_id", OracleDbType.Decimal, showtimeId, ParameterDirection.Input);
                int activeSeats = Convert.ToInt32(await cmdCheck.ExecuteScalarAsync());
                if (activeSeats > 0)
                {
                    throw new InvalidOperationException("Không thể xóa suất chiếu đã có vé đang giữ hoặc đã bán. Bạn có thể chuyển sang trạng thái ngừng hoạt động.");
                }
            }

            // Kiểm tra xem có đơn đặt vé nào chưa
            using (var cmdCheckB = conn.CreateCommand())
            {
                cmdCheckB.Transaction = transaction;
                cmdCheckB.CommandText = "SELECT COUNT(*) FROM bookings WHERE showtime_id = :p_id";
                cmdCheckB.Parameters.Add("p_id", OracleDbType.Decimal, showtimeId, ParameterDirection.Input);
                int bookingsCount = Convert.ToInt32(await cmdCheckB.ExecuteScalarAsync());
                if (bookingsCount > 0)
                {
                    throw new InvalidOperationException("Không thể xóa suất chiếu đã có lịch sử đặt vé. Vui lòng chuyển sang trạng thái ngừng hoạt động.");
                }
            }

            try
            {
                // Xóa ghế của suất chiếu trong SHOWTIME_SEATS
                using (var cmdDelSeats = conn.CreateCommand())
                {
                    cmdDelSeats.Transaction = transaction;
                    cmdDelSeats.CommandText = "DELETE FROM showtime_seats WHERE showtime_id = :p_id";
                    cmdDelSeats.Parameters.Add("p_id", OracleDbType.Decimal, showtimeId, ParameterDirection.Input);
                    await cmdDelSeats.ExecuteNonQueryAsync();
                }

                // Xóa suất chiếu trong SHOWTIMES
                using (var cmdDelSt = conn.CreateCommand())
                {
                    cmdDelSt.Transaction = transaction;
                    cmdDelSt.CommandText = "DELETE FROM showtimes WHERE showtime_id = :p_id";
                    cmdDelSt.Parameters.Add("p_id", OracleDbType.Decimal, showtimeId, ParameterDirection.Input);
                    await cmdDelSt.ExecuteNonQueryAsync();
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                try { transaction.Rollback(); } catch { }
                _logger.LogError(ex, "Error deleting showtime #{Id}", showtimeId);
                throw;
            }
        }

        public async Task<int> AddCinemaAsync(CreateCinemaInputModel input)
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var transaction = conn.BeginTransaction(IsolationLevel.ReadCommitted);
            using var cmd = conn.CreateCommand();
            cmd.Transaction = transaction;

            cmd.CommandText = @"INSERT INTO cinemas(cinema_id, cinema_name, address)
                                VALUES((SELECT NVL(MAX(cinema_id), 0) + 1 FROM cinemas), :p_name, :p_address)
                                RETURNING cinema_id INTO :p_new_id";

            cmd.Parameters.Add("p_name", OracleDbType.NVarchar2, input.CinemaName.Trim(), ParameterDirection.Input);
            cmd.Parameters.Add("p_address", OracleDbType.NVarchar2, input.Address.Trim(), ParameterDirection.Input);

            var pNewId = new OracleParameter("p_new_id", OracleDbType.Decimal, ParameterDirection.Output);
            cmd.Parameters.Add(pNewId);

            try
            {
                await cmd.ExecuteNonQueryAsync();
                transaction.Commit();
                return Convert.ToInt32(pNewId.Value.ToString());
            }
            catch (Exception ex)
            {
                try { transaction.Rollback(); } catch { }
                _logger.LogError(ex, "Error adding cinema");
                throw;
            }
        }

        public async Task UpdateCinemaAsync(UpdateCinemaInputModel input)
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var transaction = conn.BeginTransaction(IsolationLevel.ReadCommitted);
            using var cmd = conn.CreateCommand();
            cmd.Transaction = transaction;

            cmd.CommandText = @"UPDATE cinemas
                                SET cinema_name = :p_name,
                                    address = :p_address
                                WHERE cinema_id = :p_id";

            cmd.Parameters.Add("p_name", OracleDbType.NVarchar2, input.CinemaName.Trim(), ParameterDirection.Input);
            cmd.Parameters.Add("p_address", OracleDbType.NVarchar2, input.Address.Trim(), ParameterDirection.Input);
            cmd.Parameters.Add("p_id", OracleDbType.Decimal, input.CinemaId, ParameterDirection.Input);

            try
            {
                await cmd.ExecuteNonQueryAsync();
                transaction.Commit();
            }
            catch (Exception ex)
            {
                try { transaction.Rollback(); } catch { }
                _logger.LogError(ex, "Error updating cinema #{Id}", input.CinemaId);
                throw;
            }
        }

        public async Task DeleteCinemaAsync(int cinemaId)
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var transaction = conn.BeginTransaction(IsolationLevel.ReadCommitted);

            // Kiểm tra xem cụm rạp đã có suất chiếu nào chưa
            using (var cmdCheck = conn.CreateCommand())
            {
                cmdCheck.Transaction = transaction;
                cmdCheck.CommandText = "SELECT COUNT(*) FROM showtimes s JOIN rooms r ON r.room_id = s.room_id WHERE r.cinema_id = :p_id";
                cmdCheck.Parameters.Add("p_id", OracleDbType.Decimal, cinemaId, ParameterDirection.Input);
                int count = Convert.ToInt32(await cmdCheck.ExecuteScalarAsync());
                if (count > 0)
                {
                    throw new InvalidOperationException("Không thể xóa cụm rạp đã có lịch sử suất chiếu.");
                }
            }

            try
            {
                // Xóa ghế của các phòng thuộc rạp này
                using (var cmdDelSeats = conn.CreateCommand())
                {
                    cmdDelSeats.Transaction = transaction;
                    cmdDelSeats.CommandText = "DELETE FROM seats WHERE room_id IN (SELECT room_id FROM rooms WHERE cinema_id = :p_id)";
                    cmdDelSeats.Parameters.Add("p_id", OracleDbType.Decimal, cinemaId, ParameterDirection.Input);
                    await cmdDelSeats.ExecuteNonQueryAsync();
                }

                // Xóa các phòng
                using (var cmdDelRooms = conn.CreateCommand())
                {
                    cmdDelRooms.Transaction = transaction;
                    cmdDelRooms.CommandText = "DELETE FROM rooms WHERE cinema_id = :p_id";
                    cmdDelRooms.Parameters.Add("p_id", OracleDbType.Decimal, cinemaId, ParameterDirection.Input);
                    await cmdDelRooms.ExecuteNonQueryAsync();
                }

                // Xóa cụm rạp
                using (var cmdDelC = conn.CreateCommand())
                {
                    cmdDelC.Transaction = transaction;
                    cmdDelC.CommandText = "DELETE FROM cinemas WHERE cinema_id = :p_id";
                    cmdDelC.Parameters.Add("p_id", OracleDbType.Decimal, cinemaId, ParameterDirection.Input);
                    await cmdDelC.ExecuteNonQueryAsync();
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                try { transaction.Rollback(); } catch { }
                _logger.LogError(ex, "Error deleting cinema #{Id}", cinemaId);
                throw;
            }
        }

        public async Task<int> AddRoomAsync(CreateRoomInputModel input)
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var transaction = conn.BeginTransaction(IsolationLevel.ReadCommitted);
            int newRoomId = 0;

            try
            {
                // 1. Thêm phòng chiếu mới vào bảng ROOMS
                using (var cmdRoom = conn.CreateCommand())
                {
                    cmdRoom.Transaction = transaction;
                    cmdRoom.CommandText = @"INSERT INTO rooms(room_id, cinema_id, room_name)
                                            VALUES((SELECT NVL(MAX(room_id), 0) + 1 FROM rooms), :p_cinema_id, :p_room_name)
                                            RETURNING room_id INTO :p_new_id";

                    cmdRoom.Parameters.Add("p_cinema_id", OracleDbType.Decimal, input.CinemaId, ParameterDirection.Input);
                    cmdRoom.Parameters.Add("p_room_name", OracleDbType.NVarchar2, input.RoomName.Trim(), ParameterDirection.Input);

                    var pNewId = new OracleParameter("p_new_id", OracleDbType.Decimal, ParameterDirection.Output);
                    cmdRoom.Parameters.Add(pNewId);

                    await cmdRoom.ExecuteNonQueryAsync();
                    newRoomId = Convert.ToInt32(pNewId.Value.ToString());
                }

                // 2. Lấy max seat_id hiện tại để sinh 40 ghế không bị đụng sequence
                int baseSeatId = 0;
                using (var cmdMaxSeat = conn.CreateCommand())
                {
                    cmdMaxSeat.Transaction = transaction;
                    cmdMaxSeat.CommandText = "SELECT NVL(MAX(seat_id), 0) FROM seats";
                    baseSeatId = Convert.ToInt32(await cmdMaxSeat.ExecuteScalarAsync());
                }

                // 3. Tự động sinh 40 ghế vật lý (Hàng A-C STANDARD, Hàng D-E VIP)
                string[] rows = { "A", "B", "C", "D", "E" };
                int currentSeatId = baseSeatId;

                foreach (var row in rows)
                {
                    string seatType = (row == "D" || row == "E") ? "VIP" : "STANDARD";
                    for (int seatNum = 1; seatNum <= 8; seatNum++)
                    {
                        currentSeatId++;
                        using var cmdSeat = conn.CreateCommand();
                        cmdSeat.Transaction = transaction;
                        cmdSeat.CommandText = @"INSERT INTO seats(seat_id, room_id, row_label, seat_number, seat_type)
                                                VALUES(:p_seat_id, :p_room_id, :p_row, :p_num, :p_type)";

                        cmdSeat.Parameters.Add("p_seat_id", OracleDbType.Decimal, currentSeatId, ParameterDirection.Input);
                        cmdSeat.Parameters.Add("p_room_id", OracleDbType.Decimal, newRoomId, ParameterDirection.Input);
                        cmdSeat.Parameters.Add("p_row", OracleDbType.Varchar2, row, ParameterDirection.Input);
                        cmdSeat.Parameters.Add("p_num", OracleDbType.Decimal, seatNum, ParameterDirection.Input);
                        cmdSeat.Parameters.Add("p_type", OracleDbType.Varchar2, seatType, ParameterDirection.Input);

                        await cmdSeat.ExecuteNonQueryAsync();
                    }
                }

                transaction.Commit();
                return newRoomId;
            }
            catch (Exception ex)
            {
                try { transaction.Rollback(); } catch { }
                _logger.LogError(ex, "Error adding room and generating seats");
                throw;
            }
        }

        public async Task DeleteRoomAsync(int roomId)
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var transaction = conn.BeginTransaction(IsolationLevel.ReadCommitted);

            // Kiểm tra xem phòng đã có suất chiếu nào chưa
            using (var cmdCheck = conn.CreateCommand())
            {
                cmdCheck.Transaction = transaction;
                cmdCheck.CommandText = "SELECT COUNT(*) FROM showtimes WHERE room_id = :p_id";
                cmdCheck.Parameters.Add("p_id", OracleDbType.Decimal, roomId, ParameterDirection.Input);
                int count = Convert.ToInt32(await cmdCheck.ExecuteScalarAsync());
                if (count > 0)
                {
                    throw new InvalidOperationException("Không thể xóa phòng chiếu đã có lịch sử suất chiếu.");
                }
            }

            try
            {
                // Xóa ghế của phòng này trong SEATS
                using (var cmdDelSeats = conn.CreateCommand())
                {
                    cmdDelSeats.Transaction = transaction;
                    cmdDelSeats.CommandText = "DELETE FROM seats WHERE room_id = :p_id";
                    cmdDelSeats.Parameters.Add("p_id", OracleDbType.Decimal, roomId, ParameterDirection.Input);
                    await cmdDelSeats.ExecuteNonQueryAsync();
                }

                // Xóa phòng trong ROOMS
                using (var cmdDelR = conn.CreateCommand())
                {
                    cmdDelR.Transaction = transaction;
                    cmdDelR.CommandText = "DELETE FROM rooms WHERE room_id = :p_id";
                    cmdDelR.Parameters.Add("p_id", OracleDbType.Decimal, roomId, ParameterDirection.Input);
                    await cmdDelR.ExecuteNonQueryAsync();
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                try { transaction.Rollback(); } catch { }
                _logger.LogError(ex, "Error deleting room #{Id}", roomId);
                throw;
            }
        }

        public async Task<List<PromotionAdminModel>> GetAllPromotionsAdminAsync()
        {
            var list = new List<PromotionAdminModel>();
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"SELECT p.promotion_id, p.promo_code, p.discount_pct, p.valid_until, p.is_active,
                                       (SELECT COUNT(*) FROM bookings b WHERE b.promotion_id = p.promotion_id) AS used_count
                                FROM promotions p
                                ORDER BY p.valid_until DESC";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new PromotionAdminModel
                {
                    PromotionId = Convert.ToInt32(reader["promotion_id"]),
                    PromoCode = reader["promo_code"]?.ToString() ?? string.Empty,
                    DiscountPct = Convert.ToDecimal(reader["discount_pct"]),
                    ValidUntil = Convert.ToDateTime(reader["valid_until"]),
                    IsActive = Convert.ToInt32(reader["is_active"]) == 1,
                    UsedCount = Convert.ToInt32(reader["used_count"])
                });
            }

            return list;
        }

        public async Task<int> AddPromotionAsync(CreatePromotionInputModel input)
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var transaction = conn.BeginTransaction(IsolationLevel.ReadCommitted);
            using var cmd = conn.CreateCommand();
            cmd.Transaction = transaction;

            cmd.CommandText = @"INSERT INTO promotions(promotion_id, promo_code, discount_pct, valid_until, is_active)
                                VALUES((SELECT NVL(MAX(promotion_id), 0) + 1 FROM promotions), :p_code, :p_pct, :p_valid, 1)
                                RETURNING promotion_id INTO :p_new_id";

            cmd.Parameters.Add("p_code", OracleDbType.Varchar2, input.PromoCode.Trim().ToUpperInvariant(), ParameterDirection.Input);
            cmd.Parameters.Add("p_pct", OracleDbType.Decimal, input.DiscountPct, ParameterDirection.Input);
            cmd.Parameters.Add("p_valid", OracleDbType.Date, input.ValidUntil.Date, ParameterDirection.Input);

            var pNewId = new OracleParameter("p_new_id", OracleDbType.Decimal, ParameterDirection.Output);
            cmd.Parameters.Add(pNewId);

            try
            {
                await cmd.ExecuteNonQueryAsync();
                transaction.Commit();
                return Convert.ToInt32(pNewId.Value.ToString());
            }
            catch (Exception ex)
            {
                try { transaction.Rollback(); } catch { }
                _logger.LogError(ex, "Error adding promotion");
                throw;
            }
        }

        public async Task UpdatePromotionAsync(UpdatePromotionInputModel input)
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var transaction = conn.BeginTransaction(IsolationLevel.ReadCommitted);
            using var cmd = conn.CreateCommand();
            cmd.Transaction = transaction;

            cmd.CommandText = @"UPDATE promotions
                                SET promo_code = :p_code,
                                    discount_pct = :p_pct,
                                    valid_until = :p_valid,
                                    is_active = :p_active
                                WHERE promotion_id = :p_id";

            cmd.Parameters.Add("p_code", OracleDbType.Varchar2, input.PromoCode.Trim().ToUpperInvariant(), ParameterDirection.Input);
            cmd.Parameters.Add("p_pct", OracleDbType.Decimal, input.DiscountPct, ParameterDirection.Input);
            cmd.Parameters.Add("p_valid", OracleDbType.Date, input.ValidUntil.Date, ParameterDirection.Input);
            cmd.Parameters.Add("p_active", OracleDbType.Decimal, input.IsActive ? 1 : 0, ParameterDirection.Input);
            cmd.Parameters.Add("p_id", OracleDbType.Decimal, input.PromotionId, ParameterDirection.Input);

            try
            {
                await cmd.ExecuteNonQueryAsync();
                transaction.Commit();
            }
            catch (Exception ex)
            {
                try { transaction.Rollback(); } catch { }
                _logger.LogError(ex, "Error updating promotion #{Id}", input.PromotionId);
                throw;
            }
        }

        public async Task TogglePromotionActiveAsync(int promotionId)
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var transaction = conn.BeginTransaction(IsolationLevel.ReadCommitted);
            using var cmd = conn.CreateCommand();
            cmd.Transaction = transaction;

            cmd.CommandText = @"UPDATE promotions
                                SET is_active = CASE WHEN is_active = 1 THEN 0 ELSE 1 END
                                WHERE promotion_id = :p_id";
            cmd.Parameters.Add("p_id", OracleDbType.Decimal, promotionId, ParameterDirection.Input);

            try
            {
                await cmd.ExecuteNonQueryAsync();
                transaction.Commit();
            }
            catch (Exception ex)
            {
                try { transaction.Rollback(); } catch { }
                _logger.LogError(ex, "Error toggling promotion active");
                throw;
            }
        }

        public async Task DeletePromotionAsync(int promotionId)
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync();
            using var transaction = conn.BeginTransaction(IsolationLevel.ReadCommitted);

            // Kiểm tra xem mã khuyến mãi đã được đơn nào sử dụng chưa
            int usedCount = 0;
            using (var cmdCheck = conn.CreateCommand())
            {
                cmdCheck.Transaction = transaction;
                cmdCheck.CommandText = "SELECT COUNT(*) FROM bookings WHERE promotion_id = :p_id";
                cmdCheck.Parameters.Add("p_id", OracleDbType.Decimal, promotionId, ParameterDirection.Input);
                usedCount = Convert.ToInt32(await cmdCheck.ExecuteScalarAsync());
            }

            if (usedCount > 0)
            {
                // Nếu đã được sử dụng, chỉ tắt trạng thái hoạt động (is_active = 0)
                using (var cmdSoft = conn.CreateCommand())
                {
                    cmdSoft.Transaction = transaction;
                    cmdSoft.CommandText = "UPDATE promotions SET is_active = 0 WHERE promotion_id = :p_id";
                    cmdSoft.Parameters.Add("p_id", OracleDbType.Decimal, promotionId, ParameterDirection.Input);
                    await cmdSoft.ExecuteNonQueryAsync();
                }
            }
            else
            {
                // Nếu chưa từng được sử dụng, xóa hẳn
                using (var cmdDel = conn.CreateCommand())
                {
                    cmdDel.Transaction = transaction;
                    cmdDel.CommandText = "DELETE FROM promotions WHERE promotion_id = :p_id";
                    cmdDel.Parameters.Add("p_id", OracleDbType.Decimal, promotionId, ParameterDirection.Input);
                    await cmdDel.ExecuteNonQueryAsync();
                }
            }

            transaction.Commit();
        }

        #endregion
    }
}
