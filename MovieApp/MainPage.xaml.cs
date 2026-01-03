using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;

namespace MovieApp;

public partial class MainPage : ContentPage
{
    private List<Movie> _allMovies;
    private List<string> _watchedMovies;
    private int _reviewCount;

    public MainPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Update welcome message
        string userName = Preferences.Default.Get("username", "Guest");
        WelcomeLabel.Text = $"Hello, {userName}!";

        // Load all data
        await LoadDashboardData();
    }

    private async Task LoadDashboardData()
    {
        try
        {
            LoadingOverlay.IsVisible = true;

            // Load movies
            _allMovies = await MovieAutomation.GetAllMoviesAsync();

            // Load watched movies
            await LoadWatchedMovies();

            // Count reviews
            await CountReviews();

            // Update stats
            UpdateStats();

            // Load top rated movies
            await LoadTopRatedMovies();

            // Load recent reviews
            await LoadRecentReviews();

            LoadingOverlay.IsVisible = false;
        }
        catch (Exception ex)
        {
            LoadingOverlay.IsVisible = false;
            System.Diagnostics.Debug.WriteLine($"Error loading dashboard: {ex.Message}");
            // Continue even if there's an error - show empty dashboard
            UpdateStats();
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
                : JsonSerializer.Deserialize<List<string>>(watchedJson);
        }
        catch
        {
            _watchedMovies = new List<string>();
        }
    }

    private async Task CountReviews()
    {
        _reviewCount = 0;

        if (_allMovies == null) return;

        foreach (var movie in _allMovies)
        {
            try
            {
                var json = await SecureStorage.GetAsync($"review_{movie.title}");
                if (!string.IsNullOrEmpty(json))
                {
                    _reviewCount++;
                }
            }
            catch { }
        }
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

        // Average rating of collection
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
            Stroke = Color.FromArgb("#333"),
            BackgroundColor = Color.FromArgb("#1a1a1a")
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

        // Load poster image - handle both URLs and local files
        if (!string.IsNullOrEmpty(movie.poster))
        {
            if (movie.poster.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                movie.poster.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                posterImage.Source = new UriImageSource
                {
                    Uri = new Uri(movie.poster),
                    CachingEnabled = true,
                    CacheValidity = TimeSpan.FromDays(1)
                };
            }
            else
            {
                posterImage.Source = movie.poster;
            }
        }
        else
        {
            posterImage.Source = "placeholder_movie.png";
        }

        posterBorder.Content = posterImage;

        // Rating badge overlay
        var ratingBadge = new Border
        {
            BackgroundColor = Color.FromArgb("#FFB800"),
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

        // Movie info
        var infoStack = new VerticalStackLayout
        {
            Padding = new Thickness(8, 0),
            Spacing = 4
        };

        infoStack.Children.Add(new Label
        {
            Text = movie.title,
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            MaxLines = 2,
            LineBreakMode = LineBreakMode.TailTruncation
        });

        infoStack.Children.Add(new Label
        {
            Text = movie.year.ToString(),
            FontSize = 10,
            TextColor = Color.FromArgb("#999")
        });

        grid.Add(infoStack, 0, 1);

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

        if (_allMovies == null)
        {
            NoReviewsLabel.IsVisible = true;
            return;
        }

        var reviewedMovies = new List<(Movie movie, MovieReview review)>();

        // Load all reviews
        foreach (var movie in _allMovies)
        {
            try
            {
                var json = await SecureStorage.GetAsync($"review_{movie.title}");
                if (!string.IsNullOrEmpty(json))
                {
                    var review = JsonSerializer.Deserialize<MovieReview>(json);
                    if (review != null && review.Rating > 0)
                    {
                        reviewedMovies.Add((movie, review));
                    }
                }
            }
            catch { }
        }

        if (reviewedMovies.Count == 0)
        {
            NoReviewsLabel.IsVisible = true;
            return;
        }

        NoReviewsLabel.IsVisible = false;

        // Sort by date (most recent first)
        var sortedReviews = reviewedMovies
            .OrderByDescending(r => r.review.DateReviewed ?? DateTime.MinValue)
            .Take(5)
            .ToList();

        foreach (var (movie, review) in sortedReviews)
        {
            RecentReviewsContainer.Children.Add(CreateReviewCard(movie, review));
        }
    }

    private Border CreateReviewCard(Movie movie, MovieReview review)
    {
        var border = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Stroke = Color.FromArgb("#333"),
            BackgroundColor = Color.FromArgb("#1a1a1a"),
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

        // Load poster image - handle both URLs and local files
        if (!string.IsNullOrEmpty(movie.poster))
        {
            if (movie.poster.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                movie.poster.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                posterImage.Source = new UriImageSource
                {
                    Uri = new Uri(movie.poster),
                    CachingEnabled = true,
                    CacheValidity = TimeSpan.FromDays(1)
                };
            }
            else
            {
                posterImage.Source = movie.poster;
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
            TextColor = Colors.White,
            MaxLines = 1,
            LineBreakMode = LineBreakMode.TailTruncation
        });

        // Star rating display
        var starsLabel = new Label
        {
            FontSize = 16,
            TextColor = Color.FromArgb("#FFB800")
        };
        var stars = "";
        for (int i = 0; i < review.Rating; i++)
            stars += "★";
        for (int i = review.Rating; i < 5; i++)
            stars += "☆";
        starsLabel.Text = stars;
        infoStack.Children.Add(starsLabel);

        // Emojis if any
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
                TextColor = Color.FromArgb("#666")
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
            // Navigate to Search page (assuming it's in a tab)
            if (Application.Current?.MainPage is Shell shell)
            {
                await shell.GoToAsync("//Search");
            }
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
            // Navigate to Search page
            if (Application.Current?.MainPage is Shell shell)
            {
                await shell.GoToAsync("//Search");
            }
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

            // Pick a random movie
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

    #endregion
}

