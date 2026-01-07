using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MovieApp
{
    /// <summary>
    /// MEMORY OPTIMIZED: Centralized service that loads movies ONCE and shares across all pages
    /// This prevents duplicate loading and excessive memory usage
    /// </summary>
    public class MovieDataService
    {
        private static MovieDataService _instance;
        private static readonly object _lock = new object();

        private List<Movie> _allMovies;
        private bool _isLoaded;
        private bool _isLoading;
        private Task _loadingTask;

        public static MovieDataService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new MovieDataService();
                        }
                    }
                }
                return _instance;
            }
        }

        private MovieDataService()
        {
            _allMovies = new List<Movie>();
            _isLoaded = false;
            _isLoading = false;
        }

        /// <summary>
        /// CRITICAL: Load movies only once, all other calls return cached data
        /// Call this from SplashPage to preload everything
        /// </summary>
        public async Task<List<Movie>> GetMoviesAsync()
        {
            // If already loaded, return immediately
            if (_isLoaded && _allMovies != null && _allMovies.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"✅ Returning cached movies ({_allMovies.Count} movies)");
                return _allMovies;
            }

            // If currently loading, wait for that operation to complete
            if (_isLoading && _loadingTask != null)
            {
                System.Diagnostics.Debug.WriteLine("⏳ Waiting for existing load operation...");
                await _loadingTask;
                return _allMovies;
            }

            // Start loading
            _isLoading = true;
            _loadingTask = LoadMoviesInternalAsync();
            await _loadingTask;

            return _allMovies;
        }

        private async Task LoadMoviesInternalAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🎬 Loading movies from API (ONE TIME ONLY)...");
                System.Diagnostics.Debug.WriteLine($"💾 Memory before: {GC.GetTotalMemory(false) / 1024 / 1024} MB");

                // Use MovieAutomation to fetch from API
                _allMovies = await MovieAutomation.GetAllMoviesAsync();

                if (_allMovies == null)
                    _allMovies = new List<Movie>();

                _isLoaded = true;

                System.Diagnostics.Debug.WriteLine($"✅ Movies loaded successfully ({_allMovies.Count} movies)");
                System.Diagnostics.Debug.WriteLine($"💾 Memory after: {GC.GetTotalMemory(false) / 1024 / 1024} MB");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error loading movies: {ex.Message}");
                _allMovies = new List<Movie>();
                _isLoaded = false;
            }
            finally
            {
                _isLoading = false;
            }
        }

        /// <summary>
        /// Get movies synchronously (only if already loaded)
        /// </summary>
        public List<Movie> GetMoviesSync()
        {
            if (!_isLoaded)
            {
                System.Diagnostics.Debug.WriteLine("⚠️ Movies not loaded yet!");
                return new List<Movie>();
            }
            return _allMovies;
        }

        /// <summary>
        /// Check if movies are loaded
        /// </summary>
        public bool IsLoaded => _isLoaded;

        /// <summary>
        /// Get movies count without loading
        /// </summary>
        public int Count => _allMovies?.Count ?? 0;

        /// <summary>
        /// Search movies (from cached data)
        /// </summary>
        public List<Movie> SearchMovies(string searchTerm)
        {
            if (!_isLoaded || _allMovies == null)
                return new List<Movie>();

            if (string.IsNullOrWhiteSpace(searchTerm))
                return _allMovies;

            searchTerm = searchTerm.ToLower();

            return _allMovies.Where(m =>
                m.title.ToLower().Contains(searchTerm) ||
                (m.director?.ToLower().Contains(searchTerm) ?? false) ||
                (m.genre?.Any(g => g.ToLower().Contains(searchTerm)) ?? false)
            ).ToList();
        }

        /// <summary>
        /// Filter by genre (from cached data)
        /// </summary>
        public List<Movie> GetMoviesByGenre(List<string> genres)
        {
            if (!_isLoaded || _allMovies == null || genres == null || genres.Count == 0)
                return _allMovies ?? new List<Movie>();

            return _allMovies.Where(m =>
                m.genre?.Any(g => genres.Contains(g, StringComparer.OrdinalIgnoreCase)) ?? false
            ).ToList();
        }

        /// <summary>
        /// Get top rated movies (from cached data)
        /// </summary>
        public List<Movie> GetTopRated(int count = 10)
        {
            if (!_isLoaded || _allMovies == null)
                return new List<Movie>();

            return _allMovies
                .OrderByDescending(m => m.rating)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Get all unique genres (from cached data)
        /// </summary>
        public List<string> GetAllGenres()
        {
            if (!_isLoaded || _allMovies == null)
                return new List<string>();

            return _allMovies
                .Where(m => m.genre != null)
                .SelectMany(m => m.genre)
                .Distinct()
                .OrderBy(g => g)
                .ToList();
        }

        /// <summary>
        /// Get movie by title (from cached data)
        /// </summary>
        public Movie GetMovieByTitle(string title)
        {
            if (!_isLoaded || _allMovies == null || string.IsNullOrWhiteSpace(title))
                return null;

            return _allMovies.FirstOrDefault(m =>
                m.title.Equals(title, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Clear cache and force reload (use sparingly)
        /// </summary>
        public void ClearCache()
        {
            System.Diagnostics.Debug.WriteLine("🧹 Clearing MovieDataService cache");
            _allMovies?.Clear();
            _allMovies = null;
            _isLoaded = false;
            _isLoading = false;
            _loadingTask = null;

            // Also clear MovieAutomation cache
            MovieAutomation.ClearCache();
        }
    }
}