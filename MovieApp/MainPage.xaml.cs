using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;

namespace MovieApp
{
    public partial class MainPage : ContentPage
    {
        private List<Movie> _allMovies;
        private List<string> _watchedMovies;
        private int _reviewCount;
        private readonly ThemeManager _themeManager;

         //Cache review data to avoid expensive SecureStorage reads every time
        private static Dictionary<string, MovieReview> _cachedReviews = new Dictionary<string, MovieReview>();
        private static DateTime _lastReviewCacheUpdate = DateTime.MinValue;
        private const int REVIEW_CACHE_MINUTES = 5;

        public MainPage()
        {
            InitializeComponent();

            // Get theme manager and bind
            _themeManager = ThemeManager.Instance;
            BindingContext = _themeManager;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Update welcome message
            string userName = Preferences.Default.Get("username", "Guest");
            WelcomeLabel.Text = $"Hello, {userName}!";

            System.Diagnostics.Debug.WriteLine($"💾 MainPage appearing - Memory: {GC.GetTotalMemory(false) / 1024 / 1024} MB");

            // Load all data
            await LoadDashboardData();
        }

        private async Task LoadDashboardData()
        {
            try
            {
                LoadingOverlay.IsVisible = true;

                //  Use centralized data service 
                var dataService = MovieDataService.Instance;
                _allMovies = await dataService.GetMoviesAsync();

                System.Diagnostics.Debug.WriteLine($"✅ MainPage using {_allMovies?.Count ?? 0} cached movies");

                // Load watched movies
                await LoadWatchedMovies();

                // Count reviews with caching
                await CountReviewsOptimized();

                // Update stats
                UpdateStats();

                // Load top rated movies
                await LoadTopRatedMovies();

                // Load recent reviews (uses cached reviews)
                await LoadRecentReviews();

                System.Diagnostics.Debug.WriteLine($"💾 MainPage loaded - Memory: {GC.GetTotalMemory(false) / 1024 / 1024} MB");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading dashboard: {ex.Message}");
                UpdateStats();
            }
            finally
            {
                LoadingOverlay.IsVisible = false;
            }
        }

        #region Data Loading

        private async Task LoadWatchedMovies()
        {
            try
            {
                var watchedJson = await SecureStorage.GetAsync("watched_movies");
                _watchedMovies = string.IsNullOrEmpty(watchedJson)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(watchedJson) ?? new List<string>();
            }
            catch
            {
                _watchedMovies = new List<string>();
            }
        }

        private async Task CountReviewsOptimized()
        {
            // Check if cache is still valid
            if ((DateTime.Now - _lastReviewCacheUpdate).TotalMinutes < REVIEW_CACHE_MINUTES)
            {
                _reviewCount = _cachedReviews.Count;
                System.Diagnostics.Debug.WriteLine($"✅ Using cached review count: {_reviewCount}");
                return;
            }

            // Need to refresh cache
            _reviewCount = 0;
            _cachedReviews.Clear();

            if (_allMovies == null) return;

            System.Diagnostics.Debug.WriteLine($"🔄 Refreshing review cache for {_allMovies.Count} movies...");

            // Only check movies that are likely to have reviews (watched movies)
            var moviesToCheck = _watchedMovies != null && _watchedMovies.Count > 0
                ? _allMovies.Where(m => _watchedMovies.Contains(m.title)).ToList()
                : _allMovies.Take(50).ToList();

            foreach (var movie in moviesToCheck)
            {
                try
                {
                    var json = await SecureStorage.GetAsync($"review_{movie.title}");
                    if (!string.IsNullOrEmpty(json))
                    {
                        var review = JsonSerializer.Deserialize<MovieReview>(json);
                        if (review != null && review.Rating > 0)
                        {
                            _cachedReviews[movie.title] = review;
                            _reviewCount++;
                        }
                    }
                }
                catch { }
            }

            _lastReviewCacheUpdate = DateTime.Now;
            System.Diagnostics.Debug.WriteLine($"✅ Review cache updated: {_reviewCount} reviews found");
        }

        public static void InvalidateReviewCache()
        {
            _lastReviewCacheUpdate = DateTime.MinValue;
            System.Diagnostics.Debug.WriteLine("🔄 Review cache invalidated");
        }

