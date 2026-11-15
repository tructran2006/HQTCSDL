using CineSeat.Web.Models;
using CineSeat.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace CineSeat.Web.Controllers
{
    public class MoviesController : Controller
    {
        private readonly ICineSeatService _cineSeatService;
        private readonly IUserContext _userContext;

        public MoviesController(ICineSeatService cineSeatService, IUserContext userContext)
        {
            _cineSeatService = cineSeatService;
            _userContext = userContext;
        }

        public async Task<IActionResult> Index(string? search, string? genre)
        {
            var movies = await _cineSeatService.GetMoviesAsync(search, genre);
            var users = await _cineSeatService.GetUsersAsync();
            var currentUser = await _userContext.GetCurrentUserAsync();

            ViewBag.Search = search;
            ViewBag.Genre = genre;
            ViewBag.Users = users;
            ViewBag.CurrentUser = currentUser;

            // Lấy danh sách thể loại độc nhất để làm bộ lọc
            var allMovies = await _cineSeatService.GetMoviesAsync();
            ViewBag.Genres = allMovies.Select(m => m.Genre).Distinct().OrderBy(g => g).ToList();

            return View(movies);
        }

        public async Task<IActionResult> Showtimes(int id)
        {
            var movie = await _cineSeatService.GetMovieByIdAsync(id);
            if (movie == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy phim yêu cầu.";
                return RedirectToAction(nameof(Index));
            }

            var showtimes = await _cineSeatService.GetShowtimesByMovieIdAsync(id);
            ViewBag.Movie = movie;
            ViewBag.CurrentUser = await _userContext.GetCurrentUserAsync();

            return View(showtimes);
        }

        [HttpPost]
        public IActionResult SwitchUser(int userId, string? returnUrl)
        {
            _userContext.SetCurrentUser(userId);
            TempData["SuccessMessage"] = $"Đã chuyển tài khoản người dùng demo thành công!";
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
