using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using MovieProject;

namespace MovieApp
{
    public partial class Search : ContentPage
    {
        private List<Movie> _allMovies = new();
        private List<Movie> _filteredMovies = new();
        private HashSet<string> _selectedGenres = new();
        private bool _isNavigating;
        private string _searchText = "";

        private CancellationTokenSource _searchDebounceToken;

        // Genre to Emoji mapping
        private static readonly Dictionary<string, string> GenreEmojis = new()
        {
            { "Action", "💥" },
            { "Adventure", "🗺️" },
            { "Animation", "🎨" },
            { "Comedy", "😂" },
            { "Crime", "🔫" },
            { "Documentary", "📽️" },
            { "Drama", "🎭" },
            { "Family", "👨‍👩‍👧‍👦" },
            { "Fantasy", "🧙" },
            { "Horror", "👻" },
            { "Mystery", "🔍" },
            { "Romance", "❤️" },
            { "Sci-Fi", "🚀" },
            { "Thriller", "😱" },
            { "Western", "🤠" },
            { "War", "⚔️" },
            { "Musical", "🎵" },
            { "Biography", "📖" },
            { "History", "🏛️" },
            { "Sport", "⚽" }
        };

        public Search()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (_allMovies.Count == 0)
                await LoadMoviesAsync();
        }

        private async Task LoadMoviesAsync()
        {
            try
            {
                ShowLoading(true);

                _allMovies = await MovieAutomation.GetAllMoviesAsync();
                _filteredMovies = new List<Movie>(_allMovies);

                SetupGenreFilters();
                DisplayMovies(_filteredMovies);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                ShowLoading(false);
            }
        }

        private void ShowLoading(bool show)
        {
            LoadingIndicator.IsVisible = show;
            LoadingIndicator.IsRunning = show;
            ResultsScrollView.IsVisible = !show;
            NoResultsView.IsVisible = false;
        }

        // =====================
        // 🔍 SEARCH (DEBOUNCED)
        // =====================
        private void SearchEntry_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchText = e.NewTextValue ?? "";
            ClearButton.IsVisible = !string.IsNullOrEmpty(_searchText);

            _searchDebounceToken?.Cancel();
            _searchDebounceToken = new CancellationTokenSource();

            var token = _searchDebounceToken.Token;

