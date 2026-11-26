using System.Data;
using System.Text.Json;
using CineSeat.Web.Data;
using CineSeat.Web.Exceptions;
using CineSeat.Web.Models;
using CineSeat.Web.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Oracle.ManagedDataAccess.Client;
using Xunit;

namespace CineSeat.Tests;

public class CineSeatBusinessTests
{
    private const string ConnectionString =
        "Data Source=localhost:1521/FREEPDB1;User Id=CINESEAT;Password=CineSeat123;Pooling=true;Min Pool Size=1;Max Pool Size=20;";

    private readonly IConfiguration _config;
    private readonly IDbConnectionFactory _factory;

    public CineSeatBusinessTests()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "ConnectionStrings:OracleDb", ConnectionString }
        };

        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _factory = new OracleConnectionFactory(_config);
    }

    private ICineSeatService CreateService()
    {
        return new CineSeatService(_factory, NullLogger<CineSeatService>.Instance);
    }

    private async Task<int> GetAvailableSeatAsync(int showtimeId)
    {
        using var conn = await _factory.CreateOpenConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT seat_id 
              FROM showtime_seats 
             WHERE showtime_id = :showtime_id 
               AND status = 'AVAILABLE' 
               AND ROWNUM = 1";
        var p = cmd.CreateParameter();
        p.ParameterName = "showtime_id";
        p.Value = showtimeId;
        cmd.Parameters.Add(p);

        var result = await cmd.ExecuteScalarAsync();
        if (result == null || result == DBNull.Value)
        {
            throw new InvalidOperationException($"Không tìm thấy ghế trống cho suất chiếu {showtimeId}");
        }
        return Convert.ToInt32(result);
    }

    private async Task<int> GetUpcomingShowtimeAsync()
    {
        using var conn = await _factory.CreateOpenConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT showtime_id 
              FROM showtimes 
             WHERE is_active = 1 
               AND start_at > LOCALTIMESTAMP 
             ORDER BY showtime_id";
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            int showtimeId = reader.GetInt32(0);
            // Kiểm tra xem suất có ghế AVAILABLE không
            using var cmdCheck = conn.CreateCommand();
            cmdCheck.CommandText = "SELECT COUNT(*) FROM showtime_seats WHERE showtime_id = :st_id AND status = 'AVAILABLE'";
            var p = cmdCheck.CreateParameter();
            p.ParameterName = "st_id";
            p.Value = showtimeId;
            cmdCheck.Parameters.Add(p);
            long count = Convert.ToInt64(await cmdCheck.ExecuteScalarAsync());
            if (count > 0)
            {
                return showtimeId;
            }
        }
        throw new InvalidOperationException("Không tìm thấy suất chiếu hợp lệ có ghế trống");
    }

    private static decimal RoundToThousand(decimal val)
    {
        return Math.Round(val / 1000m, MidpointRounding.AwayFromZero) * 1000m;
    }

    /// <summary>
    /// TC01: Tính giá vé (suất thường, suất tối >= 18h +20%, cuối tuần +10%, áp dụng mã SALE10)
    /// </summary>
    [Fact]
    public async Task TC01_CalculateTicketPrice_BusinessRules_Succeeds()
    {
        var service = CreateService();

        // Lấy danh sách suất chiếu từ cơ sở dữ liệu
        using var conn = await _factory.CreateOpenConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT showtime_id, base_price,
                   TO_NUMBER(TO_CHAR(start_at, 'HH24')) as start_hour,
                   TO_CHAR(start_at, 'DY', 'NLS_DATE_LANGUAGE=ENGLISH') as day_of_week
              FROM showtimes
             WHERE is_active = 1 AND start_at > LOCALTIMESTAMP
             ORDER BY showtime_id";

        using var reader = await cmd.ExecuteReaderAsync();
        var showtimes = new List<(int Id, decimal BasePrice, int Hour, string Day)>();
        while (await reader.ReadAsync())
        {
            showtimes.Add((
                reader.GetInt32(0),
                reader.GetDecimal(1),
                Convert.ToInt32(reader.GetValue(2)),
                reader.GetString(3).ToUpperInvariant()
            ));
        }

        Assert.NotEmpty(showtimes);

        // 1. Kiểm tra tính giá cơ bản (1 ghế, không mã giảm)
        var st = showtimes[0];
        decimal expectedMultiplier = 1.0m;
        if (st.Hour >= 18) expectedMultiplier += 0.20m;
        if (st.Day is "SAT" or "SUN") expectedMultiplier += 0.10m;

        decimal calculatedPrice = await service.CalculateTotalPriceAsync(st.Id, 1, null);
        decimal expectedPrice = RoundToThousand(st.BasePrice * expectedMultiplier * 1 * 1.0m);

        Assert.Equal(expectedPrice, calculatedPrice);

        // 2. Kiểm tra áp dụng mã giảm giá SALE10 (giảm 10%)
        decimal priceWithSale10 = await service.CalculateTotalPriceAsync(st.Id, 1, "SALE10");
        decimal expectedSale10Price = RoundToThousand(st.BasePrice * expectedMultiplier * 1 * 0.90m);

        Assert.Equal(expectedSale10Price, priceWithSale10);

        // 3. Kiểm tra tính giá cho nhiều ghế (ví dụ: 3 ghế)
        decimal priceFor3Seats = await service.CalculateTotalPriceAsync(st.Id, 3, "SALE10");
        decimal expected3SeatsPrice = RoundToThousand(st.BasePrice * expectedMultiplier * 3 * 0.90m);

        Assert.Equal(expected3SeatsPrice, priceFor3Seats);

        // 4. Nếu có suất tối (hour >= 18), xác minh phụ thu 20%
        var eveningShowtime = showtimes.FirstOrDefault(s => s.Hour >= 18);
        if (eveningShowtime.Id > 0)
        {
            decimal eveningPrice = await service.CalculateTotalPriceAsync(eveningShowtime.Id, 1, null);
            decimal eveningMult = 1.0m + 0.20m + (eveningShowtime.Day is "SAT" or "SUN" ? 0.10m : 0m);
            decimal expectedEveningPrice = RoundToThousand(eveningShowtime.BasePrice * eveningMult);
            Assert.Equal(expectedEveningPrice, eveningPrice);
        }
    }

    /// <summary>
    /// TC02: Nhập mã khuyến mãi sai -> hệ thống bắt lỗi ORA-20002 và ném CineSeatException thân thiện
    /// </summary>
    [Fact]
    public async Task TC02_InvalidPromoCode_CatchesORA20002_ReturnsFriendlyError()
    {
        var service = CreateService();
        int showtimeId = await GetUpcomingShowtimeAsync();

        // 1. Kiểm tra khi tính giá với mã khuyến mãi không tồn tại
        var ex = await Assert.ThrowsAsync<CineSeatException>(async () =>
        {
            await service.CalculateTotalPriceAsync(showtimeId, 1, "MA_KHONG_TON_TAI_9999");
        });

        Assert.Equal(20002, ex.ErrorCode);
        Assert.Contains("khuyến mãi không hợp lệ", ex.FriendlyMessage, StringComparison.OrdinalIgnoreCase);

        // 2. Kiểm tra khi cố gắng giữ ghế với mã không hợp lệ
        int seatId = await GetAvailableSeatAsync(showtimeId);
        string seatJson = JsonSerializer.Serialize(new[] { seatId });
        var exHold = await Assert.ThrowsAsync<CineSeatException>(async () =>
        {
            await service.HoldSeatsAsync(1, showtimeId, seatJson, "MA_KHONG_TON_TAI_9999");
        });

        Assert.Equal(20002, exHold.ErrorCode);
    }

    /// <summary>
    /// TC03: Giữ ghế (HELD) -> Hủy đơn (CANCELLED) -> ghế quay về AVAILABLE
    /// </summary>
    [Fact]
    public async Task TC03_HoldSeats_ThenCancel_RevertsSeatToAvailable()
    {
        var service = CreateService();
        int showtimeId = await GetUpcomingShowtimeAsync();
        int seatId = await GetAvailableSeatAsync(showtimeId);
        string seatJson = JsonSerializer.Serialize(new[] { seatId });

        // 1. Giữ ghế
        var (bookingId, totalAmount, expiresAt) = await service.HoldSeatsAsync(1, showtimeId, seatJson, null);
        Assert.True(bookingId > 0);
        Assert.True(totalAmount > 0);

        // Kiểm tra ghế đã sang HELD
        var seatsAfterHold = await service.GetShowtimeSeatsAsync(showtimeId);
        var heldSeat = seatsAfterHold.FirstOrDefault(s => s.SeatId == seatId);
        Assert.NotNull(heldSeat);
        Assert.Equal("HELD", heldSeat.Status);
        Assert.Equal(bookingId, heldSeat.BookingId);

        // 2. Hủy đơn
        await service.CancelBookingAsync(bookingId);

        // 3. Kiểm tra trạng thái đơn là CANCELLED
        var booking = await service.GetBookingByIdAsync(bookingId);
        Assert.NotNull(booking);
        Assert.Equal("CANCELLED", booking.Status);

        // 4. Kiểm tra ghế đã quay về AVAILABLE
        var seatsAfterCancel = await service.GetShowtimeSeatsAsync(showtimeId);
        var releasedSeat = seatsAfterCancel.FirstOrDefault(s => s.SeatId == seatId);
        Assert.NotNull(releasedSeat);
        Assert.Equal("AVAILABLE", releasedSeat.Status);
        Assert.Null(releasedSeat.BookingId);
    }

    /// <summary>
    /// TC04 (Concurrency - Chống bán trùng ghế):
    /// Mô phỏng 2 request đồng thời cùng gọi giữ 1 ghế duy nhất ->
    /// 1 request thành công (HELD), request còn lại bị chặn bởi SELECT ... FOR UPDATE / ORA-20005
    /// </summary>
    [Fact]
    public async Task TC04_ConcurrentHoldSameSeat_OneSucceeds_OneFailsWithORA20005()
    {
        int showtimeId = await GetUpcomingShowtimeAsync();
        int seatId = await GetAvailableSeatAsync(showtimeId);
        string seatJson = JsonSerializer.Serialize(new[] { seatId });

        var service1 = CreateService();
        var service2 = CreateService();

        var barrier = new SemaphoreSlim(0, 2);

        // Tạo 2 task chạy đồng thời
        var task1 = Task.Run(async () =>
        {
            await barrier.WaitAsync();
            return await service1.HoldSeatsAsync(1, showtimeId, seatJson, null);
        });

        var task2 = Task.Run(async () =>
        {
            await barrier.WaitAsync();
            return await service2.HoldSeatsAsync(2, showtimeId, seatJson, null);
        });

        // Bắn tín hiệu để cả 2 task cùng kích hoạt đồng thời
        barrier.Release(2);

        (int bookingId, decimal totalAmount, DateTime expiresAt)? successResult = null;
        Exception? failedException = null;

        try
        {
            successResult = await task1;
        }
        catch (Exception ex)
        {
            failedException = ex;
        }

        try
        {
            var res2 = await task2;
            if (successResult == null)
            {
                successResult = res2;
            }
            else
            {
                Assert.Fail("Lỗi Concurrency: Cả 2 request đồng thời đều giữ được cùng 1 ghế!");
            }
        }
        catch (Exception ex)
        {
            failedException ??= ex;
        }

        // Kiểm tra kết quả: chính xác 1 request thành công và 1 request thất bại
        Assert.NotNull(successResult);
        Assert.True(successResult.Value.bookingId > 0);

        Assert.NotNull(failedException);
        var cineEx = failedException as CineSeatException;
        Assert.NotNull(cineEx);
        Assert.Equal(20005, cineEx.ErrorCode);
        Assert.Contains("người khác", cineEx.FriendlyMessage, StringComparison.OrdinalIgnoreCase);

        // Dọn dẹp đơn thành công để giải phóng ghế
        await service1.CancelBookingAsync(successResult.Value.bookingId);
    }

    /// <summary>
    /// TC05: Giữ ghế -> Thanh toán mô phỏng (DEMO_QR) ->
    /// Đơn chuyển PAID, ghế chuyển SOLD, bảng PAYMENTS và BOOKING_AUDIT ghi nhận đầy đủ
    /// </summary>
    [Fact]
    public async Task TC05_HoldSeats_ThenConfirmPayment_PaidSoldAndAuditLogged()
    {
        var service = CreateService();
        int showtimeId = await GetUpcomingShowtimeAsync();
        int seatId = await GetAvailableSeatAsync(showtimeId);
        string seatJson = JsonSerializer.Serialize(new[] { seatId });

        // 1. Giữ ghế
        var (bookingId, totalAmount, expiresAt) = await service.HoldSeatsAsync(1, showtimeId, seatJson, null);

        // 2. Thanh toán mô phỏng DEMO_QR
        await service.ConfirmBookingAsync(bookingId, "DEMO_QR");

        // 3. Kiểm tra trạng thái đơn hàng là PAID
        var booking = await service.GetBookingByIdAsync(bookingId);
        Assert.NotNull(booking);
        Assert.Equal("PAID", booking.Status);
        Assert.NotNull(booking.PaidAt);

        // 4. Kiểm tra trạng thái ghế là SOLD
        var seats = await service.GetShowtimeSeatsAsync(showtimeId);
        var seat = seats.FirstOrDefault(s => s.SeatId == seatId);
        Assert.NotNull(seat);
        Assert.Equal("SOLD", seat.Status);
        Assert.Equal(bookingId, seat.BookingId);

        // 5. Kiểm tra bảng PAYMENTS có bản ghi
        using var conn = await _factory.CreateOpenConnectionAsync();
        using var cmdPay = conn.CreateCommand();
        cmdPay.CommandText = @"
            SELECT payment_id, amount, method, payment_status 
              FROM payments 
             WHERE booking_id = :booking_id";
        var pPay = cmdPay.CreateParameter();
        pPay.ParameterName = "booking_id";
        pPay.Value = bookingId;
        cmdPay.Parameters.Add(pPay);

        using var payReader = await cmdPay.ExecuteReaderAsync();
        Assert.True(await payReader.ReadAsync(), "Bảng PAYMENTS không có bản ghi cho booking này");
        Assert.Equal(booking.TotalAmount, payReader.GetDecimal(1));
        Assert.Equal("DEMO_QR", payReader.GetString(2));
        Assert.Equal("PAID", payReader.GetString(3));

        // 6. Kiểm tra bảng BOOKING_AUDIT do trigger TRG_BOOKING_AUDIT tự động ghi
        using var cmdAudit = conn.CreateCommand();
        cmdAudit.CommandText = @"
            SELECT audit_id, old_status, new_status, description 
              FROM booking_audit 
             WHERE booking_id = :booking_id 
             ORDER BY audit_id";
        var pAudit = cmdAudit.CreateParameter();
        pAudit.ParameterName = "booking_id";
        pAudit.Value = bookingId;
        cmdAudit.Parameters.Add(pAudit);

        using var auditReader = await cmdAudit.ExecuteReaderAsync();
        var auditRecords = new List<(string? OldStatus, string NewStatus)>();
        while (await auditReader.ReadAsync())
        {
            auditRecords.Add((
                auditReader.IsDBNull(1) ? null : auditReader.GetString(1),
                auditReader.GetString(2)
            ));
        }

        // Ít nhất 2 bước audit: (null -> HELD) và (HELD -> PAID)
        Assert.True(auditRecords.Count >= 2, "BOOKING_AUDIT phải ghi nhận ít nhất 2 lần thay đổi trạng thái");
        Assert.Equal("HELD", auditRecords[0].NewStatus);
        Assert.Equal("HELD", auditRecords[1].OldStatus);
        Assert.Equal("PAID", auditRecords[1].NewStatus);
    }

    /// <summary>
    /// TC06: Kiểm tra tự động thu hồi ghế hết hạn 5 phút thông qua PR_EXPIRE_HOLDS
    /// </summary>
    [Fact]
    public async Task TC06_ExpiredHolds_AutoReleasedBy_PRExpireHolds()
    {
        var service = CreateService();
        int showtimeId = await GetUpcomingShowtimeAsync();
        int seatId = await GetAvailableSeatAsync(showtimeId);
        string seatJson = JsonSerializer.Serialize(new[] { seatId });

        // 1. Giữ ghế
        var (bookingId, totalAmount, expiresAt) = await service.HoldSeatsAsync(1, showtimeId, seatJson, null);

        // 2. Giả lập thời gian đã quá 5 phút bằng cách cập nhật expires_at và held_until về quá khứ
        using (var conn = await _factory.CreateOpenConnectionAsync())
        {
            using var cmdUpdate = conn.CreateCommand();
            cmdUpdate.CommandText = @"
                BEGIN
                    UPDATE bookings 
                       SET expires_at = LOCALTIMESTAMP - INTERVAL '1' MINUTE 
                     WHERE booking_id = :b_id;

                    UPDATE showtime_seats 
                       SET held_until = LOCALTIMESTAMP - INTERVAL '1' MINUTE 
                     WHERE booking_id = :b_id;
                    COMMIT;
                END;";
            var p = cmdUpdate.CreateParameter();
            p.ParameterName = "b_id";
            p.Value = bookingId;
            cmdUpdate.Parameters.Add(p);
            await cmdUpdate.ExecuteNonQueryAsync();
        }

        // 3. Gọi thủ tục PR_EXPIRE_HOLDS
        await service.ExpireHoldsAsync();

        // 4. Kiểm tra đơn đã chuyển sang EXPIRED
        var booking = await service.GetBookingByIdAsync(bookingId);
        Assert.NotNull(booking);
        Assert.Equal("EXPIRED", booking.Status);

        // 5. Kiểm tra ghế đã quay về AVAILABLE và booking_id được xóa (NULL)
        var seats = await service.GetShowtimeSeatsAsync(showtimeId);
        var seat = seats.FirstOrDefault(s => s.SeatId == seatId);
        Assert.NotNull(seat);
        Assert.Equal("AVAILABLE", seat.Status);
        Assert.Null(seat.BookingId);
    }

    /// <summary>
    /// TC07: Kiểm tra tính toàn vẹn ký tự tiếng Việt (UTF-8 / AL32UTF8) từ Oracle Database
    /// </summary>
    [Fact]
    public async Task TC07_VietnameseCharacterEncoding_PreservedCorrectly()
    {
        var service = CreateService();

        // 1. Kiểm tra 13 phim
        var movies = await service.GetMoviesAsync();
        Assert.NotEmpty(movies);

        var movie1 = movies.FirstOrDefault(m => m.MovieId == 1);
        Assert.NotNull(movie1);
        Assert.Equal("Khoa học viễn tưởng", movie1.Genre);
        Assert.DoesNotContain("?", movie1.Genre);

        var movie8 = movies.FirstOrDefault(m => m.MovieId == 8);
        Assert.NotNull(movie8);
        Assert.Equal("Lật Mặt 7: Một Điều Ước", movie8.Title);
        Assert.Equal("Gia đình / Tâm lý", movie8.Genre);
        Assert.DoesNotContain("?", movie8.Title);

        // Không phim nào chứa dấu ? đen hoặc lỗi mã hóa
        foreach (var m in movies)
        {
            Assert.DoesNotContain("?", m.Title);
            Assert.DoesNotContain("?", m.Genre);
        }

        // 2. Kiểm tra danh sách người dùng tiếng Việt
        var users = await service.GetUsersAsync();
        Assert.NotEmpty(users);

        var user1 = users.FirstOrDefault(u => u.UserId == 1);
        Assert.NotNull(user1);
        Assert.Equal("Nguyễn Minh Anh", user1.FullName);

        var user5 = users.FirstOrDefault(u => u.UserId == 5);
        Assert.NotNull(user5);
        Assert.Equal("Đỗ Thùy Linh", user5.FullName);

        foreach (var u in users)
        {
            Assert.DoesNotContain("?", u.FullName);
        }

        // 3. Kiểm tra cụm rạp & phòng chiếu
        var cinemas = await service.GetCinemasAsync();
        var cinema1 = cinemas.FirstOrDefault(c => c.CinemaId == 1);
        Assert.NotNull(cinema1);
        Assert.Equal("CineSeat Nguyễn Huệ", cinema1.CinemaName);
        Assert.Equal("Quận 1, TP. Hồ Chí Minh", cinema1.Address);
    }

    [Fact]
    public async Task Test8_UserAuthenticationAndRegistration_Success()
    {
        var service = CreateService();

        // 1. Kiểm tra tìm user đã có theo email
        var existingUser = await service.GetUserByEmailAsync("minhanh@demo.local");
        Assert.NotNull(existingUser);
        Assert.Equal("Nguyễn Minh Anh", existingUser.FullName);
        Assert.Equal("0900000001", existingUser.Phone);

        // 2. Đăng ký user mới và kiểm tra tìm lại
        string testEmail = $"test_{Guid.NewGuid():N}@cineseat.local";
        var newUser = await service.RegisterUserAsync("Người Dùng Thử Nghiệm", testEmail, "0987654321");
        Assert.NotNull(newUser);
        Assert.True(newUser.UserId > 0);
        Assert.Equal("Người Dùng Thử Nghiệm", newUser.FullName);

        var retrievedUser = await service.GetUserByEmailAsync(testEmail);
        Assert.NotNull(retrievedUser);
        Assert.Equal(newUser.UserId, retrievedUser.UserId);
        Assert.Equal("0987654321", retrievedUser.Phone);
    }

    [Fact]
    public void Test9_PosterHelper_ResolvesRealImages_Successfully()
    {
        string wwwroot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "CineSeat.Web", "wwwroot"));
        CineSeat.Web.Helpers.PosterHelper.Initialize(wwwroot);

        // 1. Phim có dấu hai chấm: "Dune: Part Two" -> "Dune Part Two.jpg"
        string duneUrl = CineSeat.Web.Helpers.PosterHelper.GetPosterUrl("poster-1.jpg", "Dune: Part Two", 1);
        Assert.Equal("/images/Dune Part Two.jpg", duneUrl);

        // 2. Phim "Oppenheimer" -> "Oppenheimer.jpg"
        string oppUrl = CineSeat.Web.Helpers.PosterHelper.GetPosterUrl("poster-2.jpg", "Oppenheimer", 2);
        Assert.Equal("/images/Oppenheimer.jpg", oppUrl);

        // 3. Phim "Mai" -> "Mai.jpg"
        string maiUrl = CineSeat.Web.Helpers.PosterHelper.GetPosterUrl("poster-9.jpg", "Mai", 9);
        Assert.Equal("/images/Mai.jpg", maiUrl);

        // 4. Phim tiếng Việt "Lật Mặt 7: Một Điều Ước" -> "Lật Mặt 7 Một Điều Ước.jpg"
        string latMatUrl = CineSeat.Web.Helpers.PosterHelper.GetPosterUrl("poster-8.jpg", "Lật Mặt 7: Một Điều Ước", 8);
        Assert.Equal("/images/Lật Mặt 7 Một Điều Ước.jpg", latMatUrl);
    }

    [Fact]
    public async Task Test10_AdminCRUD_CinemasAndPromotions_Success()
    {
        var service = CreateService();

        // 1. Kiểm tra thêm & quản lý mã khuyến mãi mới
        string promoCode = $"TEST{Random.Shared.Next(1000, 9999)}";
        var promoInput = new CreatePromotionInputModel
        {
            PromoCode = promoCode,
            DiscountPct = 25,
            ValidUntil = DateTime.Today.AddDays(30)
        };
        int newPromoId = await service.AddPromotionAsync(promoInput);
        Assert.True(newPromoId > 0);

        var allPromos = await service.GetAllPromotionsAdminAsync();
        var foundPromo = allPromos.FirstOrDefault(p => p.PromotionId == newPromoId);
        Assert.NotNull(foundPromo);
        Assert.Equal(promoCode, foundPromo.PromoCode);
        Assert.Equal(25, foundPromo.DiscountPct);

        // Dọn dẹp mã khuyến mãi thử nghiệm
        await service.DeletePromotionAsync(newPromoId);
    }

    [Fact]
    public async Task Test11_MoviePremiered_CannotBeEdited_AndUnpremieredCanBeEdited()
    {
        var service = CreateService();

        // 1. Phim #1 (Dune: Part Two) đã có suất chiếu / đã công chiếu -> HasMoviePremieredAsync phải trả về true
        bool isPremiered1 = await service.HasMoviePremieredAsync(1);
        Assert.True(isPremiered1);

        // Kiểm tra danh sách phim quản trị: các phim có suất chiếu phải có HasPremiered = true
        var allMovies = await service.GetAllMoviesAdminAsync();
        var movie1 = allMovies.FirstOrDefault(m => m.MovieId == 1);
        Assert.NotNull(movie1);
        Assert.True(movie1.HasPremiered);
        Assert.False(movie1.CanEdit); // Không được phép sửa để tránh lỗi dữ liệu

        // 2. Thêm một phim mới chưa có suất chiếu (chưa công chiếu)
        string testMovieTitle = $"Phim Thử Nghiệm {Random.Shared.Next(1000, 9999)}";
        var createInput = new CreateMovieInputModel
        {
            Title = testMovieTitle,
            Genre = "Khoa học viễn tưởng",
            DurationMin = 110,
            AgeRating = "T13",
            Synopsis = "Mô tả phim thử nghiệm chưa công chiếu",
            PosterFile = "default-poster.jpg"
        };

        int newMovieId = await service.AddMovieAsync(createInput);
        Assert.True(newMovieId > 0);

        try
        {
            // Phim mới chưa có suất chiếu -> Chưa công chiếu -> HasMoviePremieredAsync phải trả về false
            bool isNewMoviePremiered = await service.HasMoviePremieredAsync(newMovieId);
            Assert.False(isNewMoviePremiered);

            var updatedList = await service.GetAllMoviesAdminAsync();
            var newMovie = updatedList.FirstOrDefault(m => m.MovieId == newMovieId);
            Assert.NotNull(newMovie);
            Assert.False(newMovie.HasPremiered);
            Assert.Equal(0, newMovie.ShowtimeCount);
            Assert.True(newMovie.CanEdit); // Phim chưa công chiếu ĐƯỢC PHÉP SỬA

            // Thực hiện chỉnh sửa thành công
            var updateInput = new UpdateMovieInputModel
            {
                MovieId = newMovieId,
                Title = testMovieTitle + " (Đã Chỉnh Sửa)",
                Genre = "Hành động / Viễn tưởng",
                DurationMin = 125,
                AgeRating = "T16",
                Synopsis = "Mô tả đã được cập nhật trước khi công chiếu",
                PosterFile = "default-poster.jpg",
                IsActive = true
            };

            await service.UpdateMovieAsync(updateInput);

            // Kiểm tra thông tin đã được cập nhật
            var reloadedList = await service.GetAllMoviesAdminAsync();
            var editedMovie = reloadedList.FirstOrDefault(m => m.MovieId == newMovieId);
            Assert.NotNull(editedMovie);
            Assert.Equal(testMovieTitle + " (Đã Chỉnh Sửa)", editedMovie.Title);
            Assert.Equal(125, editedMovie.DurationMin);
            Assert.Equal("T16", editedMovie.AgeRating);
        }
        finally
        {
            // Dọn dẹp phim thử nghiệm
            await service.DeleteMovieAsync(newMovieId);
        }
    }

    [Fact]
    public async Task Test12_AdminUserContext_ReturnsZeroUserId_AndCannotBookTicket()
    {
        // Kiểm tra phân quyền: Tài khoản Admin (Quản trị viên) KHÔNG ĐƯỢC PHÉP ĐẶT VÉ
        // và KHÔNG ĐƯỢC gán nhầm userId sang tài khoản khách hàng ID=1 (Nguyễn Minh Anh)
        var service = CreateService();

        // 1. Giả lập phiên đăng nhập của Admin
        var adminClaims = new List<System.Security.Claims.Claim>
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "0"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "Quản Trị Viên CineSeat"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, "admin@cineseat.vn"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "ADMIN")
        };
        var identity = new System.Security.Claims.ClaimsIdentity(adminClaims, "Cookie");
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = principal };
        var httpContextAccessor = new Microsoft.AspNetCore.Http.HttpContextAccessor { HttpContext = httpContext };

        var adminUserContext = new UserContext(httpContextAccessor, service);

        // Khẳng định vai trò Admin
        Assert.True(adminUserContext.IsAdmin);
        Assert.False(adminUserContext.IsCustomer);
        Assert.Equal("ADMIN", adminUserContext.CurrentUserRole);

        // ĐẶC TẢ QUAN TRỌNG: CurrentUserId của Admin PHẢI BẰNG 0, KHÔNG ĐƯỢC fallback về 1
        Assert.Equal(0, adminUserContext.CurrentUserId);

        var adminUser = await adminUserContext.GetCurrentUserAsync();
        Assert.NotNull(adminUser);
        Assert.Equal(0, adminUser.UserId);

        // 2. Nếu Admin cố tình gọi giữ ghế với userId = 0 -> Oracle PL/SQL PKG_BOOKING.PR_HOLD
        // sẽ từ chối ngay lập tức do user_id=0 không tồn tại trong bảng APP_USERS (Bắt lỗi -20006)
        int showtimeId = await GetUpcomingShowtimeAsync();
        int seatId = await GetAvailableSeatAsync(showtimeId);
        string seatJson = JsonSerializer.Serialize(new[] { seatId });

        var ex = await Assert.ThrowsAsync<CineSeatException>(() =>
            service.HoldSeatsAsync(adminUserContext.CurrentUserId, showtimeId, seatJson, null)
        );

        Assert.Equal(20006, ex.ErrorCode);
    }
}
