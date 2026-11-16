using System.Security.Claims;
using CineSeat.Web.Models;
using CineSeat.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineSeat.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly ICineSeatService _cineSeatService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            ICineSeatService cineSeatService, 
            IConfiguration configuration, 
            ILogger<AccountController> logger)
        {
            _cineSeatService = cineSeatService;
            _configuration = configuration;
            _logger = logger;
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("ADMIN"))
                {
                    return RedirectToAction("Dashboard", "Admin");
                }
                return RedirectToAction("Index", "Movies");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string inputEmail = model.Email.Trim();
            string inputPassword = model.Password.Trim();

            // 1. Kiểm tra tài khoản Quản Trị Viên (Admin) cấu hình trong appsettings.json
            var adminEmail = _configuration["AdminAccount:Email"] ?? "admin@cineseat.vn";
            var adminPassword = _configuration["AdminAccount:Password"] ?? "Admin@123";
            var adminFullName = _configuration["AdminAccount:FullName"] ?? "Quản Trị Viên CineSeat";

            if (string.Equals(inputEmail, adminEmail, StringComparison.OrdinalIgnoreCase) && inputPassword == adminPassword)
            {
                var adminClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, "0"),
                    new Claim(ClaimTypes.Name, adminFullName),
                    new Claim(ClaimTypes.Email, adminEmail),
                    new Claim(ClaimTypes.Role, "ADMIN")
                };

                var claimsIdentity = new ClaimsIdentity(adminClaims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                _logger.LogInformation("Admin đăng nhập thành công: {Email}", adminEmail);
                TempData["SuccessMessage"] = $"Chào mừng {adminFullName} đăng nhập hệ thống quản trị!";

                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }
                return RedirectToAction("Dashboard", "Admin");
            }

            // 2. Kiểm tra tài khoản Khách hàng (Customer) trong bảng APP_USERS của Oracle 23ai
            try
            {
                var user = await _cineSeatService.GetUserByEmailAsync(inputEmail);
                if (user != null)
                {
                    // Chấp nhận mật khẩu là số điện thoại hoặc mật khẩu demo mặc định "123456"
                    bool isPasswordValid = (!string.IsNullOrEmpty(user.Phone) && inputPassword == user.Phone.Trim())
                                        || inputPassword == "123456";

                    if (isPasswordValid)
                    {
                        var customerClaims = new List<Claim>
                        {
                            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                            new Claim(ClaimTypes.Name, user.FullName),
                            new Claim(ClaimTypes.Email, user.Email),
                            new Claim(ClaimTypes.MobilePhone, user.Phone ?? string.Empty),
                            new Claim(ClaimTypes.Role, "CUSTOMER")
                        };

                        var claimsIdentity = new ClaimsIdentity(customerClaims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var authProperties = new AuthenticationProperties
                        {
                            IsPersistent = true,
                            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                        };

                        await HttpContext.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            new ClaimsPrincipal(claimsIdentity),
                            authProperties);

                        _logger.LogInformation("Khách hàng đăng nhập thành công: {Email} (ID: {UserId})", user.Email, user.UserId);
                        TempData["SuccessMessage"] = $"Đăng nhập thành công! Chào mừng {user.FullName}.";

                        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                        {
                            return Redirect(model.ReturnUrl);
                        }
                        return RedirectToAction("Index", "Movies");
                    }
                }

                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không chính xác. Vui lòng kiểm tra lại thông tin đăng nhập.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xác thực đăng nhập người dùng: {Email}", inputEmail);
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi kết nối cơ sở dữ liệu. Vui lòng thử lại sau.");
            }

            return View(model);
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Movies");
            }
            return View(new RegisterViewModel());
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var newUser = await _cineSeatService.RegisterUserAsync(model.FullName, model.Email, model.Phone);

                // Tự động đăng nhập với role CUSTOMER ngay sau khi đăng ký thành công
                var customerClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, newUser.UserId.ToString()),
                    new Claim(ClaimTypes.Name, newUser.FullName),
                    new Claim(ClaimTypes.Email, newUser.Email),
                    new Claim(ClaimTypes.MobilePhone, newUser.Phone ?? string.Empty),
                    new Claim(ClaimTypes.Role, "CUSTOMER")
                };

                var claimsIdentity = new ClaimsIdentity(customerClaims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity));

                _logger.LogInformation("Đăng ký thành công người dùng mới: {Email} (ID: {UserId})", newUser.Email, newUser.UserId);
                TempData["SuccessMessage"] = $"Đăng ký tài khoản thành công! Chào mừng {newUser.FullName} đến với CineSeat.";

                return RedirectToAction("Index", "Movies");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo người dùng mới: {Email}", model.Email);
                ModelState.AddModelError(string.Empty, "Không thể hoàn tất đăng ký do lỗi cơ sở dữ liệu. Vui lòng thử lại.");
            }

            return View(model);
        }

        // POST / GET: /Account/Logout
        [HttpPost]
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["SuccessMessage"] = "Bạn đã đăng xuất an toàn khỏi hệ thống CineSeat.";
            return RedirectToAction("Index", "Movies");
        }

        // GET: /Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
