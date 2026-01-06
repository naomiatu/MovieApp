using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly ThemeManager _themeManager;

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

            // Get theme manager and bind
            _themeManager = ThemeManager.Instance;
            BindingContext = _themeManager;

            // Listen for theme changes to refresh genre chips
            _themeManager.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_themeManager.IsDarkTheme))
                {
                    // Refresh genre chips with new theme
                    if (_allMovies.Count > 0)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            SetupGenreFilters();
                            DisplayMovies(_filteredMovies);
                        });
                    }
                }
            };
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
                await DisplayAlert("Error", $"Failed to load movies: {ex.Message}", "OK");
            }
            finally
            {
                ShowLoading(false);
            }
        }

        private void ShowLoading(bool show)
        {
            LoadingContainer.IsVisible = show;
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
            }, TaskScheduler.Default);
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

            var textLabel = new Label
            {
                Text = genre,
                TextColor = selected ? Colors.Black : _themeManager.TextColor,
                FontAttributes = selected ? FontAttributes.Bold : FontAttributes.None,
                VerticalOptions = LayoutOptions.Center,
                FontSize = 14
            };

            stackLayout.Children.Add(textLabel);

            var border = new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = 20 },
                Padding = new Thickness(15, 8),
                BackgroundColor = selected ? _themeManager.AccentColor : _themeManager.CardBackgroundColor,
                Stroke = selected ? _themeManager.AccentColor : _themeManager.BorderColor,
                StrokeThickness = 1,
                Content = stackLayout
            };

            border.Shadow = new Shadow
            {
                Brush = selected ? _themeManager.AccentColor : Colors.Transparent,
                Radius = 12,
                Opacity = 0.6f
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

            border.GestureRecognizers.Add(tap);

            // Subscribe to theme changes
            _themeManager.PropertyChanged += (s, e) =>
            {
                if (!selected)
                {
                    border.BackgroundColor = _themeManager.CardBackgroundColor;
                    border.Stroke = _themeManager.BorderColor;
                    textLabel.TextColor = _themeManager.TextColor;
                }
            };

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
                    ? _themeManager.AccentColor
                    : _themeManager.CardBackgroundColor;

                chip.Stroke = isSelected ? _themeManager.AccentColor : _themeManager.BorderColor;

                chip.Shadow = new Shadow
                {
                    Brush = isSelected ? _themeManager.AccentColor : Colors.Transparent,
                    Radius = 12,
                    Opacity = 0.6f
                };

                // Update text color
                var textLabel = content.Children.OfType<Label>().LastOrDefault();
                if (textLabel != null)
                {
                    textLabel.TextColor = isSelected ? Colors.Black : _themeManager.TextColor;
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
                WidthRequest = 100,
                HeightRequest = 150,
                VerticalOptions = LayoutOptions.Center
            };

            // Bind poster border colors
            posterBorder.SetBinding(Border.StrokeProperty, new Binding(nameof(_themeManager.BorderColor), source: _themeManager));
            posterBorder.SetBinding(Border.BackgroundColorProperty, new Binding(nameof(_themeManager.CardBackgroundColor), source: _themeManager));

            var posterImage = new Image
            {
                Aspect = Aspect.AspectFill,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            };

            // Use TMDBImageHelper to get proper poster URL
            if (!string.IsNullOrEmpty(movie.poster))
            {
                string posterUrl = TMDBImageHelper.GetSmartPosterUrl(movie.poster, TMDBImageHelper.PosterSize.Small);

                if (!string.IsNullOrEmpty(posterUrl))
                {
                    if (posterUrl.StartsWith("http://") || posterUrl.StartsWith("https://"))
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
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                LineBreakMode = LineBreakMode.WordWrap,
                MaxLines = 2
            };
            titleLabel.SetBinding(Label.TextColorProperty, new Binding(nameof(_themeManager.TextColor), source: _themeManager));

            var infoStack = new HorizontalStackLayout
            {
                Spacing = 10
            };

            if (movie.year > 0)
            {
                var yearLabel = new Label
                {
                    Text = movie.year.ToString(),
                    FontSize = 14
                };
                yearLabel.SetBinding(Label.TextColorProperty, new Binding(nameof(_themeManager.SubtextColor), source: _themeManager));
                infoStack.Children.Add(yearLabel);
            }

            if (movie.rating > 0)
            {
                var ratingLabel = new Label
                {
                    Text = $"⭐ {movie.rating:F1}",
                    FontSize = 14
                };
                ratingLabel.SetBinding(Label.TextColorProperty, new Binding(nameof(_themeManager.AccentColor), source: _themeManager));
                infoStack.Children.Add(ratingLabel);
            }

            var genreLabel = new Label
            {
                Text = movie.genre != null && movie.genre.Count > 0
                    ? string.Join(" • ", movie.genre)
                    : "Unknown",
                FontSize = 13,
                LineBreakMode = LineBreakMode.TailTruncation,
                MaxLines = 1
            };
            genreLabel.SetBinding(Label.TextColorProperty, new Binding(nameof(_themeManager.SubtextColor), source: _themeManager));

            detailsStack.Children.Add(titleLabel);
            detailsStack.Children.Add(infoStack);
            detailsStack.Children.Add(genreLabel);

            Grid.SetColumn(detailsStack, 1);
            grid.Children.Add(detailsStack);

            // Main Border
            var border = new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = 15 },
                Content = grid,
                Margin = new Thickness(5),
                Shadow = new Shadow
                {
                    Brush = Colors.Black,
                    Radius = 8,
                    Opacity = 0.3f
                }
            };

            // Bind border colors
            border.SetBinding(Border.StrokeProperty, new Binding(nameof(_themeManager.BorderColor), source: _themeManager));
            border.SetBinding(Border.BackgroundColorProperty, new Binding(nameof(_themeManager.CardBackgroundColor), source: _themeManager));

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
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to open movie details: {ex.Message}", "OK");
            }
            finally
            {
                _isNavigating = false;
            }
        }
    }
}