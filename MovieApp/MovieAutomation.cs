using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MovieApp
{
    public static class MovieAutomation
    {
        private static List<Movie> _movies;
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string TMDB_API_KEY = "fbcdb95be1bd9b050b61dfd15153ce85"; 
        private const string TMDB_BASE_URL = "https://api.themoviedb.org/3";

        // =====================
        // TMDB RESPONSE MODELS
        // =====================
        private class TMDBMovieResponse
        {
            [JsonPropertyName("results")]
            public List<TMDBMovie> Results { get; set; }

            [JsonPropertyName("page")]
            public int Page { get; set; }

            [JsonPropertyName("total_pages")]
            public int TotalPages { get; set; }
        }

        private class TMDBMovie
        {
            [JsonPropertyName("id")]
            public int Id { get; set; }

            [JsonPropertyName("title")]
            public string Title { get; set; }

            [JsonPropertyName("release_date")]
            public string ReleaseDate { get; set; }

            [JsonPropertyName("poster_path")]
            public string PosterPath { get; set; }

            [JsonPropertyName("backdrop_path")]
            public string BackdropPath { get; set; }

            [JsonPropertyName("overview")]
            public string Overview { get; set; }

            [JsonPropertyName("vote_average")]
            public double VoteAverage { get; set; }

            [JsonPropertyName("genre_ids")]
            public List<int> GenreIds { get; set; }
        }

        private class TMDBMovieDetails
        {
            [JsonPropertyName("id")]
            public int Id { get; set; }

            [JsonPropertyName("title")]
            public string Title { get; set; }

            [JsonPropertyName("release_date")]
            public string ReleaseDate { get; set; }

            [JsonPropertyName("poster_path")]
            public string PosterPath { get; set; }

            [JsonPropertyName("overview")]
            public string Overview { get; set; }

            [JsonPropertyName("vote_average")]
            public double VoteAverage { get; set; }

            [JsonPropertyName("genres")]
            public List<TMDBGenre> Genres { get; set; }

            [JsonPropertyName("runtime")]
            public int Runtime { get; set; }

            [JsonPropertyName("credits")]
            public TMDBCredits Credits { get; set; }
        }

        private class TMDBGenre
        {
            [JsonPropertyName("id")]
            public int Id { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; }
        }

        private class TMDBCredits
        {
            [JsonPropertyName("crew")]
            public List<TMDBCrewMember> Crew { get; set; }
        }

        private class TMDBCrewMember
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("job")]
            public string Job { get; set; }
        }

        // Genre ID mapping (TMDB uses IDs for genres in their responses)
        private static readonly Dictionary<int, string> GenreMap = new Dictionary<int, string>
        {
            { 28, "Action" },
            { 12, "Adventure" },
            { 16, "Animation" },
            { 35, "Comedy" },
            { 80, "Crime" },
            { 99, "Documentary" },
            { 18, "Drama" },
            { 10751, "Family" },
            { 14, "Fantasy" },
            { 36, "History" },
            { 27, "Horror" },
            { 10402, "Musical" },
            { 9648, "Mystery" },
            { 10749, "Romance" },
            { 878, "Sci-Fi" },
            { 10770, "TV Movie" },
            { 53, "Thriller" },
            { 10752, "War" },
            { 37, "Western" }
        };

        // =====================
        // PUBLIC API METHODS
        // =====================

   
        public static async Task<List<Movie>> GetAllMoviesAsync()
        {
            if (_movies != null)
                return _movies;

            try
            {
                System.Diagnostics.Debug.WriteLine("🔄 Fetching movies from TMDB API...");

                // Fetch a combination of popular and top rated movies
                var popularMovies = await FetchPopularMoviesAsync(pages: 3);
                var topRatedMovies = await FetchTopRatedMoviesAsync(pages: 2);

                // Combine and remove duplicates
                _movies = popularMovies
                    .Concat(topRatedMovies)
                    .GroupBy(m => m.title)
                    .Select(g => g.First())
                    .OrderByDescending(m => m.rating)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"✅ Loaded {_movies?.Count ?? 0} movies from TMDB API");

                if (_movies != null && _movies.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"First movie: {_movies[0].title}");
                    System.Diagnostics.Debug.WriteLine($"Poster path: {_movies[0].poster}");
                }

                return _movies ?? new List<Movie>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error loading movies from TMDB: {ex.Message}");
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
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await GetAllMoviesAsync();
            }

            try
            {
                // Search using TMDB API for better results
                var apiResults = await FetchSearchResultsAsync(searchTerm);

                // Also search local cache
                var movies = await GetAllMoviesAsync();
                searchTerm = searchTerm.ToLower();
                var localResults = movies.Where(m =>
                    m.title.ToLower().Contains(searchTerm) ||
                    (m.director?.ToLower().Contains(searchTerm) ?? false) ||
                    (m.genre?.Any(g => g.ToLower().Contains(searchTerm)) ?? false)
                ).ToList();

                // Combine results, prioritizing API results
                return apiResults
                    .Concat(localResults)
                    .GroupBy(m => m.title)
                    .Select(g => g.First())
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Search error: {ex.Message}");

                // Fallback to local search if API fails
                var movies = await GetAllMoviesAsync();
                searchTerm = searchTerm.ToLower();
                return movies.Where(m =>
                    m.title.ToLower().Contains(searchTerm) ||
                    (m.director?.ToLower().Contains(searchTerm) ?? false) ||
                    (m.genre?.Any(g => g.ToLower().Contains(searchTerm)) ?? false)
                ).ToList();
            }
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


        public static async Task<List<Movie>> GetSimilarMoviesAsync(string movieTitle, int count = 10)
        {
            try
            {
                // First, search for the movie to get its TMDB ID
                var searchUrl = $"{TMDB_BASE_URL}/search/movie?api_key={TMDB_API_KEY}&query={Uri.EscapeDataString(movieTitle)}";
                var searchResponse = await _httpClient.GetStringAsync(searchUrl);

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var searchResult = JsonSerializer.Deserialize<TMDBMovieResponse>(searchResponse, options);

                if (searchResult?.Results == null || searchResult.Results.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Could not find movie: {movieTitle}");
                    return new List<Movie>();
                }

                var movieId = searchResult.Results[0].Id;

                // Now fetch similar movies from TMDB
                var similarUrl = $"{TMDB_BASE_URL}/movie/{movieId}/similar?api_key={TMDB_API_KEY}";
                var similarResponse = await _httpClient.GetStringAsync(similarUrl);
                var similarResult = JsonSerializer.Deserialize<TMDBMovieResponse>(similarResponse, options);

                if (similarResult?.Results != null)
                {
                    var similarMovies = similarResult.Results
                        .Take(count)
                        .Select(ConvertToMovie)
                        .ToList();

                    System.Diagnostics.Debug.WriteLine($"✅ Found {similarMovies.Count} similar movies for '{movieTitle}'");
                    return similarMovies;
                }

                return new List<Movie>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error fetching similar movies: {ex.Message}");
                return new List<Movie>();
            }
        }

   
        public static async Task<List<Movie>> GetRecommendedMoviesAsync(string movieTitle, int count = 10)
        {
            try
            {
                // First, search for the movie to get its TMDB ID
                var searchUrl = $"{TMDB_BASE_URL}/search/movie?api_key={TMDB_API_KEY}&query={Uri.EscapeDataString(movieTitle)}";
                var searchResponse = await _httpClient.GetStringAsync(searchUrl);

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var searchResult = JsonSerializer.Deserialize<TMDBMovieResponse>(searchResponse, options);

                if (searchResult?.Results == null || searchResult.Results.Count == 0)
                {
                    return new List<Movie>();
                }

                var movieId = searchResult.Results[0].Id;

                // Fetch recommendations from TMDB
                var recUrl = $"{TMDB_BASE_URL}/movie/{movieId}/recommendations?api_key={TMDB_API_KEY}";
                var recResponse = await _httpClient.GetStringAsync(recUrl);
                var recResult = JsonSerializer.Deserialize<TMDBMovieResponse>(recResponse, options);

                if (recResult?.Results != null)
                {
                    return recResult.Results
                        .Take(count)
                        .Select(ConvertToMovie)
                        .ToList();
                }

                return new List<Movie>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error fetching recommendations: {ex.Message}");
                return new List<Movie>();
            }
        }



        /// Fetch popular movies from TMDB
        private static async Task<List<Movie>> FetchPopularMoviesAsync(int pages = 3)
        {
            var allMovies = new List<Movie>();

            try
            {
                for (int page = 1; page <= pages; page++)
                {
                    var url = $"{TMDB_BASE_URL}/movie/popular?api_key={TMDB_API_KEY}&page={page}";
                    var response = await _httpClient.GetStringAsync(url);

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var result = JsonSerializer.Deserialize<TMDBMovieResponse>(response, options);

                    if (result?.Results != null)
                    {
                        foreach (var tmdbMovie in result.Results)
                        {
                            allMovies.Add(ConvertToMovie(tmdbMovie));
                        }
                    }

                    // Small delay to avoid rate limiting
                    await Task.Delay(100);
                }

                return allMovies;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error fetching popular movies: {ex.Message}");
                return new List<Movie>();
            }
        }

        /// Fetch top rated movies from TMDB
        private static async Task<List<Movie>> FetchTopRatedMoviesAsync(int pages = 2)
        {
            var allMovies = new List<Movie>();

            try
            {
                for (int page = 1; page <= pages; page++)
                {
                    var url = $"{TMDB_BASE_URL}/movie/top_rated?api_key={TMDB_API_KEY}&page={page}";
                    var response = await _httpClient.GetStringAsync(url);

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var result = JsonSerializer.Deserialize<TMDBMovieResponse>(response, options);

                    if (result?.Results != null)
                    {
                        foreach (var tmdbMovie in result.Results)
                        {
                            allMovies.Add(ConvertToMovie(tmdbMovie));
                        }
                    }

                    await Task.Delay(100);
                }

                return allMovies;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error fetching top rated: {ex.Message}");
                return new List<Movie>();
            }
        }

        /// Search movies by query
        private static async Task<List<Movie>> FetchSearchResultsAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<Movie>();

            try
            {
                var encodedQuery = Uri.EscapeDataString(query);
                var url = $"{TMDB_BASE_URL}/search/movie?api_key={TMDB_API_KEY}&query={encodedQuery}";
                var response = await _httpClient.GetStringAsync(url);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var result = JsonSerializer.Deserialize<TMDBMovieResponse>(response, options);

                if (result?.Results != null)
                {
                    return result.Results.Select(ConvertToMovie).ToList();
                }

                return new List<Movie>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error searching movies: {ex.Message}");
                return new List<Movie>();
            }
        }

        /// Get detailed movie information including director
        private static async Task<Movie> FetchMovieDetailsAsync(int movieId)
        {
            try
            {
                var url = $"{TMDB_BASE_URL}/movie/{movieId}?api_key={TMDB_API_KEY}&append_to_response=credits";
                var response = await _httpClient.GetStringAsync(url);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var details = JsonSerializer.Deserialize<TMDBMovieDetails>(response, options);

                if (details != null)
                {
                    return ConvertDetailsToMovie(details);
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error fetching movie details: {ex.Message}");
                return null;
            }
        }

        // =====================
        // CONVERSION METHODS
        // =====================

        /// Convert basic TMDB movie to our Movie model
        private static Movie ConvertToMovie(TMDBMovie tmdbMovie)
        {
            var genres = tmdbMovie.GenreIds?
                .Where(id => GenreMap.ContainsKey(id))
                .Select(id => GenreMap[id])
                .ToList() ?? new List<string>();

            int year = 0;
            if (!string.IsNullOrEmpty(tmdbMovie.ReleaseDate) && tmdbMovie.ReleaseDate.Length >= 4)
            {
                int.TryParse(tmdbMovie.ReleaseDate.Substring(0, 4), out year);
            }

            return new Movie
            {
                title = tmdbMovie.Title ?? "Unknown",
                year = year,
                genre = genres,
                director = "Unknown", // Basic response doesn't include director
                rating = Math.Round(tmdbMovie.VoteAverage, 1),
                emoji = MovieHelper.GetEmojiForGenre(genres),
                poster = tmdbMovie.PosterPath ?? "", // Store just the path (e.g., "/abc123.jpg")
                storyline = tmdbMovie.Overview ?? "No description available."
            };
        }

        /// Convert detailed TMDB movie to our Movie model
        private static Movie ConvertDetailsToMovie(TMDBMovieDetails details)
        {
            var genres = details.Genres?
                .Select(g => g.Name)
                .ToList() ?? new List<string>();

            int year = 0;
            if (!string.IsNullOrEmpty(details.ReleaseDate) && details.ReleaseDate.Length >= 4)
            {
                int.TryParse(details.ReleaseDate.Substring(0, 4), out year);
            }

            // Find director from crew
            var director = details.Credits?.Crew?
                .FirstOrDefault(c => c.Job == "Director")?.Name ?? "Unknown";

            return new Movie
            {
                title = details.Title ?? "Unknown",
                year = year,
                genre = genres,
                director = director,
                rating = Math.Round(details.VoteAverage, 1),
                emoji = MovieHelper.GetEmojiForGenre(genres),
                poster = details.PosterPath ?? "",
                storyline = details.Overview ?? "No description available."
            };
        }
    }
}