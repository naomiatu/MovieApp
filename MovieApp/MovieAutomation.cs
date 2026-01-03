using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace MovieApp
{
    public static class MovieAutomation
    {
        private static List<Movie> _movies;

        public static async Task<List<Movie>> GetAllMoviesAsync()
        {
            if (_movies != null)
                return _movies;

            try
            {
                // Load from Resources/Raw/movies.json
                using var stream = await FileSystem.OpenAppPackageFileAsync("movies.json");
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                _movies = JsonSerializer.Deserialize<List<Movie>>(json, options);

                // Add debug output
                System.Diagnostics.Debug.WriteLine($"✅ Loaded {_movies?.Count ?? 0} movies from JSON");

                if (_movies != null && _movies.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"First movie: {_movies[0].title}");
                    System.Diagnostics.Debug.WriteLine($"Poster URL: {_movies[0].poster}");
                }

                return _movies ?? new List<Movie>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error loading movies: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return new List<Movie>();
            }
        }

        public static async Task<Movie> GetMovieByNameAsync(string movieName)
        {
            var movies = await GetAllMoviesAsync();
            return movies.FirstOrDefault(m =>
                m.title.Equals(movieName, StringComparison.OrdinalIgnoreCase));
        }

        public static async Task<List<Movie>> SearchMoviesAsync(string searchTerm)
        {
            var movies = await GetAllMoviesAsync();
            if (string.IsNullOrWhiteSpace(searchTerm))
                return movies;

            searchTerm = searchTerm.ToLower();
            return movies.Where(m =>
                m.title.ToLower().Contains(searchTerm) ||
                (m.director?.ToLower().Contains(searchTerm) ?? false) ||
                (m.genre?.Any(g => g.ToLower().Contains(searchTerm)) ?? false)
            ).ToList();
        }

        public static async Task<List<Movie>> GetMoviesByGenreAsync(string genre)
        {
            var movies = await GetAllMoviesAsync();
            if (string.IsNullOrWhiteSpace(genre))
                return movies;

            return movies.Where(m => m.genre?.Contains(genre) ?? false).ToList();
        }

        public static async Task<List<Movie>> GetTopRatedMoviesAsync(int count = 10)
        {
            var movies = await GetAllMoviesAsync();
            return movies.OrderByDescending(m => m.rating).Take(count).ToList();
        }

        public static async Task<List<string>> GetAllGenresAsync()
        {
            var movies = await GetAllMoviesAsync();
            return movies
                .Where(m => m.genre != null)
                .SelectMany(m => m.genre)
                .Distinct()
                .OrderBy(g => g)
                .ToList();
        }

        public static async Task<List<Movie>> GetMoviesByYearAsync(int year)
        {
            var movies = await GetAllMoviesAsync();
            return movies.Where(m => m.year == year).ToList();
        }

        public static async Task<List<Movie>> GetMoviesByDirectorAsync(string director)
        {
            var movies = await GetAllMoviesAsync();
            return movies.Where(m =>
                m.director?.Equals(director, StringComparison.OrdinalIgnoreCase) ?? false
            ).ToList();
        }

        public static void ClearCache()
        {
            _movies = null;
        }
    }
}