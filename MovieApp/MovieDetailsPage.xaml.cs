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
        private static List<SimilarMovie> _cachedSimilarMovies;
        private static DateTime _similarMoviesCacheTime;
        private const int CACHE_MINUTES = 30;

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
        }

        private void InitializeButtons()
        {
            _starButtons = new[] { Star1, Star2, Star3, Star4, Star5 };
            _emojiButtons = new[] { Emoji1, Emoji2, Emoji3, Emoji4, Emoji5, Emoji6 };
        }

        // Cleanup resources when page disappears
        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            try
            {
                // Clear image sources to free memory
                if (MoviePoster?.Source is UriImageSource)
                {
                    MoviePoster.Source = null;
                }

                // Clear similar movies list
                if (SimilarMoviesList != null)
                {
                    SimilarMoviesList.Children.Clear();
                }

                // Clear references (but don't clear static cache)
                _allSimilarMovies?.Clear();
                _allSimilarMovies = null;

                System.Diagnostics.Debug.WriteLine($"🧹 Cleaned up MovieDetailsPage for: {_currentMovie?.title}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during cleanup: {ex.Message}");
            }
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
            MainPage.UpdateCacheReview(_currentMovie.title, _currentReview);

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

            try
            {
                if (!string.IsNullOrWhiteSpace(_currentMovie.poster))
                {
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
                    MoviePoster.Source = "placeholder_movie.png";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading poster: {ex.Message}");
                MoviePoster.Source = "placeholder_movie.png";
            }

            MovieTitle.Text = _currentMovie.title ?? "Unknown Movie";

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
                : _themeManager.AccentColor;
        }

        private void UpdateStarDisplay(int rating)
        {
            for (int i = 0; i < _starButtons.Length; i++)
            {
                _starButtons[i].Text = i < rating ? "★" : "☆";
                _starButtons[i].TextColor = i < rating ? _themeManager.AccentColor : Colors.Gray;
            }
            RatingText.Text = rating > 0 ? $"{rating}/5" : "";
        }

        private void UpdateEmojiDisplay(List<string> selectedEmojis)
        {
            if (selectedEmojis == null) return;

            foreach (var button in _emojiButtons)
            {
                button.BackgroundColor = selectedEmojis.Contains(button.Text)
                    ? _themeManager.AccentColor
                    : _themeManager.IconBackgroundColor;

                button.TextColor = Colors.White;
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
        }

    
        private async void Emoji_Clicked(object sender, EventArgs e)
        {
            if (sender is not Button clickedEmoji) return;

            _currentReview ??= new MovieReview { MovieName = _currentMovie?.title };
            string emoji = clickedEmoji.Text;

            if (_currentReview.SelectedEmojis.Contains(emoji))
            {
                _currentReview.SelectedEmojis.Remove(emoji);
                clickedEmoji.BackgroundColor = _themeManager.IconBackgroundColor;
            }
            else
            {
                _currentReview.SelectedEmojis.Add(emoji);
                clickedEmoji.BackgroundColor = _themeManager.AccentColor;
            }

            // Always ensure the emoji itself is visible
            clickedEmoji.TextColor = Colors.White;

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
        }

        private async void ShareReview_Clicked(object sender, EventArgs e)
        {
            if (_currentReview == null || _currentReview.Rating == 0) return;

            string shareText = $"I rated '{_currentMovie.title}' {_currentReview.Rating} stars! 🌟";
            await Share.RequestAsync(new ShareTextRequest
            {
                Text = shareText,
                Title = "Share Movie Review"
            });
        }

        private async void Back_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
        #endregion


        #region Similar Movies
        private async Task LoadSimilarMovies()
        {
            try
            {
                // Use cached data if available and not expired
                if (_cachedSimilarMovies != null && (DateTime.Now - _similarMoviesCacheTime).TotalMinutes < CACHE_MINUTES)
                {
                    _allSimilarMovies = _cachedSimilarMovies;
                }
                else
                {
                    var assembly = GetType().Assembly;
                    var resourceName = assembly.GetManifestResourceNames().FirstOrDefault(r => r.EndsWith("similar_movies.json"));

                    if (resourceName == null)
                    {
                        System.Diagnostics.Debug.WriteLine("⚠️ similar_movies.json not found in embedded resources");
                        return;
                    }

                    using (var stream = assembly.GetManifestResourceStream(resourceName))
                    using (var reader = new StreamReader(stream))
                    {
                        string json = await reader.ReadToEndAsync();
                        _allSimilarMovies = JsonSerializer.Deserialize<List<SimilarMovie>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        _cachedSimilarMovies = _allSimilarMovies;
                        _similarMoviesCacheTime = DateTime.Now;
                    }
                }

                var filteredMovies = FilterSimilarMoviesByGenre();

                if (filteredMovies.Count > 0)
                {
                    PopulateSimilarMoviesUI(filteredMovies);
                    SimilarMoviesSection.IsVisible = true;
                }
                else
                {
                    SimilarMoviesSection.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading similar movies: {ex.Message}");
                SimilarMoviesSection.IsVisible = false;
            }
        }

        private List<SimilarMovie> FilterSimilarMoviesByGenre()
        {
            if (_currentMovie?.genre == null || _allSimilarMovies == null) return new List<SimilarMovie>();

            // Get only 3 similar movies
            return _allSimilarMovies
                .Where(sm => sm.Genre != null && sm.Genre.Any(g => _currentMovie.genre.Contains(g)) && sm.Title != _currentMovie.title)
                .Take(3)
                .ToList();
        }

        private void PopulateSimilarMoviesUI(List<SimilarMovie> movies)
        {
            SimilarMoviesList.Children.Clear();

            foreach (var movie in movies)
            {
                var movieCard = CreateSimilarMovieCard(movie);
                SimilarMoviesList.Children.Add(movieCard);
            }
        }

        private Border CreateSimilarMovieCard(SimilarMovie movie)
        {
            // Create card matching MainPage style
            var border = new Border
            {
                WidthRequest = 140,
                HeightRequest = 240,
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                Stroke = _themeManager.BorderColor,
                BackgroundColor = _themeManager.CardBackgroundColor
            };

            var grid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = 180 },
                    new RowDefinition { Height = GridLength.Star }
                }
            };

            // Poster
            var posterBorder = new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                Margin = new Thickness(0, 0, 0, 8)
            };

            var posterImage = new Image
            {
                Aspect = Aspect.AspectFill,
                WidthRequest = 140,
                HeightRequest = 180
            };

            try
            {
                if (!string.IsNullOrWhiteSpace(movie.Poster))
                {
                    string posterUrl = TMDBImageHelper.GetThumbnailUrl(movie.Poster);

                    if (!string.IsNullOrEmpty(posterUrl))
                    {
                        if (posterUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                            posterUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                        {
                            posterImage.Source = new UriImageSource
                            {
                                Uri = new Uri(posterUrl),
                                CachingEnabled = true,
                                CacheValidity = TimeSpan.FromDays(7)
                            };
                        }
                        else
                        {
                            posterImage.Source = posterUrl;
                        }
                    }
                    else
                    {
                        posterImage.Source = "placeholder_movie.png";
                    }
                }
                else
                {
                    posterImage.Source = "placeholder_movie.png";
                }
            }
            catch
            {
                posterImage.Source = "placeholder_movie.png";
            }

            posterBorder.Content = posterImage;
            grid.Add(posterBorder, 0, 0);

            // Movie info
            var infoStack = new VerticalStackLayout
            {
                Padding = new Thickness(8, 0, 8, 8),
                Spacing = 4
            };

            // Title
            var titleLabel = new Label
            {
                Text = movie.Title,
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                TextColor = _themeManager.TextColor,
                MaxLines = 2,
                LineBreakMode = LineBreakMode.TailTruncation
            };
            infoStack.Children.Add(titleLabel);

            // Year & Rating
            var yearRatingStack = new HorizontalStackLayout
            {
                Spacing = 5
            };

            var yearLabel = new Label
            {
                Text = movie.Year.ToString(),
                FontSize = 11,
                TextColor = _themeManager.SubtextColor
            };
            yearRatingStack.Children.Add(yearLabel);

            var ratingLabel = new Label
            {
                Text = $"⭐ {movie.Rating:F1}",
                FontSize = 11,
                TextColor = _themeManager.AccentColor
            };
            yearRatingStack.Children.Add(ratingLabel);

            infoStack.Children.Add(yearRatingStack);

            grid.Add(infoStack, 0, 1);

            border.Content = grid;

            // Add tap gesture
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += async (s, e) => await OnSimilarMovieTapped(movie);
            border.GestureRecognizers.Add(tapGesture);

            return border;
        }

        private async Task OnSimilarMovieTapped(SimilarMovie similarMovie)
        {
            try
            {
                var movie = MovieHelper.CreateFromSimilarMovie(
                    similarMovie.Title,
                    similarMovie.Poster,
                    similarMovie.Genre,
                    similarMovie.Storyline,
                    similarMovie.Year,
                    similarMovie.Director,
                    similarMovie.Rating
                );

                await Navigation.PushAsync(new MovieDetailsPage(movie));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error navigating to similar movie: {ex.Message}");
                await DisplayAlert("Error", "Unable to load movie details", "OK");
            }
        }
        #endregion

        #region Clear Cache & Sign Out
        private async void ClearCache_Tapped(object sender, EventArgs e)
        {
            if (await DisplayAlert("Clear Cache", "Reset all data?", "Yes", "No"))
            {
                Preferences.Clear();
                SecureStorage.RemoveAll();
                _themeManager.ResetToDefault();

                // Static call works if class is in the same namespace below
                ImageCacheManager.ClearAllCache();

                _cachedSimilarMovies = null;
                Application.Current.MainPage = new NavigationPage(new SplashPage());
            }
        }

        private async void SignOut_Tapped(object sender, EventArgs e)
        {
            if (await DisplayAlert("Sign Out", "Are you sure?", "Yes", "No"))
            {
                Preferences.Remove("username");
                Application.Current.MainPage = new NavigationPage(new SplashPage());
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
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("year")] public int Year { get; set; }
        [JsonPropertyName("genre")] public List<string> Genre { get; set; }
        [JsonPropertyName("director")] public string Director { get; set; }
        [JsonPropertyName("rating")] public double Rating { get; set; }
        [JsonPropertyName("poster")] public string Poster { get; set; }
        [JsonPropertyName("storyline")] public string Storyline { get; set; }
    }

    public static class ImageCacheManager
    {
        public static void ClearAllCache()
        {
            try
            {
                var cacheDir = System.IO.Path.Combine(FileSystem.CacheDirectory, "ImageCache");
                if (Directory.Exists(cacheDir))
                {
                    Directory.Delete(cacheDir, true);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
            }
        }
    }
    #endregion
}