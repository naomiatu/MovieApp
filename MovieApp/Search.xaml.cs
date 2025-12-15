using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;

namespace MovieApp;

public partial class Search : ContentPage
{
    private List<Movie> _allMovies;
    private List<Movie> _filteredMovies;
    private HashSet<string> _selectedGenres = new HashSet<string>();
    private string _searchText = "";

    public Search()
    {
        InitializeComponent();
        LoadMovies();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadMovies();
    }

    private async Task LoadMovies()
    {
        try
        {
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;
            ResultsScrollView.IsVisible = false;
            NoResultsView.IsVisible = false;

            _allMovies = await MovieAutomation.GetAllMoviesAsync();
            _filteredMovies = new List<Movie>(_allMovies);

            SetupGenreFilters();
            DisplayMovies(_filteredMovies);

            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
            ResultsScrollView.IsVisible = true;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load movies: {ex.Message}", "OK");
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
        }
    }

    #region Genre Chips

    private void SetupGenreFilters()
    {
        if (_allMovies == null || _allMovies.Count == 0)
            return;

        var allGenres = new HashSet<string>();
        foreach (var movie in _allMovies)
        {
            if (movie.genre != null)
            {
                foreach (var genre in movie.genre)
                    allGenres.Add(genre);
            }
        }

        GenreChipsContainer.Children.Clear();

        // Add "All" chip
        var allChip = CreateGenreChip("All", "🎬", true);
        GenreChipsContainer.Children.Add(allChip);

        // Add genre chips
        foreach (var genre in allGenres.OrderBy(g => g))
        {
            var emoji = GetGenreEmoji(genre);
            var chip = CreateGenreChip(genre, emoji, false);
            GenreChipsContainer.Children.Add(chip);
        }
    }

