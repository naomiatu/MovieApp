using System;
using Microsoft.Maui.Controls;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Maui.Graphics;


namespace MovieApp;

public partial class MovieDetailsPage : ContentPage
{
    
        private Movie _currentMovie;

        public MovieDetailsPage()
        {
            InitializeComponent();
        }

        // This accepts a movie file name
        public MovieDetailsPage(string movieFileName) : this()
        {
            LoadMovieData(movieFileName);
        }

        // Overload to accept Movie object directly
        public MovieDetailsPage(Movie movie) : this()
        {
            _currentMovie = movie;
            UpdateUI();
        }

        private async void LoadMovieData(string movieName)
        {
            try
            {
                _currentMovie = await MovieAutomation.GetMovieByNameAsync(movieName);

                if (_currentMovie == null)
                {
                    await DisplayAlert("Error", "Movie not found", "OK");
                    await Navigation.PopAsync();
                    return;
                }

                UpdateUI();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load movie data: {ex.Message}", "OK");
            }
        }

        private void UpdateUI()
        {
            if (_currentMovie == null) return;

            // Update movie poster
            MoviePoster.Source = _currentMovie.ImageFileName;

            // Update movie title
            MovieTitle.Text = _currentMovie.name;

            // Update genres
            MovieGenres.Text = _currentMovie.FormattedGenres;

            // Update description
            MovieDescription.Text = _currentMovie.storyline;

            // Update rating display (you can calculate this from user ratings later)
            UpdateRatingDisplay(4.0); // Default rating

            // Update cast if available
            if (_currentMovie.actors != null && _currentMovie.actors.Count > 0)
            {
                CastContainer.Children.Clear();
                foreach (var actor in _currentMovie.actors.Take(5))
                {
                    var castMember = CreateCastMember(actor);
                    CastContainer.Children.Add(castMember);
                }
            }
        }

        private void UpdateRatingDisplay(double rating)
        {
            MovieRating.Text = rating.ToString("0.0");

            // Update star colors based on rating
            var fullStars = (int)rating;
            var hasHalfStar = (rating % 1) >= 0.5;

            var stars = new[] { StarIcon1, StarIcon2, StarIcon3, StarIcon4, StarIcon5 };

            for (int i = 0; i < stars.Length; i++)
            {
                if (i < fullStars)
                {
                    stars[i].TextColor = Color.FromArgb("#FFB800");
                }
                else if (i == fullStars && hasHalfStar)
                {
                    stars[i].TextColor = Color.FromArgb("#FFB800");
                }
                else
                {
                    stars[i].TextColor = Color.FromArgb("#666");
                }
            }
        }

        private VerticalStackLayout CreateCastMember(string actorName)
        {
            var container = new VerticalStackLayout
            {
                Spacing = 8,
                WidthRequest = 100
            };

            var frame = new Frame
            {
                CornerRadius = 50,
                Padding = 0,
                WidthRequest = 100,
                HeightRequest = 100,
                HorizontalOptions = LayoutOptions.Center,
                HasShadow = false,
                BorderColor = Color.FromArgb("#333"),
                BackgroundColor = Color.FromArgb("#222")
            };

            // Try to load actor image, fallback to placeholder
            var actorImageName = actorName.ToLower().Replace(" ", "_").Replace(".", "") + ".jpg";
            var image = new Image
            {
                Source = actorImageName,
                Aspect = Aspect.AspectFill
            };

            frame.Content = image;
            container.Children.Add(frame);

            var label = new Label
            {
                Text = actorName,
                FontSize = 12,
                TextColor = Colors.White,
                HorizontalTextAlignment = TextAlignment.Center,
                LineBreakMode = LineBreakMode.WordWrap
            };

            container.Children.Add(label);

            return container;
        }


        private async void Back_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void PlayMovie_Clicked(object sender, EventArgs e)
    {
        await DisplayAlert("Play Movie", "Starting movie playback...", "OK");
    }

    private async void WatchNow_Clicked(object sender, EventArgs e)
    {
        await DisplayAlert("Watch Now", "Opening movie player...", "OK");
    }

    private async void RateMovie_Clicked(object sender, EventArgs e)
    {
        string result = await DisplayActionSheet(
            "Rate this movie",
            "Cancel",
            null,
            "⭐ 1 Star",
            "⭐⭐ 2 Stars",
            "⭐⭐⭐ 3 Stars",
            "⭐⭐⭐⭐ 4 Stars",
            "⭐⭐⭐⭐⭐ 5 Stars");

        if (result != "Cancel" && result != null)
        {
            await DisplayAlert("Thank You!", $"You rated this movie: {result}", "OK");
        }
    }
}