using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace MovieApp
{
    /// <summary>
    /// Movie data model representing a movie entity
    /// </summary>
    public class Movie
    {
        [JsonPropertyName("title")]
        public string title { get; set; }

        [JsonPropertyName("year")]
        public int year { get; set; }

        [JsonPropertyName("genre")]
        public List<string> genre { get; set; }

        [JsonPropertyName("director")]
        public string director { get; set; }

        [JsonPropertyName("rating")]
        public double rating { get; set; }

        [JsonPropertyName("emoji")]
        public string emoji { get; set; }

        [JsonPropertyName("poster")]
        public string poster { get; set; }

        [JsonPropertyName("storyline")]
        public string storyline { get; set; }

        // Computed properties for UI display

        /// <summary>
        /// Gets the movie name (same as title)
        /// </summary>
        public string name => title;

        /// <summary>
        /// Gets the image file name from poster property
        /// </summary>
        public string ImageFileName => poster;

        /// <summary>
        /// Gets formatted genre string for display
        /// </summary>
        public string FormattedGenres => genre != null && genre.Count > 0
            ? string.Join(" | ", genre)
            : "Unknown";
    }
}