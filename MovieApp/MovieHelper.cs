using System;
using System.Collections.Generic;

namespace MovieApp
{
    public static class MovieHelper
    {
        /// Creates a Movie object from similar movie data
        public static Movie CreateFromSimilarMovie(
            string title,
            string posterUrl,
            List<string> genres,
            string storyline,
            int year,
            string director,
            double rating)
        {
            return new Movie
            {
                title = title ?? "Unknown",
                poster = posterUrl ?? "", // Set poster, not ImageFileName
                genre = genres ?? new List<string>(),
                storyline = storyline ?? "No description available.",
                year = year,
                director = director ?? "Unknown",
                rating = rating,
                emoji = GetEmojiForGenre(genres)
            };
        }

        /// Creates a Movie with minimal required data
        public static Movie CreateMinimal(string title, string posterUrl)
        {
            return new Movie
            {
                title = title ?? "Unknown",
                poster = posterUrl ?? "",
                genre = new List<string>(),
                storyline = "No description available.",
                year = 0,
                director = "Unknown",
                rating = 0,
                emoji = "🎬"
            };
        }

        /// <summary>
        /// Gets an appropriate emoji for the movie's primary genre
        /// Made public so TMDBService can use it
        /// </summary>
        public static string GetEmojiForGenre(List<string> genres)
        {
            if (genres == null || genres.Count == 0)
                return "🎬";

            var genreEmojiMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Action", "💥" },
                { "Adventure", "🗺️" },
                { "Animation", "🎨" },
                { "Comedy", "😂" },
                { "Crime", "🔫" },
                { "Documentary", "📽️" },
                { "Drama", "🎭" },
                { "Family", "👨‍👩‍👧‍👦" },
                { "Fantasy", "🧙" },
                { "Horror", "👻" },
                { "Mystery", "🔍" },
                { "Romance", "❤️" },
                { "Sci-Fi", "🚀" },
                { "Science Fiction", "🚀" }, // TMDB uses "Science Fiction"
                { "Thriller", "😱" },
                { "Western", "🤠" },
                { "War", "⚔️" },
                { "Musical", "🎵" },
                { "Music", "🎵" },
                { "Biography", "📖" },
                { "History", "🏛️" },
                { "Sport", "⚽" },
                { "TV Movie", "📺" }
            };

            // Return emoji for first matching genre
            foreach (var genre in genres)
            {
                if (genreEmojiMap.TryGetValue(genre, out string emoji))
                    return emoji;
            }

            return "🎬"; // Default
        }
    }
}