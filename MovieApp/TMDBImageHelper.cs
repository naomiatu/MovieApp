using System;

namespace MovieApp
{
    /// <summary>
    /// Helper class for constructing TMDB image URLs from poster paths.
    ///  Uses smaller image sizes by default to reduce RAM usage.
    /// </summary>
    public static class TMDBImageHelper
    {
        private const string TMDB_IMAGE_BASE_URL = "https://image.tmdb.org/t/p/";

        public enum PosterSize
        {
            Thumbnail,  
            Small,    
            Medium,     
            Large,      
            ExtraLarge, 
            Original    
        }

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
                PosterSize.Thumbnail => "w92",
                PosterSize.Small => "w185",
                PosterSize.Medium => "w342",
                PosterSize.Large => "w500",
                PosterSize.ExtraLarge => "w780",
                PosterSize.Original => "original",
                _ => "w342"
            };

            return $"{TMDB_IMAGE_BASE_URL}{sizeString}{posterPath}";
        }

        public static bool IsTMDBPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            return path.StartsWith("/") &&
                   (path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(".png", StringComparison.OrdinalIgnoreCase));
        }

        public static string GetSmartPosterUrl(string posterPath, PosterSize size = PosterSize.Medium)
        {
            if (string.IsNullOrWhiteSpace(posterPath))
                return null;

            if (posterPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                posterPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return posterPath;
            }

            return IsTMDBPath(posterPath) ? GetPosterUrl(posterPath, size) : posterPath;
        }

        public static string GetBackdropUrl(string backdropPath, bool isLarge = false)
        {
            if (string.IsNullOrWhiteSpace(backdropPath)) return null;
            if (!backdropPath.StartsWith("/")) backdropPath = "/" + backdropPath;

            string size = isLarge ? "w780" : "w500";
            return $"{TMDB_IMAGE_BASE_URL}{size}{backdropPath}";
        }

        public static string GetProfileUrl(string profilePath, bool isLarge = false)
        {
            if (string.IsNullOrWhiteSpace(profilePath)) return null;
            if (!profilePath.StartsWith("/")) profilePath = "/" + profilePath;

            string size = isLarge ? "w185" : "w92";
            return $"{TMDB_IMAGE_BASE_URL}{size}{profilePath}";
        }

        public static string GetThumbnailUrl(string posterPath) => GetPosterUrl(posterPath, PosterSize.Thumbnail);
        public static string GetListPosterUrl(string posterPath) => GetPosterUrl(posterPath, PosterSize.Small);
        public static string GetDetailPosterUrl(string posterPath) => GetPosterUrl(posterPath, PosterSize.Medium);
    }
}