            Task.Delay(350, token).ContinueWith(t =>
            {
                if (!t.IsCanceled)
                    MainThread.BeginInvokeOnMainThread(ApplyFilters);
            });
        }

        private void ClearSearch_Clicked(object sender, EventArgs e)
        {
            SearchEntry.Text = "";
            _searchText = "";
            ClearButton.IsVisible = false;
            ApplyFilters();
        }

        // =====================
        // 🎭 GENRE FILTERS WITH EMOJIS
        // =====================
        private void SetupGenreFilters()
        {
            GenreChipsContainer.Children.Clear();
            _selectedGenres.Clear();

            GenreChipsContainer.Children.Add(CreateGenreChip("All", "🎬", true));

            var genres = _allMovies
                .Where(m => m.genre != null)
                .SelectMany(m => m.genre)
                .Distinct()
                .OrderBy(g => g);

            foreach (var genre in genres)
            {
                var emoji = GenreEmojis.ContainsKey(genre) ? GenreEmojis[genre] : "🎥";
                GenreChipsContainer.Children.Add(CreateGenreChip(genre, emoji, false));
            }
        }

        private Border CreateGenreChip(string genre, string emoji, bool selected)
        {
            var stackLayout = new HorizontalStackLayout
            {
                Spacing = 6,
                VerticalOptions = LayoutOptions.Center
            };

            stackLayout.Children.Add(new Label
            {
                Text = emoji,
                FontSize = 16,
                VerticalOptions = LayoutOptions.Center
            });

            stackLayout.Children.Add(new Label
            {
                Text = genre,
                TextColor = selected ? Colors.Black : Colors.White,
                FontAttributes = selected ? FontAttributes.Bold : FontAttributes.None,
                VerticalOptions = LayoutOptions.Center,
                FontSize = 14
            });

            var border = new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = 20 },
                Padding = new Thickness(15, 8),
                BackgroundColor = selected ? Color.FromArgb("#FFB800") : Color.FromArgb("#1a1a1a"),
                Stroke = selected ? Color.FromArgb("#FFB800") : Color.FromArgb("#333"),
                Content = stackLayout
            };

            var tap = new TapGestureRecognizer();
            tap.Tapped += async (_, _) =>
            {
                await border.ScaleTo(1.15, 90, Easing.CubicOut);
                await border.ScaleTo(1.0, 90, Easing.CubicIn);
                await border.ScaleTo(1.1, 80);
                await border.ScaleTo(1.0, 80);
                ToggleGenre(genre, border, stackLayout);
            };
            border.Shadow = new Shadow
            {
                Brush = selected ? Color.FromArgb("#FFB800") : Colors.Transparent,
                Radius = 12,
                Opacity = 0.6f
            };

            border.GestureRecognizers.Add(tap);
            return border;
        }

        private void ToggleGenre(string genre, Border chip, HorizontalStackLayout content)
        {
            if (genre == "All")
            {
                _selectedGenres.Clear();
                SetupGenreFilters();
            }
            else
            {
                if (!_selectedGenres.Add(genre))
                    _selectedGenres.Remove(genre);

                bool isSelected = _selectedGenres.Contains(genre);
                chip.BackgroundColor = isSelected
                    ? Color.FromArgb("#FFB800")
                    : Color.FromArgb("#1a1a1a");

                chip.Shadow = new Shadow
                {
                    Brush = isSelected ? Color.FromArgb("#FFB800") : Colors.Transparent,
                    Radius = 12,
                    Opacity = 0.6f
                };

                // Update text color
                var textLabel = content.Children.OfType<Label>().LastOrDefault();
                if (textLabel != null)
                {
                    textLabel.TextColor = isSelected ? Colors.Black : Colors.White;
                    textLabel.FontAttributes = isSelected ? FontAttributes.Bold : FontAttributes.None;
                }
            }

            ApplyFilters();
        }

        // =====================
        // 🎬 FILTERING
        // =====================
        private void ApplyFilters()
        {
            _filteredMovies = _allMovies.Where(movie =>
            {
                bool matchesSearch =
                    string.IsNullOrWhiteSpace(_searchText) ||
                    movie.title.Contains(_searchText, StringComparison.OrdinalIgnoreCase);

                bool matchesGenre =
                    _selectedGenres.Count == 0 ||
                    (movie.genre?.Any(g => _selectedGenres.Contains(g)) ?? false);

                return matchesSearch && matchesGenre;
            }).ToList();

            DisplayMovies(_filteredMovies);
        }

        private Border CreateMovieCard(Movie movie)
        {
            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = new GridLength(120) }, // Image width
                    new ColumnDefinition { Width = GridLength.Star }      // Title width
                },
                Padding = 10
            };

            // Movie Poster Image
            var posterBorder = new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = 10 },
                Stroke = Color.FromArgb("#333"),
                StrokeThickness = 1,
                WidthRequest = 100,
                HeightRequest = 150,
                BackgroundColor = Color.FromArgb("#222"),
                VerticalOptions = LayoutOptions.Center
            };

            var posterImage = new Image
            {
                Aspect = Aspect.AspectFill,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            };

            // Check if poster is a URL or local file
            if (!string.IsNullOrEmpty(movie.poster))
            {
                if (movie.poster.StartsWith("http://") || movie.poster.StartsWith("https://"))
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
                // Fallback placeholder
                posterImage.Source = "placeholder_movie.png";
            }

            posterBorder.Content = posterImage;
            Grid.SetColumn(posterBorder, 0);
            grid.Children.Add(posterBorder);

            // Movie Details (Title, Year, Rating)
            var detailsStack = new VerticalStackLayout
            {
                Spacing = 5,
                Padding = new Thickness(15, 0, 0, 0),
                VerticalOptions = LayoutOptions.Center
            };

            var titleLabel = new Label
            {
                Text = movie.title,
                TextColor = Colors.White,
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                LineBreakMode = LineBreakMode.WordWrap,
                MaxLines = 2
            };

            var infoStack = new HorizontalStackLayout
            {
                Spacing = 10
            };

            if (movie.year > 0)
            {
                infoStack.Children.Add(new Label
                {
                    Text = movie.year.ToString(),
                    TextColor = Color.FromArgb("#999"),
                    FontSize = 14
                });
            }

            if (movie.rating > 0)
            {
                infoStack.Children.Add(new Label
                {
                    Text = $"⭐ {movie.rating:F1}",
                    TextColor = Color.FromArgb("#FFB800"),
                    FontSize = 14
                });
            }

            var genreLabel = new Label
            {
                Text = movie.genre != null && movie.genre.Count > 0
                    ? string.Join(" • ", movie.genre)
                    : "Unknown",
                TextColor = Color.FromArgb("#999"),
                FontSize = 13,
                LineBreakMode = LineBreakMode.TailTruncation,
                MaxLines = 1
            };

            detailsStack.Children.Add(titleLabel);
            detailsStack.Children.Add(infoStack);
            detailsStack.Children.Add(genreLabel);

            Grid.SetColumn(detailsStack, 1);
            grid.Children.Add(detailsStack);

            // Main Border
            var border = new Border
            {
                Stroke = Color.FromArgb("#333"),
                StrokeShape = new RoundRectangle { CornerRadius = 15 },
                BackgroundColor = Color.FromArgb("#1a1a1a"),
                Content = grid,
                Margin = new Thickness(5),
                Shadow = new Shadow
                {
                    Brush = Colors.Black,
                    Radius = 8,
                    Opacity = 0.3f
                }
            };

            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += async (_, _) => await MovieCard_Tapped(movie);
            border.GestureRecognizers.Add(tapGesture);

            return border;
        }

        private async void DisplayMovies(List<Movie> movies)
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
                var card = CreateMovieCard(movie);

                // Initial animation state
                card.Opacity = 0;
                card.TranslationY = 20;

                MoviesContainer.Children.Add(card);

                // Staggered animation
                await Task.Delay(40);

                _ = card.FadeTo(1, 250, Easing.CubicOut);
                _ = card.TranslateTo(0, 0, 250, Easing.CubicOut);
            }
        }

        private async Task MovieCard_Tapped(Movie movie)
        {
            if (_isNavigating)
                return;

            try
            {
                _isNavigating = true;
                await Navigation.PushAsync(new MovieDetailsPage(movie));
            }
            finally
            {
                _isNavigating = false;
            }
        }
    }
}