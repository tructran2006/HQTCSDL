using CineSeat.Web.Models;
using CineSeat.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace CineSeat.Web.Controllers
{
    public class MoviesController : Controller
    {
        private readonly ICineSeatService _cineSeatService;

        public MoviesController(ICineSeatService cineSeatService)
        {
            _cineSeatService = cineSeatService;
        }

        // GET: / (hoặc /Movies/Index)
        public async Task<IActionResult> Index(string? search, string? genre, string? ageRating)
        {
            var movies = await _cineSeatService.GetMoviesAsync(search, genre, ageRating);
            ViewBag.Search = search;
            ViewBag.SelectedGenre = genre;
            ViewBag.SelectedAgeRating = ageRating ?? "ALL";

            // Lấy danh sách thể loại động từ phim
            var allMovies = await _cineSeatService.GetMoviesAsync();
            ViewBag.AllGenres = allMovies
                .SelectMany(m => m.Genre.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                .Distinct()
                .OrderBy(g => g)
                .ToList();

            // Phim nổi bật cho Hero Banner (chọn phim có nhiều suất chiếu nhất hoặc phim đầu tiên)
            ViewBag.HeroMovie = movies.OrderByDescending(m => m.ShowtimeCount).FirstOrDefault() 
                             ?? allMovies.FirstOrDefault();

            return View(movies);
        }

        // GET: /Movies/Details/{id}
        public async Task<IActionResult> Details(int id)
        {
            var movie = await _cineSeatService.GetMovieByIdAsync(id);
            if (movie == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy bộ phim yêu cầu.";
                return RedirectToAction(nameof(Index));
            }

            var showtimes = await _cineSeatService.GetShowtimesByMovieIdAsync(id);
            var vm = new MovieDetailsViewModel
            {
                Movie = movie,
                Showtimes = showtimes
            };

            return View(vm);
        }

        // GET: /Movies/Showtimes/{id} (Alias trỏ về Details để tương thích ngược)
        public IActionResult Showtimes(int id)
        {
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: /Movies/CinemaShowtimes (Lịch chiếu theo từng rạp)
        public async Task<IActionResult> CinemaShowtimes(int? cinemaId)
        {
            var cinemas = await _cineSeatService.GetCinemasAsync();
            if (!cinemas.Any())
            {
                TempData["ErrorMessage"] = "Chưa có thông tin cụm rạp.";
                return RedirectToAction(nameof(Index));
            }

            int selectedId = cinemaId ?? cinemas.First().CinemaId;
            var selectedCinema = cinemas.FirstOrDefault(c => c.CinemaId == selectedId) ?? cinemas.First();
            var showtimes = await _cineSeatService.GetShowtimesByCinemaIdAsync(selectedId);

            var vm = new CinemaShowtimesViewModel
            {
                Cinemas = cinemas,
                SelectedCinemaId = selectedId,
                SelectedCinema = selectedCinema,
                Showtimes = showtimes
            };

            return View(vm);
        }
    }
}
