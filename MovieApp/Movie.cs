using System.Collections.Generic;
using System.Linq;

namespace MovieApp
{
    public class Movie
    {
        public string name { get; set; }
        public string storyline { get; set; }
        public List<string> actors { get; set; }
        public List<string> genre { get; set; }

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

        // Property to get image file name based on movie name
        public string ImageFileName
        {
            get
            {
                // Converts movie name to lowercase and replaces spaces with underscores
                return name?.ToLower().Replace(" ", "_").Replace(":", "").Replace("'", "") + ".jpg" ?? "dotnet_bot.png";
            }
        }
    }
}