        #endregion

        #region Stats Update

        private void UpdateStats()
        {
            // Total movies
            TotalMoviesLabel.Text = _allMovies?.Count.ToString() ?? "0";

            // Watched count
            WatchedCountLabel.Text = _watchedMovies?.Count.ToString() ?? "0";

            // Reviews count
            ReviewsCountLabel.Text = _reviewCount.ToString();

            // Average rating
            if (_allMovies != null && _allMovies.Count > 0)
            {
                var avgRating = _allMovies.Average(m => m.rating);
                AverageRatingLabel.Text = avgRating.ToString("F1");
            }
            else
            {
                AverageRatingLabel.Text = "0.0";
            }
        }

        #endregion

        #region Top Rated Movies

        private async Task LoadTopRatedMovies()
        {
            TopRatedContainer.Children.Clear();

            if (_allMovies == null || _allMovies.Count == 0)
                return;

            // Get top 10 rated movies
            var topMovies = _allMovies
                .OrderByDescending(m => m.rating)
                .Take(10)
                .ToList();

            foreach (var movie in topMovies)
            {
                TopRatedContainer.Children.Add(CreateTopRatedCard(movie));
            }
        }

        private Border CreateTopRatedCard(Movie movie)
        {
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

            // MEMORY FIX: Use Thumbnail size for list items
            if (!string.IsNullOrEmpty(movie.poster))
            {
                string posterUrl = TMDBImageHelper.GetThumbnailUrl(movie.poster);

                if (!string.IsNullOrEmpty(posterUrl))
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
                    posterImage.Source = "placeholder_movie.png";
                }
            }
            else
            {
                posterImage.Source = "placeholder_movie.png";
            }

            posterBorder.Content = posterImage;

            // Rating badge
            var ratingBadge = new Border
            {
                BackgroundColor = _themeManager.AccentColor,
                Padding = new Thickness(8, 4),
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(0, 8, 8, 0),
                StrokeShape = new RoundRectangle { CornerRadius = 8 },
                Content = new Label
                {
                    Text = movie.rating.ToString("F1"),
                    TextColor = Colors.Black,
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold
                }
            };

            var posterContainer = new Grid();
            posterContainer.Children.Add(posterBorder);
            posterContainer.Children.Add(ratingBadge);

            grid.Add(posterContainer, 0, 0);

            // Title
            var titleStack = new VerticalStackLayout
            {
                Spacing = 4,
                Padding = new Thickness(8, 0),
                VerticalOptions = LayoutOptions.Center
            };

            titleStack.Children.Add(new Label
            {
                Text = movie.title,
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                TextColor = _themeManager.TextColor,
                MaxLines = 2,
                LineBreakMode = LineBreakMode.TailTruncation,
                HorizontalTextAlignment = TextAlignment.Center
            });

            grid.Add(titleStack, 0, 1);
            border.Content = grid;

            // Tap gesture
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += async (s, e) => await OpenMovieDetails(movie);
            border.GestureRecognizers.Add(tapGesture);

            return border;
        }

        #endregion

        #region Recent Reviews

        private async Task LoadRecentReviews()
        {
            RecentReviewsContainer.Children.Clear();

            if (_allMovies == null || _allMovies.Count == 0)
            {
                NoReviewsLabel.IsVisible = true;
                return;
            }

            if (_cachedReviews.Count == 0)
            {
                NoReviewsLabel.IsVisible = true;
                return;
            }

            NoReviewsLabel.IsVisible = false;

            var reviewedMovies = _cachedReviews
                .Select(kvp =>
                {
                    var movie = _allMovies.FirstOrDefault(m => m.title == kvp.Key);
                    return movie != null ? (movie, kvp.Value) : default;
                })
                .Where(x => x != default)
                .OrderByDescending(x => x.Value.DateReviewed ?? DateTime.MinValue)
                .Take(5)
                .ToList();

            foreach (var (movie, review) in reviewedMovies)
            {
                RecentReviewsContainer.Children.Add(CreateReviewCard(movie, review));
            }
        }

