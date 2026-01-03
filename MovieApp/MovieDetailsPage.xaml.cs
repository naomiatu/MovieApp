using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using MovieProject;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MovieApp
{
    public partial class MovieDetailsPage : ContentPage
    {
        private Movie _currentMovie;
        private MovieReview _currentReview;
        private Button[] _starButtons;
        private Button[] _emojiButtons;
        private List<SimilarMovie> _allSimilarMovies;

        public MovieDetailsPage()
        {
            InitializeComponent();
            InitializeButtons();
        }

        // Constructor that accepts a Movie object
        public MovieDetailsPage(Movie movie) : this()
        {
            _currentMovie = movie;
            UpdateUI();
            _ = LoadReviewAsync();
            _ = LoadSimilarMovies();
            _ = LoadCastMembers();
        }

        private void InitializeButtons()
        {
            _starButtons = new[] { Star1, Star2, Star3, Star4, Star5 };
            _emojiButtons = new[] { Emoji1, Emoji2, Emoji3, Emoji4, Emoji5, Emoji6 };
        }

        #region Load/Save Review
        private async Task LoadReviewAsync()
        {
            if (_currentMovie == null) return;

            try
            {
                var json = await SecureStorage.GetAsync($"review_{_currentMovie.title}");
                _currentReview = !string.IsNullOrEmpty(json)
                    ? JsonSerializer.Deserialize<MovieReview>(json)
                    : new MovieReview { MovieName = _currentMovie.title };

                UpdateReviewUI();
            }
            catch
            {
                _currentReview = new MovieReview { MovieName = _currentMovie.title };
            }
        }

        private async Task SaveReviewAsync()
        {
            if (_currentReview == null || _currentMovie == null) return;

            var json = JsonSerializer.Serialize(_currentReview);
            await SecureStorage.SetAsync($"review_{_currentMovie.title}", json);

            if (_currentReview.IsWatched || _currentReview.Rating > 0)
            {
                await AddToWatchedListAsync();
            }
        }

        private async Task AddToWatchedListAsync()
        {
            try
            {
                var watchedJson = await SecureStorage.GetAsync("watched_movies");
                var watchedList = string.IsNullOrEmpty(watchedJson)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(watchedJson);

                if (!watchedList.Contains(_currentMovie.title))
                {
                    watchedList.Add(_currentMovie.title);
                    await SecureStorage.SetAsync("watched_movies", JsonSerializer.Serialize(watchedList));
                }
            }
            catch { }
        }
        #endregion

        #region UI Updates
        private void UpdateUI()
        {
            if (_currentMovie == null) return;

            // Handle poster image - check if it's a URL or local file
            try
            {
                if (!string.IsNullOrWhiteSpace(_currentMovie.poster))
                {
                    if (_currentMovie.poster.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                        _currentMovie.poster.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        MoviePoster.Source = new UriImageSource
                        {
                            Uri = new Uri(_currentMovie.poster),
                            CachingEnabled = true,
                            CacheValidity = TimeSpan.FromDays(1)
                        };
                    }
                    else
                    {
                        MoviePoster.Source = _currentMovie.poster;
                    }
                }
                else
                {
                    // Set a placeholder if no image
                    MoviePoster.Source = "placeholder_movie.png";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading poster: {ex.Message}");
                MoviePoster.Source = "placeholder_movie.png";
            }

            MovieTitle.Text = _currentMovie.title ?? "Unknown Movie";

            // Handle genres
            if (_currentMovie.genre != null && _currentMovie.genre.Count > 0)
            {
                MovieGenres.Text = string.Join(" • ", _currentMovie.genre);
            }
            else
            {
                MovieGenres.Text = "Genre not available";
            }

            MovieDescription.Text = _currentMovie.storyline ?? "No description available.";
        }

        private void UpdateReviewUI()
        {
            if (_currentReview == null) return;

            UpdateStarDisplay(_currentReview.Rating);
            UpdateEmojiDisplay(_currentReview.SelectedEmojis);
            WatchedBadge.IsVisible = _currentReview.IsWatched;
            WatchedButton.Text = _currentReview.IsWatched ? "✓ Watched" : "Mark as Watched";
            WatchedButton.BackgroundColor = _currentReview.IsWatched
                ? Color.FromArgb("#2196F3")
                : Color.FromArgb("#4CAF50");
        }

        private void UpdateStarDisplay(int rating)
        {
            for (int i = 0; i < _starButtons.Length; i++)
            {
                _starButtons[i].Text = i < rating ? "★" : "☆";
                _starButtons[i].TextColor = i < rating ? Color.FromArgb("#FFB800") : Color.FromArgb("#666");
            }
            RatingText.Text = rating > 0 ? $"{rating} star{(rating != 1 ? "s" : "")}" : "Not rated";
        }

        private void UpdateEmojiDisplay(List<string> selectedEmojis)
        {
            if (selectedEmojis == null) return;

            foreach (var button in _emojiButtons)
            {
                button.BackgroundColor = selectedEmojis.Contains(button.Text)
                    ? Color.FromArgb("#FFB800")
                    : Color.FromArgb("#2a2a2a");
            }
        }
        #endregion

        #region Button Events
        private async void Star_Clicked(object sender, EventArgs e)
        {
            if (sender is not Button clickedStar) return;

            _currentReview ??= new MovieReview { MovieName = _currentMovie?.title };
            int rating = Array.IndexOf(_starButtons, clickedStar) + 1;

            await clickedStar.ScaleTo(1.5, 100, Easing.CubicOut);
            await clickedStar.ScaleTo(1.0, 100, Easing.CubicIn);

            _currentReview.Rating = rating;
            _currentReview.DateReviewed = DateTime.Now;
            UpdateStarDisplay(rating);
            await SaveReviewAsync();

            await DisplayAlert("Rating Saved", $"You rated this movie {rating} star{(rating != 1 ? "s" : "")}!", "OK");
        }

        private async void Emoji_Clicked(object sender, EventArgs e)
        {
            if (sender is not Button clickedEmoji) return;

            _currentReview ??= new MovieReview { MovieName = _currentMovie?.title };
            string emoji = clickedEmoji.Text;

            if (_currentReview.SelectedEmojis.Contains(emoji))
            {
                _currentReview.SelectedEmojis.Remove(emoji);
                clickedEmoji.BackgroundColor = Color.FromArgb("#2a2a2a");
            }
            else
            {
                _currentReview.SelectedEmojis.Add(emoji);
                clickedEmoji.BackgroundColor = Color.FromArgb("#FFB800");
            }

            await clickedEmoji.ScaleTo(1.2, 80, Easing.CubicOut);
            await clickedEmoji.ScaleTo(1.0, 80, Easing.CubicIn);
            await SaveReviewAsync();
        }

        private async void ToggleWatched_Clicked(object sender, EventArgs e)
        {
            _currentReview ??= new MovieReview { MovieName = _currentMovie?.title };
            _currentReview.IsWatched = !_currentReview.IsWatched;

            if (_currentReview.IsWatched)
                _currentReview.DateWatched = DateTime.Now;

            UpdateReviewUI();
            await SaveReviewAsync();

            string message = _currentReview.IsWatched ? "Added to your watched list!" : "Removed from watched list";
            await DisplayAlert("Success", message, "OK");
        }

        private async void ShareReview_Clicked(object sender, EventArgs e)
        {
            if (_currentReview == null || (_currentReview.Rating == 0 && _currentReview.SelectedEmojis.Count == 0))
            {
                await DisplayAlert("No Review", "Please rate the movie or add reactions before sharing!", "OK");
                return;
            }

            string reviewText = $"🎬 {_currentMovie.title}\n";
            if (_currentReview.Rating > 0) reviewText += $"⭐ Rating: {_currentReview.Rating}/5 stars\n";
            if (_currentReview.SelectedEmojis.Count > 0) reviewText += $"Reactions: {string.Join(" ", _currentReview.SelectedEmojis)}\n";
            if (_currentReview.IsWatched && _currentReview.DateWatched.HasValue)
                reviewText += $"\n✓ Watched on {_currentReview.DateWatched.Value:MMM dd, yyyy}";

            try
            {
                await Share.Default.RequestAsync(new ShareTextRequest
                {
                    Text = reviewText,
                    Title = $"My review of {_currentMovie.title}"
                });
            }
            catch
            {
                await DisplayAlert("Error", "Could not share review", "OK");
            }
        }

        private void PlayMovie_Clicked(object sender, EventArgs e)
        {
            // Use trailer URL from movie if available, otherwise use default
            string trailerUrl = "https://www.youtube.com/watch?v=Ke1Y3P9D0Bc"; // Default trailer

            // Check if movie has a trailerUrl property
            var trailerProperty = _currentMovie?.GetType().GetProperty("trailerUrl");
            if (trailerProperty != null)
            {
                var url = trailerProperty.GetValue(_currentMovie) as string;
                if (!string.IsNullOrEmpty(url))
                    trailerUrl = url;
            }

            TrailerWebView.Source = new UrlWebViewSource { Url = trailerUrl };
            TrailerWebView.IsVisible = true;
            PlayButton.IsVisible = false;
        }

        private async void Back_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
        #endregion

        #region Cast Members
        private async Task LoadCastMembers()
        {
            // Check if Movie has a cast property
            var castProperty = _currentMovie?.GetType().GetProperty("cast");
            if (castProperty == null)
            {
                // Hide cast section if property doesn't exist
                CastSection.IsVisible = false;
                return;
            }

            var castList = castProperty.GetValue(_currentMovie) as List<CastMember>;

            if (castList == null || castList.Count == 0)
            {
                // Hide cast section if no cast data
                CastSection.IsVisible = false;
                return;
            }

            CastContainer.Children.Clear();

            foreach (var castMember in castList.Take(10)) // Limit to 10 cast members
            {
                var castCard = CreateCastCard(castMember);
                CastContainer.Children.Add(castCard);
            }
        }

        private VerticalStackLayout CreateCastCard(CastMember cast)
        {
            var container = new VerticalStackLayout
            {
                Spacing = 8,
                WidthRequest = 100
            };

            var imageBorder = new Border
            {
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 50 },
                Stroke = Color.FromArgb("#333"),
                StrokeThickness = 2,
                WidthRequest = 100,
                HeightRequest = 100,
                HorizontalOptions = LayoutOptions.Center,
                BackgroundColor = Color.FromArgb("#222")
            };

            var image = new Image
            {
                Aspect = Aspect.AspectFill
            };

            // Handle cast image URL
            if (!string.IsNullOrEmpty(cast.ImageUrl))
            {
                if (cast.ImageUrl.StartsWith("http://") || cast.ImageUrl.StartsWith("https://"))
                {
                    image.Source = new UriImageSource
                    {
                        Uri = new Uri(cast.ImageUrl),
                        CachingEnabled = true,
                        CacheValidity = TimeSpan.FromDays(1)
                    };
                }
                else
                {
                    image.Source = cast.ImageUrl;
                }
            }
            else
            {
                image.Source = "placeholder_avatar.png";
            }

            imageBorder.Content = image;
            container.Children.Add(imageBorder);

            var nameLabel = new Label
            {
                Text = cast.Name ?? "Unknown",
                FontSize = 12,
                TextColor = Colors.White,
                HorizontalTextAlignment = TextAlignment.Center,
                LineBreakMode = LineBreakMode.TailTruncation,
                MaxLines = 2
            };

            container.Children.Add(nameLabel);

            return container;
        }
        #endregion

        #region Similar Movies
        private async Task LoadSimilarMovies()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();

                // Try multiple possible resource names
                var possibleNames = new[]
                {
                    "MovieApp.Data.similar_movies.json",
                    "MovieApp.similar_movies.json",
                    "similar_movies.json"
                };

                Stream stream = null;
                foreach (var resourceName in possibleNames)
                {
                    stream = assembly.GetManifestResourceStream(resourceName);
                    if (stream != null) break;
                }

                if (stream == null)
                {
                    // If JSON file doesn't exist, hide similar movies section
                    SimilarMoviesSection.IsVisible = false;
                    return;
                }

                using (stream)
                using (var reader = new StreamReader(stream))
                {
                    string json = await reader.ReadToEndAsync();

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    _allSimilarMovies = JsonSerializer.Deserialize<List<SimilarMovie>>(json, options);

                    if (_allSimilarMovies == null || _allSimilarMovies.Count == 0)
                    {
                        SimilarMoviesSection.IsVisible = false;
                        return;
                    }

                    // Filter similar movies by matching genres
                    var filteredMovies = FilterSimilarMoviesByGenre();

                    if (filteredMovies.Count == 0)
                    {
                        // If no matches, show random selection
                        filteredMovies = _allSimilarMovies
                            .OrderBy(x => Guid.NewGuid())
                            .Take(10)
                            .ToList();
                    }

                    SimilarMoviesCollection.ItemsSource = filteredMovies;
                }
            }
            catch (Exception ex)
            {
                // Hide section on error instead of showing alert
                SimilarMoviesSection.IsVisible = false;
                System.Diagnostics.Debug.WriteLine($"Failed to load similar movies: {ex.Message}");
            }
        }

        private List<SimilarMovie> FilterSimilarMoviesByGenre()
        {
            if (_currentMovie?.genre == null || _currentMovie.genre.Count == 0 || _allSimilarMovies == null)
                return new List<SimilarMovie>();

            // Get movies that share at least one genre with current movie
            var similarMovies = _allSimilarMovies
                .Where(sm => sm.Genre != null &&
                            sm.Genre.Any(g => _currentMovie.genre.Contains(g, StringComparer.OrdinalIgnoreCase)) &&
                            sm.Title != _currentMovie.title) // Exclude current movie
                .OrderByDescending(sm => sm.Genre.Count(g => _currentMovie.genre.Contains(g, StringComparer.OrdinalIgnoreCase)))
                .ThenByDescending(sm => sm.Rating) // Secondary sort by rating
                .Take(10)
                .ToList();

            return similarMovies;
        }

        private async void OnSimilarMovieSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.Count == 0) return;

            var selected = e.CurrentSelection[0] as SimilarMovie;
            if (selected != null)
            {
                try
                {
                    // Use helper to create Movie from SimilarMovie
                    var movie = MovieHelper.CreateFromSimilarMovie(
                        selected.Title ?? "Unknown",
                        selected.Poster ?? "",
                        selected.Genre ?? new List<string>(),
                        selected.Storyline ?? "No description available.",
                        selected.Year,
                        selected.Director ?? "Unknown",
                        selected.Rating
                    );

                    await Navigation.PushAsync(new MovieDetailsPage(movie));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error creating movie: {ex.Message}");
                    await DisplayAlert("Error", "Unable to load movie details", "OK");
                }
            }

            ((CollectionView)sender).SelectedItem = null;
        }
        #endregion

        #region Clear Cache & Sign Out
        private async void ClearCache_Tapped(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert(
                "Clear Cache",
                "This will reset all app settings and remove saved reviews. Continue?",
                "Yes", "No");

            if (!confirm) return;

            try
            {
                Preferences.Clear();
                SecureStorage.RemoveAll();

                await DisplayAlert("Success", "Cache cleared successfully.", "OK");

                Application.Current.MainPage = new NavigationPage(new SplashPage());
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to clear cache: {ex.Message}", "OK");
            }
        }

        private async void SignOut_Tapped(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert(
                "Sign Out",
                "Are you sure you want to sign out?",
                "Yes", "No");

            if (!confirm) return;

            try
            {
                Preferences.Remove("username");
                Preferences.Remove("IsDarkTheme");

                await DisplayAlert("Signed Out", "You have been signed out.", "OK");

                Application.Current.MainPage = new NavigationPage(new SplashPage());
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to sign out: {ex.Message}", "OK");
            }
        }
        #endregion
    }

    #region Data Models
    public class MovieReview
    {
        public string MovieName { get; set; }
        public int Rating { get; set; }
        public List<string> SelectedEmojis { get; set; } = new List<string>();
        public bool IsWatched { get; set; }
        public DateTime? DateWatched { get; set; }
        public DateTime? DateReviewed { get; set; }
    }

    public class SimilarMovie
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("year")]
        public int Year { get; set; }

        [JsonPropertyName("genre")]
        public List<string> Genre { get; set; }

        [JsonPropertyName("director")]
        public string Director { get; set; }

        [JsonPropertyName("rating")]
        public double Rating { get; set; }

        [JsonPropertyName("emoji")]
        public string Emoji { get; set; }

        [JsonPropertyName("poster")]
        public string Poster { get; set; }

        [JsonPropertyName("storyline")]
        public string Storyline { get; set; }
    }

    public class CastMember
    {
        public string Name { get; set; }
        public string ImageUrl { get; set; }
        public string Character { get; set; }
    }
    #endregion
}