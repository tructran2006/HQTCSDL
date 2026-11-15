using CineSeat.Web.Models;

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
                if (context != null && context.Request.Cookies.TryGetValue(UserCookieKey, out var cookieVal))
                {
                    if (int.TryParse(cookieVal, out int uid) && uid > 0)
                    {
                        return uid;
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
