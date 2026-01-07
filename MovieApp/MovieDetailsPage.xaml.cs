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

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            try
            {
                if (MoviePoster?.Source is UriImageSource)
                {
                    MoviePoster.Source = null;
                }
                if (SimilarMoviesList != null)
                {
                    SimilarMoviesList.Children.Clear();
                }
                _allSimilarMovies?.Clear();
                _allSimilarMovies = null;
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
                        if (posterUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                        {
                            MoviePoster.Source = new UriImageSource
                            {
                                Uri = new Uri(posterUrl),
                                CachingEnabled = true,
                                CacheValidity = TimeSpan.FromDays(7)
                            };
                        }
                        else { MoviePoster.Source = posterUrl; }
                    }
                    else { MoviePoster.Source = "placeholder_movie.png"; }
                }
                else { MoviePoster.Source = "placeholder_movie.png"; }
            }
            catch
            {
                MoviePoster.Source = "placeholder_movie.png";
            }

            MovieTitle.Text = _currentMovie.title ?? "Unknown Movie";
            MovieGenres.Text = (_currentMovie.genre != null && _currentMovie.genre.Count > 0)
                ? string.Join(" • ", _currentMovie.genre)
                : "Genre not available";
            MovieDescription.Text = _currentMovie.storyline ?? "No description available.";
        }

        private void UpdateReviewUI()
        {
            if (_currentReview == null) return;
            UpdateStarDisplay(_currentReview.Rating);
            UpdateEmojiDisplay(_currentReview.SelectedEmojis);
            WatchedBadge.IsVisible = _currentReview.IsWatched;
            WatchedButton.Text = _currentReview.IsWatched ? "✓ Watched" : "Mark as Watched";
            WatchedButton.BackgroundColor = _currentReview.IsWatched ? _themeManager.BlueAccent : _themeManager.AccentColor;
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
                SimilarMoviesList.Children.Clear();
                var allMovies = await MovieDataService.Instance.GetMoviesAsync();
                var filteredMovies = FilterSimilarMoviesByGenre(allMovies);
                if (filteredMovies != null && filteredMovies.Count > 0)
                {
                    PopulateSimilarMoviesUI(filteredMovies);
                    SimilarMoviesSection.IsVisible = true;
                }
                else { SimilarMoviesSection.IsVisible = false; }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading similar movies: {ex.Message}");
                SimilarMoviesSection.IsVisible = false;
            }
        }

        private List<SimilarMovie> FilterSimilarMoviesByGenre(List<Movie> allMovies)
        {
            if (_currentMovie?.genre == null || allMovies == null)
                return new List<SimilarMovie>();

          
            return allMovies
                .Where(m => m.title != _currentMovie.title &&
                            m.genre != null &&
                            m.genre.Any(g => _currentMovie.genre.Contains(g)))
                .OrderByDescending(m => m.genre.Count(g => _currentMovie.genre.Contains(g))) 
                .ThenByDescending(m => m.rating)
                .Take(5)
                .Select(m => new SimilarMovie
                {
                    Title = m.title,
                    Poster = m.poster,
                    Genre = m.genre,
                    Storyline = m.storyline,
                    Year = m.year,
                    Rating = m.rating,
                    Director = m.director ?? "Unknown"
                })
                .ToList();
        }
        private void PopulateSimilarMoviesUI(List<SimilarMovie> movies)
        {
            SimilarMoviesList.Children.Clear();
            foreach (var movie in movies)
            {
                SimilarMoviesList.Children.Add(CreateSimilarMovieCard(movie));
            }
        }

        private Border CreateSimilarMovieCard(SimilarMovie movie)
        {
            var border = new Border
            {
                WidthRequest = 140,
                HeightRequest = 240,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                BackgroundColor = _themeManager.CardBackgroundColor
            };

            var grid = new Grid { RowDefinitions = { new RowDefinition { Height = 180 }, new RowDefinition { Height = GridLength.Star } } };
            var posterImage = new Image { Aspect = Aspect.AspectFill, WidthRequest = 140, HeightRequest = 180 };

            if (!string.IsNullOrWhiteSpace(movie.Poster))
            {
                string posterUrl = TMDBImageHelper.GetSmartPosterUrl(movie.Poster, TMDBImageHelper.PosterSize.Small);
                if (!string.IsNullOrEmpty(posterUrl))
                {
                    posterImage.Source = new UriImageSource { Uri = new Uri(posterUrl), CachingEnabled = true, CacheValidity = TimeSpan.FromDays(7) };
                }
            }
            else { posterImage.Source = "placeholder_movie.png"; }

            grid.Add(new Border { StrokeShape = new RoundRectangle { CornerRadius = 12 }, Content = posterImage }, 0, 0);

            var infoStack = new VerticalStackLayout { Padding = 8, Spacing = 2 };
            infoStack.Children.Add(new Label { Text = movie.Title, FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = _themeManager.TextColor, MaxLines = 1 });
            var yearRating = new HorizontalStackLayout { Spacing = 5 };
            yearRating.Children.Add(new Label { Text = movie.Year > 0 ? movie.Year.ToString() : "N/A", FontSize = 10, TextColor = _themeManager.SubtextColor });
            yearRating.Children.Add(new Label { Text = $"⭐ {movie.Rating:F1}", FontSize = 10, TextColor = _themeManager.AccentColor });
            infoStack.Children.Add(yearRating);
            grid.Add(infoStack, 0, 1);

            border.Content = grid;
            var tap = new TapGestureRecognizer();
            tap.Tapped += async (s, e) => await OnSimilarMovieTapped(movie);
            border.GestureRecognizers.Add(tap);
            return border;
        }

        private async Task OnSimilarMovieTapped(SimilarMovie sm)
        {
            try
            {
                var movie = new Movie { title = sm.Title, poster = sm.Poster, genre = sm.Genre, storyline = sm.Storyline, year = sm.Year, director = sm.Director, rating = sm.Rating };
                await Navigation.PushAsync(new MovieDetailsPage(movie));
            }
            catch { await DisplayAlert("Error", "Unable to load movie details", "OK"); }
        }
        #endregion

        #region Maintenance
        private async void ClearCache_Tapped(object sender, EventArgs e)
        {
            if (await DisplayAlert("Clear Cache", "Reset all data?", "Yes", "No"))
            {
                Preferences.Clear();
                SecureStorage.RemoveAll();
                _themeManager.ResetToDefault();
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
                var cacheDir =System.IO.Path.Combine(FileSystem.CacheDirectory, "ImageCache");
                if (Directory.Exists(cacheDir)) Directory.Delete(cacheDir, true);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}"); }
        }
    }
    #endregion
}