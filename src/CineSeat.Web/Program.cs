using System.Text.Encodings.Web;
using System.Text.Unicode;
using CineSeat.Web.Data;
using CineSeat.Web.Helpers;
using CineSeat.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình UTF-8 HtmlEncoder & JsonEncoder cho toàn bộ tiếng Việt không bị escape HTML entity
builder.Services.AddSingleton<HtmlEncoder>(HtmlEncoder.Create(UnicodeRanges.All));

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
    });
builder.Services.AddHttpContextAccessor();

// Đăng ký dịch vụ kết nối Oracle và Service nghiệp vụ
builder.Services.AddSingleton<IDbConnectionFactory, OracleConnectionFactory>();
builder.Services.AddScoped<ICineSeatService, CineSeatService>();
builder.Services.AddScoped<IUserContext, UserContext>();

// Cấu hình Cookie Authentication phân quyền ADMIN / CUSTOMER
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.LogoutPath = "/Account/Logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

// Hỗ trợ Session
builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(30);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Khởi tạo PosterHelper với đường dẫn WebRoot
PosterHelper.Initialize(app.Environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"));

// Route mặc định trỏ vào Movies/Index (Trang chủ CineSeat)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Movies}/{action=Index}/{id?}");

app.Run();
