using System;

namespace MovieApp
{
    /// <summary>
    /// Helper class for constructing TMDB image URLs from poster paths.
    /// TMDB returns poster paths like "/abc123.jpg" which need to be combined
    /// with the base URL and size to create complete image URLs.
    /// </summary>
    public static class TMDBImageHelper
    {
        // TMDB Image Base URL (https://developer.themoviedb.org/docs/image-basics)
        private const string TMDB_IMAGE_BASE_URL = "https://image.tmdb.org/t/p/";

        // Available poster sizes (in order of quality)
        // w92, w154, w185, w342, w500, w780, original
        public enum PosterSize
        {
            Small,      
            Medium,    
            Large,      
            ExtraLarge, 
            Original   
        }

        /// Converts a TMDB poster path to a full URL
        public static string GetPosterUrl(string posterPath, PosterSize size = PosterSize.Medium)
        {
            if (string.IsNullOrWhiteSpace(posterPath))
                return null;

            if (posterPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                posterPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return posterPath;
            }

            if (!posterPath.StartsWith("/"))
                posterPath = "/" + posterPath;

            string sizeString = size switch
            {
                PosterSize.Small => "w185",
                PosterSize.Medium => "w342",
                PosterSize.Large => "w500",
                PosterSize.ExtraLarge => "w780",
                PosterSize.Original => "original",
                _ => "w342"
            };

            return $"{TMDB_IMAGE_BASE_URL}{sizeString}{posterPath}";
        }

        /// Checks if a path looks like a TMDB poster path
        public static bool IsTMDBPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            return path.StartsWith("/") &&
                   (path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(".png", StringComparison.OrdinalIgnoreCase));
        }

        /// Gets the appropriate poster URL with smart detection
        public static string GetSmartPosterUrl(string posterPath, PosterSize size = PosterSize.Medium)
        {
            if (string.IsNullOrWhiteSpace(posterPath))
                return null;

            if (posterPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                posterPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return posterPath;
            }

            if (IsTMDBPath(posterPath))
            {
                return GetPosterUrl(posterPath, size);
            }

            return posterPath;
        }

        /// Gets backdrop URL from TMDB path
        public static string GetBackdropUrl(string backdropPath, bool isLarge = false)
        {
            if (string.IsNullOrWhiteSpace(backdropPath))
                return null;

            if (backdropPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                backdropPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return backdropPath;
            }

            if (!backdropPath.StartsWith("/"))
                backdropPath = "/" + backdropPath;

            string size = isLarge ? "w1280" : "w780";
            return $"{TMDB_IMAGE_BASE_URL}{size}{backdropPath}";
        }

        /// Gets profile image URL for cast members
        public static string GetProfileUrl(string profilePath, bool isLarge = false)
        {
            if (string.IsNullOrWhiteSpace(profilePath))
                return null;

            if (profilePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                profilePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return profilePath;
            }

            if (!profilePath.StartsWith("/"))
                profilePath = "/" + profilePath;

            string size = isLarge ? "h632" : "w185";
            return $"{TMDB_IMAGE_BASE_URL}{size}{profilePath}";
        }
    }
}