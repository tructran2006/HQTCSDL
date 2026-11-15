using CineSeat.Web.Models;
using Microsoft.AspNetCore.Http;

namespace CineSeat.Web.Services
{
    public interface IUserContext
    {
        int CurrentUserId { get; }
        Task<AppUser> GetCurrentUserAsync();
        void SetCurrentUser(int userId);
    }

    public class UserContext : IUserContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICineSeatService _cineSeatService;
        private const string UserSessionKey = "CurrentUserId";
        private const string UserCookieKey = "CineSeat_UserId";

        public UserContext(IHttpContextAccessor httpContextAccessor, ICineSeatService cineSeatService)
        {
            _httpContextAccessor = httpContextAccessor;
            _cineSeatService = cineSeatService;
        }

        public int CurrentUserId
        {
            get
            {
                var context = _httpContextAccessor.HttpContext;
                if (context != null)
                {
                    // 1. Kiểm tra Session trước
                    try
                    {
                        var sessionUid = context.Session.GetInt32(UserSessionKey);
                        if (sessionUid.HasValue && sessionUid.Value > 0)
                        {
                            return sessionUid.Value;
                        }
                    }
                    catch
                    {
                        // Bỏ qua nếu Session chưa sẵn sàng
                    }

                    // 2. Kiểm tra Cookie
                    if (context.Request.Cookies.TryGetValue(UserCookieKey, out var cookieVal))
                    {
                        if (int.TryParse(cookieVal, out int uid) && uid > 0)
                        {
                            return uid;
                        }
                    }
                }
                return 1; // Mặc định là Nguyễn Minh Anh (user_id = 1)
            }
        }

        public async Task<AppUser> GetCurrentUserAsync()
        {
            int uid = CurrentUserId;
            var user = await _cineSeatService.GetUserByIdAsync(uid);
            return user ?? new AppUser { UserId = 1, FullName = "Nguyễn Minh Anh", Email = "minhanh@demo.local" };
        }

        public void SetCurrentUser(int userId)
        {
            var context = _httpContextAccessor.HttpContext;
            if (context != null)
            {
                // Lưu vào Session
                try
                {
                    context.Session.SetInt32(UserSessionKey, userId);
                }
                catch
                {
                    // Bỏ qua nếu Session lỗi
                }

                // Lưu vào Cookie
                context.Response.Cookies.Append(UserCookieKey, userId.ToString(), new CookieOptions
                {
                    Expires = DateTimeOffset.Now.AddDays(7),
                    HttpOnly = true,
                    IsEssential = true
                });
            }
        }
    }
}
