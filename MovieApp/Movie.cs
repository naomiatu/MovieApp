using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace MovieApp
{
    public class Movie
    {
        public string name { get; set; }
        public string storyline { get; set; }
        public List<string> actors { get; set; }
        [JsonPropertyName("categories")]
        public List<string> genre { get; set; }
        [JsonPropertyName("release-date")]
        public string releaseDate { get; set; }

        public double imDbRating { get; set; }
        public int year { get; set; }
        public string trailerUrl { get; set; }
        public int runtime { get; set; }
        public string director { get; set; }
        public string mpaaRating { get; set; }
        // Generate image filename from movie name if not provided
        private string _imageFileName;
        public string ImageFileName
        {
            get
            {
                if (!string.IsNullOrEmpty(_imageFileName))
                    return _imageFileName;

                // Generate from movie name
                if (!string.IsNullOrEmpty(name))
                {
                    return name.ToLower()
                        .Replace(" ", "_")
                        .Replace(":", "")
                        .Replace("&", "and")
                        .Replace("'", "")
                        .Replace(",", "")
                        + ".jpg";
                }
                return "placeholder.jpg";
            }
            set { _imageFileName = value; }
        }

        // Property to get formatted genres
        public string FormattedGenres
        {
            get
            {
                if (genre == null || genre.Count == 0)
                    return "Movie";

                return "Movie | " + string.Join(" | ", genre);
            }
        }

        public string FormattedRuntime => runtime > 0
            ? $"{runtime} min"
            : "Unknown";

        public string FormattedRating => imDbRating > 0
            ? $"⭐ {imDbRating}/10"
            : "Not rated";

        public string FormattedYear => year > 0
            ? year.ToString()
            : "Unknown";
    }
}