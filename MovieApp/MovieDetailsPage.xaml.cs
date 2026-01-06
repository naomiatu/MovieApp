using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
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
        private readonly ThemeManager _themeManager;

        public MovieDetailsPage()
        {
            InitializeComponent();
            InitializeButtons();

            // Get theme manager and bind
            _themeManager = ThemeManager.Instance;
            BindingContext = _themeManager;
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
                    : JsonSerializer.Deserialize<List<string>>(watchedJson) ?? new List<string>();

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

            // Handle poster image using TMDBImageHelper
            try
            {
                if (!string.IsNullOrWhiteSpace(_currentMovie.poster))
                {
                    // Use TMDBImageHelper to get proper poster URL (Large size for details page)
                    string posterUrl = TMDBImageHelper.GetSmartPosterUrl(_currentMovie.poster, TMDBImageHelper.PosterSize.Large);

                    if (!string.IsNullOrEmpty(posterUrl))
                    {
                        if (posterUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                            posterUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                        {
                            MoviePoster.Source = new UriImageSource
                            {
                                Uri = new Uri(posterUrl),
                                CachingEnabled = true,
                                CacheValidity = TimeSpan.FromDays(7)
                            };
                        }
                        else
                        {
                            MoviePoster.Source = posterUrl;
                        }
                    }
                    else
                    {
                        MoviePoster.Source = "placeholder_movie.png";
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
                ? _themeManager.BlueAccent
                : _themeManager.GreenAccent;
        }

        private void UpdateStarDisplay(int rating)
        {
            for (int i = 0; i < _starButtons.Length; i++)
            {
                _starButtons[i].Text = i < rating ? "★" : "☆";
                _starButtons[i].TextColor = i < rating ? _themeManager.AccentColor : Color.FromArgb("#666");
            }
            RatingText.Text = rating > 0 ? $"{rating} star{(rating != 1 ? "s" : "")}" : "Not rated";
        }

        private void UpdateEmojiDisplay(List<string> selectedEmojis)
        {
            if (selectedEmojis == null) return;

            foreach (var button in _emojiButtons)
            {
                button.BackgroundColor = selectedEmojis.Contains(button.Text)
                    ? _themeManager.AccentColor
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
                clickedEmoji.BackgroundColor = _themeManager.AccentColor;
            }

            await clickedEmoji.ScaleTo(1.2, 80, Easing.CubicOut);
            await clickedEmoji.ScaleTo(1.0, 80, Easing.CubicIn);
            await SaveReviewAsync();
        }

        private async void ToggleWatched_Clicked(object sender, EventArgs e)
        {
            _currentReview ??= new MovieReview { MovieName = _currentMovie?.title };
            _currentReview.IsWatched = !_currentReview.IsWatched;
            _currentReview.DateWatched = _currentReview.IsWatched ? DateTime.Now : null;

            UpdateReviewUI();
            await SaveReviewAsync();

            string message = _currentReview.IsWatched
                ? "Marked as watched! 🎬"
                : "Removed from watched list.";
            await DisplayAlert("Success", message, "OK");
        }

        private async void ShareReview_Clicked(object sender, EventArgs e)
        {
            if (_currentReview == null || _currentReview.Rating == 0)
            {
                await DisplayAlert("No Review", "Please rate this movie first!", "OK");
                return;
            }

            string shareText = $"I rated '{_currentMovie.title}' {_currentReview.Rating} stars! 🌟";
            if (_currentReview.SelectedEmojis.Count > 0)
            {
                shareText += $"\n{string.Join(" ", _currentReview.SelectedEmojis)}";
            }

            await Share.RequestAsync(new ShareTextRequest
            {
                Text = shareText,
                Title = "Share Movie Review"
            });
        }

        private async void PlayMovie_Clicked(object sender, EventArgs e)
        {
            await DisplayAlert("Play Movie", "Trailer playback feature coming soon!", "OK");
        }

        private async void Back_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
        #endregion

        #region Cast Members
        private async Task LoadCastMembers()
        {
            try
            {
                // Mock cast data - replace with actual API call
                var castMembers = new List<CastMember>
                {
                    new CastMember { Name = "Actor 1", Character = "Role 1", ImageUrl = "" },
                    new CastMember { Name = "Actor 2", Character = "Role 2", ImageUrl = "" },
                    new CastMember { Name = "Actor 3", Character = "Role 3", ImageUrl = "" }
                };

                CastContainer.Children.Clear();
                foreach (var cast in castMembers)
                {
                    CastContainer.Children.Add(CreateCastMemberView(cast));
                }

                CastSection.IsVisible = castMembers.Count > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading cast: {ex.Message}");
                CastSection.IsVisible = false;
            }
        }

        private View CreateCastMemberView(CastMember cast)
        {
            var container = new VerticalStackLayout
            {
                Spacing = 8,
                WidthRequest = 100
            };

            // Cast member image
            var imageBorder = new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = 50 },
                WidthRequest = 80,
                HeightRequest = 80,
                BackgroundColor = Color.FromArgb("#1a1a1a"),
                HorizontalOptions = LayoutOptions.Center
            };

            var image = new Image
            {
                Aspect = Aspect.AspectFill,
                WidthRequest = 80,
                HeightRequest = 80
            };

            if (!string.IsNullOrEmpty(cast.ImageUrl))
            {
                string profileUrl = TMDBImageHelper.GetProfileUrl(cast.ImageUrl);
                if (!string.IsNullOrEmpty(profileUrl))
                {
                    image.Source = new UriImageSource
                    {
                        Uri = new Uri(profileUrl),
                        CachingEnabled = true,
                        CacheValidity = TimeSpan.FromDays(7)
                    };
                }
                else
                {
                    image.Source = "placeholder_person.png";
                }
            }
            else
            {
                image.Source = "placeholder_person.png";
            }

            imageBorder.Content = image;
            container.Children.Add(imageBorder);

            // Name
            container.Children.Add(new Label
            {
                Text = cast.Name,
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center,
                MaxLines = 1,
                LineBreakMode = LineBreakMode.TailTruncation
            });

            // Character
            container.Children.Add(new Label
            {
                Text = cast.Character,
                FontSize = 10,
                TextColor = Color.FromArgb("#999"),
                HorizontalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center,
                MaxLines = 1,
                LineBreakMode = LineBreakMode.TailTruncation
            });

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

                    // Convert poster paths to full URLs using TMDBImageHelper
                    foreach (var movie in _allSimilarMovies)
                    {
                        if (!string.IsNullOrEmpty(movie.Poster))
                        {
                            movie.Poster = TMDBImageHelper.GetSmartPosterUrl(movie.Poster, TMDBImageHelper.PosterSize.Small);
                        }
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

                // Reset theme to default
                _themeManager.ResetToDefault();

                await DisplayAlert("Success", "Cache cleared successfully.", "OK");

                Application.Current?.CloseWindow(Application.Current.Windows[0]);
                Application.Current?.OpenWindow(new Window(new NavigationPage(new SplashPage())));
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
                SecureStorage.RemoveAll();

                await DisplayAlert("Signed Out", "You have been signed out.", "OK");

                Application.Current?.CloseWindow(Application.Current.Windows[0]);
                Application.Current?.OpenWindow(new Window(new NavigationPage(new SplashPage())));
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