        private Border CreateReviewCard(Movie movie, MovieReview review)
        {
            var border = new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                Stroke = _themeManager.BorderColor,
                BackgroundColor = _themeManager.CardBackgroundColor,
                Padding = 12
            };

            var grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = 60 },
                    new ColumnDefinition { Width = GridLength.Star }
                },
                ColumnSpacing = 12
            };

            // Small poster
            var posterBorder = new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = 8 },
                WidthRequest = 60,
                HeightRequest = 90
            };

            var posterImage = new Image
            {
                Aspect = Aspect.AspectFill
            };

            // MEMORY FIX: Use Thumbnail size
            if (!string.IsNullOrEmpty(movie.poster))
            {
                string posterUrl = TMDBImageHelper.GetThumbnailUrl(movie.poster);

                if (!string.IsNullOrEmpty(posterUrl))
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
                    posterImage.Source = "placeholder_movie.png";
                }
            }
            else
            {
                posterImage.Source = "placeholder_movie.png";
            }

            posterBorder.Content = posterImage;
            grid.Add(posterBorder, 0, 0);

            // Review info
            var infoStack = new VerticalStackLayout
            {
                Spacing = 6,
                VerticalOptions = LayoutOptions.Center
            };

            infoStack.Children.Add(new Label
            {
                Text = movie.title,
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = _themeManager.TextColor,
                MaxLines = 1,
                LineBreakMode = LineBreakMode.TailTruncation
            });

            // Stars
            var starsLabel = new Label
            {
                FontSize = 16,
                TextColor = _themeManager.AccentColor
            };
            var stars = "";
            for (int i = 0; i < review.Rating; i++)
                stars += "★";
            for (int i = review.Rating; i < 5; i++)
                stars += "☆";
            starsLabel.Text = stars;
            infoStack.Children.Add(starsLabel);

            // Emojis
            if (review.SelectedEmojis != null && review.SelectedEmojis.Count > 0)
            {
                infoStack.Children.Add(new Label
                {
                    Text = string.Join(" ", review.SelectedEmojis.Take(3)),
                    FontSize = 14
                });
            }

            // Date
            if (review.DateReviewed.HasValue)
            {
                infoStack.Children.Add(new Label
                {
                    Text = review.DateReviewed.Value.ToString("MMM dd, yyyy"),
                    FontSize = 11,
                    TextColor = _themeManager.SubtextColor
                });
            }

            grid.Add(infoStack, 1, 0);
            border.Content = grid;

            // Tap gesture
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += async (s, e) => await OpenMovieDetails(movie);
            border.GestureRecognizers.Add(tapGesture);

            return border;
        }

        #endregion

        #region Navigation & Actions

        private async Task OpenMovieDetails(Movie movie)
        {
            try
            {
                await Navigation.PushAsync(new MovieDetailsPage(movie));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
            }
        }

        private async void SeeAllTopRated_Clicked(object sender, EventArgs e)
        {
            try
            {
                await Shell.Current.GoToAsync("//SearchPage");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
            }
        }


        private async void BrowseMovies_Clicked(object sender, EventArgs e)
        {
            try
            {
                await Shell.Current.GoToAsync("//SearchPage");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
            }
        }


        private async void RandomMovie_Clicked(object sender, EventArgs e)
        {
            try
            {
                if (_allMovies == null || _allMovies.Count == 0)
                {
                    await DisplayAlert("Oops", "No movies available!", "OK");
                    return;
                }

                var random = new Random();
                var randomMovie = _allMovies[random.Next(_allMovies.Count)];

                await Navigation.PushAsync(new MovieDetailsPage(randomMovie));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
                await DisplayAlert("Error", "Could not open random movie", "OK");
            }
        }

        public static void UpdateCacheReview(string movieTitle, MovieReview review)
        {
            _cachedReviews[movieTitle] = review;
            _lastReviewCacheUpdate = DateTime.Now;
            System.Diagnostics.Debug.WriteLine($"Review cache updated for: {movieTitle}");
        }

        #endregion

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            // Clear container children to free memory
            TopRatedContainer?.Children.Clear();
            RecentReviewsContainer?.Children.Clear();

            System.Diagnostics.Debug.WriteLine("🧹 MainPage cleaned up");
        }
    }
}