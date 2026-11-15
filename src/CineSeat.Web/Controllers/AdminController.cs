using CineSeat.Web.Exceptions;
using CineSeat.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace CineSeat.Web.Controllers
{
    public class AdminController : Controller
    {
        private readonly ICineSeatService _cineSeatService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ICineSeatService cineSeatService, ILogger<AdminController> logger)
        {
            _cineSeatService = cineSeatService;
            _logger = logger;
        }

        public async Task<IActionResult> Dashboard()
        {
            var stats = await _cineSeatService.GetAdminDashboardStatsAsync();
            return View(stats);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TriggerExpire()
        {
            try
            {
                await _cineSeatService.ExpireHoldsAsync();
                TempData["SuccessMessage"] = "Đã thực thi thủ tục PR_EXPIRE_HOLDS thành công! Các đơn và ghế giữ quá hạn 5 phút đã được tự động giải phóng.";
            }
            catch (CineSeatException cex)
            {
                TempData["ErrorMessage"] = cex.FriendlyMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi chạy PR_EXPIRE_HOLDS");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi thực thi PR_EXPIRE_HOLDS.";
            }

            return RedirectToAction(nameof(Dashboard));
        }
    }
}
