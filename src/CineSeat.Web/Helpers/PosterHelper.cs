using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace CineSeat.Web.Helpers
{
    public static class PosterHelper
    {
        private static string? _webRootPath;
        private static readonly ConcurrentDictionary<string, string> _cache = new(StringComparer.OrdinalIgnoreCase);

        public static void Initialize(string webRootPath)
        {
            _webRootPath = webRootPath;
            _cache.Clear();
        }

        /// <summary>
        /// Lấy URL của ảnh poster phim (tự động khớp ảnh thật trong wwwroot/images)
        /// </summary>
        public static string GetPosterUrl(string? posterFile, string? movieTitle, int movieId = 0)
        {
            string cacheKey = $"{movieId}_{posterFile}_{movieTitle}";
            if (_cache.TryGetValue(cacheKey, out var cachedUrl))
            {
                return cachedUrl;
            }

            string resolvedUrl = ResolvePosterUrlInternal(posterFile, movieTitle, movieId);
            _cache[cacheKey] = resolvedUrl;
            return resolvedUrl;
        }

        private static string ResolvePosterUrlInternal(string? posterFile, string? movieTitle, int movieId)
        {
            string imagesDir = _webRootPath != null 
                ? Path.Combine(_webRootPath, "images")
                : Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");

            if (!Directory.Exists(imagesDir))
            {
                return !string.IsNullOrWhiteSpace(posterFile) ? $"/images/{posterFile}" : "/images/default-poster.jpg";
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };

            // 1. Nếu có movieTitle, ưu tiên tìm ảnh thật khớp với tên phim (ví dụ: "Dune Part Two.jpg", "Mai.jpg")
            if (!string.IsNullOrWhiteSpace(movieTitle))
            {
                string cleanTitle = movieTitle.Replace(":", "").Replace("/", " ").Replace("\\", " ").Replace("?", "").Replace("*", "").Trim();

                // 1.1 Thử trực tiếp cleanTitle + các extension
                foreach (var ext in allowedExtensions)
                {
                    string candidate = cleanTitle + ext;
                    if (File.Exists(Path.Combine(imagesDir, candidate)))
                    {
                        return $"/images/{candidate}";
                    }
                }

                // 1.2 Thử tìm file theo so khớp tên chuẩn hóa (bỏ dấu câu, so sánh tương đối)
                string normTitle = NormalizeForComparison(cleanTitle);
                try
                {
                    var files = Directory.GetFiles(imagesDir);
                    foreach (var file in files)
                    {
                        string fileName = Path.GetFileName(file);
                        string fileNameWithoutExt = Path.GetFileNameWithoutExtension(file);
                        string normFileName = NormalizeForComparison(fileNameWithoutExt);

                        if (normFileName.Length >= 3 && (normFileName == normTitle || normFileName.Contains(normTitle) || normTitle.Contains(normFileName)))
                        {
                            // Tránh nhầm với poster-1.jpg nếu có ảnh tên phim thật
                            if (!fileName.StartsWith("poster-", StringComparison.OrdinalIgnoreCase) &&
                                !fileName.Equals("default-poster.jpg", StringComparison.OrdinalIgnoreCase))
                            {
                                return $"/images/{fileName}";
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore file access issues and continue fallbacks
                }
            }

            // 2. Thử trực tiếp posterFile nếu được cung cấp và file tồn tại
            if (!string.IsNullOrWhiteSpace(posterFile) && !posterFile.Equals("default-poster.jpg", StringComparison.OrdinalIgnoreCase))
            {
                string directPath = Path.Combine(imagesDir, posterFile);
                if (File.Exists(directPath))
                {
                    return $"/images/{posterFile}";
                }
            }

            // 3. Thử tìm theo poster-{movieId}.*
            if (movieId > 0)
            {
                foreach (var ext in allowedExtensions)
                {
                    string candidate = $"poster-{movieId}{ext}";
                    if (File.Exists(Path.Combine(imagesDir, candidate)))
                    {
                        return $"/images/{candidate}";
                    }
                }
            }

            // 4. Fallback poster-1.jpg ... nếu posterFile là poster-X.jpg
            if (!string.IsNullOrWhiteSpace(posterFile) && File.Exists(Path.Combine(imagesDir, posterFile)))
            {
                return $"/images/{posterFile}";
            }

            return "/images/default-poster.jpg";
        }

        private static string NormalizeForComparison(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            // Xóa ký tự đặc biệt, dấu câu, chỉ giữ chữ và số
            string noSpecial = Regex.Replace(input, @"[^\p{L}\p{Nd}]+", "").ToLowerInvariant();
            return noSpecial;
        }
    }
}
