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
                20001 => "Mỗi đơn hàng chỉ được phép đặt từ 1 đến 8 ghế.",
                20002 => "Mã khuyến mãi không hợp lệ hoặc đã hết hạn sử dụng.",
                20003 => "Không tìm thấy thông tin suất chiếu hoặc suất chiếu đã ngưng hoạt động.",
                20004 => "Danh sách ghế chọn không hợp lệ hoặc có ghế bị trùng lặp.",
                20005 => "Ghế bạn chọn vừa có người khác đặt trước hoặc đang được giữ chỗ. Vui lòng chọn ghế khác.",
                20006 => "Không tìm thấy thông tin tài khoản hoặc suất chiếu đã quá giờ bắt đầu.",
                20007 => "Đơn hàng không ở trạng thái chờ thanh toán hoặc đã bị hủy.",
                20008 => "Đã hết thời gian 5 phút giữ chỗ! Đơn hàng đã hết hạn, các ghế đã được giải phóng.",
                20009 => "Phương thức thanh toán không hợp lệ.",
                20010 => "Số lượng ghế của suất chiếu không còn đủ để hoàn tất đơn hàng.",
                20011 => "Không thể hủy đơn đặt vé đã thanh toán thành công.",
                20012 => "Ghế đã bán không thể hoàn trả khi chưa có xác nhận từ ban quản lý rạp.",
                20013 => "Ghế cần được giữ chỗ trước khi tiến hành thanh toán.",
                20014 => "Thông tin người thanh toán không khớp với người giữ chỗ.",
                _ => $"Hệ thống gặp sự cố trong quá trình xử lý: {ex.Message}"
            };

            return new CineSeatException(code, message, ex);
        }
    }
}
