using System.Security.Claims;
using CineSeat.Web.Models;
using Microsoft.AspNetCore.Http;

namespace CineSeat.Web.Services
{
    public interface IUserContext
    {
        int CurrentUserId { get; }
        bool IsAuthenticated { get; }
        bool IsAdmin { get; }
        bool IsCustomer { get; }
        string CurrentUserName { get; }
        string CurrentUserEmail { get; }
        string CurrentUserPhone { get; }
        string CurrentUserRole { get; }
        Task<AppUser?> GetCurrentUserAsync();
    }

    public class UserContext : IUserContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICineSeatService _cineSeatService;

        public UserContext(IHttpContextAccessor httpContextAccessor, ICineSeatService cineSeatService)
        {
            _httpContextAccessor = httpContextAccessor;
            _cineSeatService = cineSeatService;
        }

        private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

        public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

        public bool IsAdmin => IsAuthenticated && User?.IsInRole("ADMIN") == true;

        public bool IsCustomer => IsAuthenticated && User?.IsInRole("CUSTOMER") == true;

        public string CurrentUserRole => IsAdmin ? "ADMIN" : (IsCustomer ? "CUSTOMER" : "GUEST");

        public string CurrentUserName
        {
            get
            {
                if (!IsAuthenticated || User == null)
                {
                    return "Khách";
                }
                return User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Name) ?? "Người dùng";
            }
        }

        public string CurrentUserEmail => User?.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

        public string CurrentUserPhone => User?.FindFirstValue(ClaimTypes.MobilePhone) ?? string.Empty;

        public int CurrentUserId
        {
            get
            {
                if (IsAdmin)
                {
                    // Admin là tài khoản quản trị hệ thống, không có ID khách hàng và không được phép đặt vé
                    return 0;
                }

                if (IsAuthenticated && User != null)
                {
                    var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (int.TryParse(idClaim, out int uid) && uid > 0)
                    {
                        return uid;
                    }
                }
                // Khách chưa đăng nhập trả về 0, tuyệt đối không fallback về 1 để tránh gán nhầm vé cho khách hàng ID=1
                return 0;
            }
        }

        public async Task<AppUser?> GetCurrentUserAsync()
        {
            if (IsAdmin)
            {
                return new AppUser
                {
                    UserId = 0,
                    FullName = CurrentUserName,
                    Email = CurrentUserEmail,
                    Phone = "0999999999"
                };
            }

            if (IsAuthenticated)
            {
                int uid = CurrentUserId;
                if (uid > 0)
                {
                    var user = await _cineSeatService.GetUserByIdAsync(uid);
                    if (user != null)
                    {
                        return user;
                    }
                }
            }

            // Khách chưa đăng nhập trả về null
            return null;
        }
    }
}
