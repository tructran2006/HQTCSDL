using Oracle.ManagedDataAccess.Client;

namespace CineSeat.Web.Exceptions
{
    public class CineSeatException : Exception
    {
        public int ErrorCode { get; }
        public string FriendlyMessage { get; }

        public CineSeatException(int errorCode, string friendlyMessage, Exception? innerException = null)
            : base(friendlyMessage, innerException)
        {
            ErrorCode = errorCode;
            FriendlyMessage = friendlyMessage;
        }

        public static CineSeatException FromOracleException(OracleException ex)
        {
            int code = ex.Number;
            string message = code switch
            {
                20001 => "Mỗi đơn hàng chỉ được phép đặt từ 1 đến 8 ghế (ORA-20001).",
                20002 => "Mã khuyến mãi không hợp lệ hoặc đã hết hạn sử dụng (ORA-20002).",
                20003 => "Không tìm thấy thông tin suất chiếu hoặc suất chiếu đã ngưng hoạt động (ORA-20003).",
                20004 => "Danh sách ghế chọn không hợp lệ hoặc có ghế bị trùng lặp (ORA-20004).",
                20005 => "Ghế đã có người khác nhanh tay giữ hoặc mua trước đó! Vui lòng chọn ghế khác (ORA-20005).",
                20006 => "Không tìm thấy tài khoản người dùng hoặc suất chiếu đã quá giờ (ORA-20006).",
                20007 => "Đơn hàng không ở trạng thái đang giữ chỗ (HELD) để thanh toán (ORA-20007).",
                20008 => "Đã hết thời gian 5 phút giữ chỗ! Đơn hàng đã hết hạn, vui lòng đặt lại (ORA-20008).",
                20009 => "Phương thức thanh toán thử nghiệm không hợp lệ (chỉ chấp nhận DEMO_QR, DEMO_CARD, DEMO_CASH) (ORA-20009).",
                20010 => "Số lượng ghế của suất không còn đủ để hoàn tất đơn hàng (ORA-20010).",
                20011 => "Không thể hủy đơn đặt vé! Chỉ có thể hủy đơn đang trong trạng thái giữ chỗ (HELD) (ORA-20011).",
                20012 => "Vi phạm quy tắc bảo vệ: Ghế đã bán (SOLD) không thể trả lại khi chưa hoàn tiền (ORA-20012).",
                20013 => "Vi phạm quy tắc bảo vệ: Phải giữ ghế (HELD) trước khi chuyển sang trạng thái đã bán (SOLD) (ORA-20013).",
                20014 => "Vi phạm quy tắc bảo vệ: Không được phép đổi chủ sở hữu ghế khi thanh toán (ORA-20014).",
                _ => $"Lỗi cơ sở dữ liệu Oracle [{code}]: {ex.Message}"
            };

            return new CineSeatException(code, message, ex);
        }
    }
}
