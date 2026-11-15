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
}