    private Border CreateGenreChip(string genre, string emoji, bool isSelected)
    {
        var border = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 20 },
            Padding = new Thickness(15, 8),
            BackgroundColor = isSelected ? Color.FromArgb("#FFB800") : Color.FromArgb("#1a1a1a"),
            Stroke = isSelected ? Color.FromArgb("#FFB800") : Color.FromArgb("#333")
        };

        var stack = new HorizontalStackLayout { Spacing = 5 };
        stack.Children.Add(new Label
        {
            Text = emoji,
            FontSize = 16,
            VerticalOptions = LayoutOptions.Center
        });

        stack.Children.Add(new Label
        {
            Text = genre,
            FontSize = 14,
            TextColor = isSelected ? Colors.Black : Colors.White,
            FontAttributes = isSelected ? FontAttributes.Bold : FontAttributes.None,
            VerticalOptions = LayoutOptions.Center
        });

        border.Content = stack;

        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += (s, e) => GenreChip_Tapped(genre, border);
        border.GestureRecognizers.Add(tapGesture);

        return border;
    }

    private void GenreChip_Tapped(string genre, Border chipBorder)
    {
        if (genre == "All")
        {
            _selectedGenres.Clear();
            foreach (var child in GenreChipsContainer.Children.OfType<Border>())
            {
                var stack = child.Content as HorizontalStackLayout;
                var label = stack?.Children.OfType<Label>().LastOrDefault();

                if (label?.Text == "All")
                {
                    child.BackgroundColor = Color.FromArgb("#FFB800");
                    child.Stroke = Color.FromArgb("#FFB800");
                    label.TextColor = Colors.Black;
                    label.FontAttributes = FontAttributes.Bold;
                }
                else
                {
                    child.BackgroundColor = Color.FromArgb("#1a1a1a");
                    child.Stroke = Color.FromArgb("#333");
                    if (label != null)
                    {
                        label.TextColor = Colors.White;
                        label.FontAttributes = FontAttributes.None;
                    }
                }
            }
        }
        else
        {
            if (_selectedGenres.Contains(genre))
                _selectedGenres.Remove(genre);
            else
                _selectedGenres.Add(genre);

            var stack = chipBorder.Content as HorizontalStackLayout;
            var label = stack?.Children.OfType<Label>().LastOrDefault();

            if (_selectedGenres.Contains(genre))
            {
                chipBorder.BackgroundColor = Color.FromArgb("#FFB800");
                chipBorder.Stroke = Color.FromArgb("#FFB800");
                if (label != null)
                {
                    label.TextColor = Colors.Black;
                    label.FontAttributes = FontAttributes.Bold;
                }
            }
            else
            {
                chipBorder.BackgroundColor = Color.FromArgb("#1a1a1a");
                chipBorder.Stroke = Color.FromArgb("#333");
                if (label != null)
                {
                    label.TextColor = Colors.White;
                    label.FontAttributes = FontAttributes.None;
                }
            }
        }

        if (_selectedGenres.Count == 0)
        {
            foreach (var child in GenreChipsContainer.Children.OfType<Border>())
            {
                var s = child.Content as HorizontalStackLayout;
                var l = s?.Children.OfType<Label>().LastOrDefault();
                if (l?.Text == "All")
                {
                    child.BackgroundColor = Color.FromArgb("#FFB800");
                    child.Stroke = Color.FromArgb("#FFB800");
                    l.TextColor = Colors.Black;
                    l.FontAttributes = FontAttributes.Bold;
                    break;
                }
            }
        }

        ApplyFilters();
    }

    private string GetGenreEmoji(string genre) => genre.ToLower() switch
    {
        "action" => "💥",
        "adventure" => "🗺️",
        "comedy" => "😂",
        "drama" => "🎭",
        "horror" => "👻",
        "thriller" => "🔪",
        "romance" => "💕",
        "sci-fi" or "science fiction" => "🚀",
        "fantasy" => "🧙",
        "animation" => "🎨",
        "family" => "👨‍👩‍👧‍👦",
        "mystery" => "🔍",
        "crime" => "🚔",
        "documentary" => "📽️",
        "biography" => "📚",
        "history" => "⏳",
        "war" => "⚔️",
        "western" => "🤠",
        "musical" => "🎵",
        "sport" => "⚽",
        _ => "🎬"
    };

    #endregion

    #region Movie Cards

    private Border CreateMovieCard(Movie movie)
    {
        var border = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 15 },
            Stroke = Color.FromArgb("#333"),
            BackgroundColor = Color.FromArgb("#1a1a1a"),
            Margin = new Thickness(0),
            Shadow = new Shadow
            {
                Brush = Colors.Black,
                Offset = new Point(0, 4),
                Radius = 8,
                Opacity = 0.4f
            }
        };

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = 120 },
                new ColumnDefinition { Width = GridLength.Star }
            }
        };

        // Poster with overlay
        var posterBorder = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 15 },
            WidthRequest = 120,
            HeightRequest = 180
        };

        posterBorder.Content = new Image
        {
            Source = movie.ImageFileName,
            Aspect = Aspect.AspectFill
        };

        var posterGrid = new Grid { WidthRequest = 120, HeightRequest = 180 };
        posterGrid.Children.Add(posterBorder);

        var playIconBorder = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 20 },
            WidthRequest = 40,
            HeightRequest = 40,
            BackgroundColor = Color.FromArgb("#80000000"),
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Content = new Label
            {
                Text = "▶",
                FontSize = 20,
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            }
        };
        posterGrid.Children.Add(playIconBorder);

        grid.Add(posterGrid, 0, 0);

        // Info Stack
        var infoStack = new VerticalStackLayout
        {
            Padding = new Thickness(15, 10),
            Spacing = 8
        };

        infoStack.Children.Add(new Label
        {
            Text = movie.name,
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            MaxLines = 2
        });

        if (movie.year > 0)
        {
            infoStack.Children.Add(new Label
            {
                Text = $"📅 {movie.year}",
                FontSize = 13,
                TextColor = Color.FromArgb("#999")
            });
        }

        // Bottom info: rating and reviewed badge
        var bottomStack = new HorizontalStackLayout { Spacing = 10 };

        if (CheckIfReviewed(movie.name))
        {
            var reviewedBadge = new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = 10 },
                BackgroundColor = Color.FromArgb("#4CAF50"),
                Padding = new Thickness(8, 4),
                Content = new Label
                {
                    Text = "✓ Reviewed",
                    FontSize = 10,
                    TextColor = Colors.White,
                    FontAttributes = FontAttributes.Bold
                }
            };
            bottomStack.Children.Add(reviewedBadge);
        }

        if (movie.imDbRating > 0)
        {
            bottomStack.Children.Add(new Label
            {
                Text = $"⭐ {movie.imDbRating}/10",
                FontSize = 12,
                TextColor = Color.FromArgb("#FFB800")
            });
        }

        infoStack.Children.Add(bottomStack);

        grid.Add(infoStack, 1, 0);

        border.Content = grid;

        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += async (_, _) => await MovieCard_Tapped(movie);
        border.GestureRecognizers.Add(tapGesture);

        return border;
    }

    private bool CheckIfReviewed(string movieName)
    {
        try
        {
            var json = SecureStorage.GetAsync($"review_{movieName}").Result;
            if (!string.IsNullOrEmpty(json))
            {
                var review = JsonSerializer.Deserialize<MovieReview>(json);
                return review != null;
            }
        }
        catch { }

        return false;
    }

    private async Task MovieCard_Tapped(Movie movie)
    {
        await Navigation.PushAsync(new MovieDetailsPage(movie));
    }

    #endregion

    #region Search

    private void SearchEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchText = e.NewTextValue ?? "";
        ClearButton.IsVisible = !string.IsNullOrEmpty(_searchText);
        ApplyFilters();
    }

    private void ClearSearch_Clicked(object sender, EventArgs e)
    {
        SearchEntry.Text = "";
        _searchText = "";
        ClearButton.IsVisible = false;
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        if (_allMovies == null || _allMovies.Count == 0)
            return;

        _filteredMovies = _allMovies.Where(movie =>
        {
            bool matchesSearch = string.IsNullOrEmpty(_searchText) ||
                                 movie.name.Contains(_searchText, StringComparison.OrdinalIgnoreCase);

            bool matchesGenre = _selectedGenres.Count == 0 ||
                                (movie.genre != null && movie.genre.Any(g => _selectedGenres.Contains(g)));

            return matchesSearch && matchesGenre;
        }).ToList();

        DisplayMovies(_filteredMovies);
    }

    private void DisplayMovies(List<Movie> movies)
    {
        MoviesContainer.Children.Clear();

        if (movies == null || movies.Count == 0)
        {
            ResultsScrollView.IsVisible = false;
            NoResultsView.IsVisible = true;
            return;
        }

        ResultsScrollView.IsVisible = true;
        NoResultsView.IsVisible = false;

        foreach (var movie in movies)
        {
            MoviesContainer.Children.Add(CreateMovieCard(movie));
        }
    }

    #endregion
}
