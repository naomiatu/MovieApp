using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace MovieApp
{
    public static class MovieAutomation
    {
        private static List<Movie> _allMovies;

        // Loads all JSON files from the Resources folder
        public static async Task<List<Movie>> GetAllMoviesAsync()
        {
            if (_allMovies != null)
                return _allMovies;

            _allMovies = new List<Movie>();

            try
            {
                // Gets all JSON files from the Raw resources folder
                var assembly = Assembly.GetExecutingAssembly();
                var resourceNames = assembly.GetManifestResourceNames()
                    .Where(r => r.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var resourceName in resourceNames)
                {
                    using var stream = assembly.GetManifestResourceStream(resourceName);
                    if (stream != null)
                    {
                        using var reader = new StreamReader(stream);
                        var json = await reader.ReadToEndAsync();

                        var movie = JsonSerializer.Deserialize<Movie>(json, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (movie != null)
                        {
                            _allMovies.Add(movie);
                        }
                    }
                }

                if (_allMovies.Count == 0)
                {
                    await LoadFromFileSystemAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading movies: {ex.Message}");
            }

            return _allMovies;
        }

        private static async Task LoadFromFileSystemAsync()
        {
            try
            {
                var folderPath = Path.Combine(FileSystem.AppDataDirectory, "movies");

                var appPath = AppDomain.CurrentDomain.BaseDirectory;
                var resourcePath = Path.Combine(appPath, "Resources", "Raw");

                string[] searchPaths = { folderPath, resourcePath };

                foreach (var path in searchPaths)
                {
                    if (Directory.Exists(path))
                    {
                        var jsonFiles = Directory.GetFiles(path, "*.json");

                        foreach (var file in jsonFiles)
                        {
                            var json = await File.ReadAllTextAsync(file);
                            var movie = JsonSerializer.Deserialize<Movie>(json, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                            if (movie != null)
                            {
                                _allMovies.Add(movie);
                            }
                        }

                        if (_allMovies.Count > 0)
                            break; // if movies are found it breaks as theres no need to check other paths
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading from file system: {ex.Message}");
            }
        }

        // Get a specific movie by name
        public static async Task<Movie> GetMovieByNameAsync(string movieName)
        {
            var movies = await GetAllMoviesAsync();
            return movies.FirstOrDefault(m =>
                m.name.Equals(movieName, StringComparison.OrdinalIgnoreCase));
        }

        // Clears cache to force reload
        public static void ClearCache()
        {
            _allMovies = null;
        }
    }
}