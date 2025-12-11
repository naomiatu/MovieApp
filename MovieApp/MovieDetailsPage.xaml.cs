using System;
using Microsoft.Maui.Controls;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Maui.Graphics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
namespace MovieApp;

public partial class MovieDetailsPage : ContentPage
{
    private Movie _currentMovie;
    private MovieReview _currentReview;
    private Button[] _starButtons;
    private Button[] _emojiButtons;

    public MovieDetailsPage()
    {
        InitializeComponent();
        InitializeButtons();
    }

    private void InitializeButtons()
    {
        _starButtons = new[] { Star1, Star2, Star3, Star4, Star5 };
        _emojiButtons = new[] { Emoji1, Emoji2, Emoji3, Emoji4, Emoji5, Emoji6 };
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
        LoadReview();
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
            LoadReview();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load movie data: {ex.Message}", "OK");
        }
    }

    private async void LoadReview()
    {
        if (_currentMovie == null) return;

        try
        {
            var json = await SecureStorage.GetAsync($"review_{_currentMovie.name}");
            if (!string.IsNullOrEmpty(json))
            {
                _currentReview = JsonSerializer.Deserialize<MovieReview>(json);
                UpdateReviewUI();
            }
            else
            {
                _currentReview = new MovieReview { MovieName = _currentMovie.name };
            }
        }
        catch (Exception)
        {
            _currentReview = new MovieReview { MovieName = _currentMovie.name };
        }
    }

    private async 
    Task
SaveReview()
    {
        if (_currentReview == null || _currentMovie == null) return;

        try
        {
            var json = JsonSerializer.Serialize(_currentReview);
            await SecureStorage.SetAsync($"review_{_currentMovie.name}", json);

            // Also save to watched list if rated or marked as watched
            if (_currentReview.IsWatched || _currentReview.Rating > 0)
            {
                await AddToWatchedList();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to save review: {ex.Message}", "OK");
        }
    }

    private async Task AddToWatchedList()
    {
        try
        {
            var watchedJson = await SecureStorage.GetAsync("watched_movies");
            var watchedList = string.IsNullOrEmpty(watchedJson)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(watchedJson);

            if (!watchedList.Contains(_currentMovie.name))
            {
                watchedList.Add(_currentMovie.name);
                var json = JsonSerializer.Serialize(watchedList);
                await SecureStorage.SetAsync("watched_movies", json);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error adding to watched list: {ex.Message}");
        }
    }

    private void UpdateReviewUI()
    {
        if (_currentReview == null) return;

        // Update star rating
        UpdateStarDisplay(_currentReview.Rating);

        // Update emoji reactions
        UpdateEmojiDisplay(_currentReview.SelectedEmojis);

        // Update watched status
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
            if (i < rating)
            {
                _starButtons[i].Text = "★";
                _starButtons[i].TextColor = Color.FromArgb("#FFB800");
            }
            else
            {
                _starButtons[i].Text = "☆";
                _starButtons[i].TextColor = Color.FromArgb("#666");
            }
        }

        RatingText.Text = rating > 0
            ? $"{rating} star{(rating != 1 ? "s" : "")}"
            : "Not rated";
    }

    private void UpdateEmojiDisplay(List<string> selectedEmojis)
    {
        if (selectedEmojis == null) return;

        foreach (var button in _emojiButtons)
        {
            if (selectedEmojis.Contains(button.Text))
            {
                button.BackgroundColor = Color.FromArgb("#FFB800");
            }
            else
            {
                button.BackgroundColor = Color.FromArgb("#2a2a2a");
            }
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

    private async void Star_Clicked(object sender, EventArgs e)
    {
        if (sender is not Button clickedStar) return;

        var rating = Array.IndexOf(_starButtons, clickedStar) + 1;

        _currentReview.Rating = rating;
        _currentReview.DateReviewed = DateTime.Now;

        UpdateStarDisplay(rating);
        await SaveReview();

        await DisplayAlert("Rating Saved", $"You rated this movie {rating} star{(rating != 1 ? "s" : "")}!", "OK");
    }

    private async void Emoji_Clicked(object sender, EventArgs e)
    {
        if (sender is not Button clickedEmoji) return;

        var emoji = clickedEmoji.Text;

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

        await SaveReview();
    }

    private async void ToggleWatched_Clicked(object sender, EventArgs e)
    {
        _currentReview.IsWatched = !_currentReview.IsWatched;

        if (_currentReview.IsWatched)
        {
            _currentReview.DateWatched = DateTime.Now;
        }

        UpdateReviewUI();
        await SaveReview();

        var message = _currentReview.IsWatched
            ? "Added to your watched list!"
            : "Removed from watched list";
        await DisplayAlert("Success", message, "OK");
    }

    private async void ShareReview_Clicked(object sender, EventArgs e)
    {
        if (_currentReview.Rating == 0 && _currentReview.SelectedEmojis.Count == 0)
        {
            await DisplayAlert("No Review", "Please rate the movie or add reactions before sharing!", "OK");
            return;
        }

        var reviewText = $"🎬 {_currentMovie.name}\n\n";

        if (_currentReview.Rating > 0)
        {
            reviewText += $"⭐ Rating: {_currentReview.Rating}/5 stars\n";
        }

        if (_currentReview.SelectedEmojis.Count > 0)
        {
            reviewText += $"Reactions: {string.Join(" ", _currentReview.SelectedEmojis)}\n";
        }

        if (_currentReview.IsWatched)
        {
            reviewText += $"\n✓ Watched on {_currentReview.DateWatched:MMM dd, yyyy}";
        }

        await Share.RequestAsync(new ShareTextRequest
        {
            Text = reviewText,
            Title = $"My review of {_currentMovie.name}"
        });
    }

    private async void PlayMovie_Clicked(object sender, EventArgs e)
    {
        string trailerUrl = "https://www.youtube.com/watch?v=Ke1Y3P9D0Bc";
        TrailerWebView.Source = new UrlWebViewSource { Url = trailerUrl };
        TrailerWebView.IsVisible = true;
        PlayButton.IsVisible = false;
    }

    private async void WatchNow_Clicked(object sender, EventArgs e)
    {
        await DisplayAlert("Watch Now", "Starting movie playback...", "OK");
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
            // Extract the star rating number
            int rating = result.Count(c => c == '⭐');

            _currentReview.Rating = rating;
            _currentReview.DateReviewed = DateTime.Now;

            UpdateStarDisplay(rating);
            await SaveReview();

            await DisplayAlert("Thank You!", $"You rated this movie: {result}", "OK");
        }
    }

    private async void Back_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

}

// Movie Review Model
public class MovieReview
{
    public string MovieName { get; set; }
    public int Rating { get; set; }
    public List<string> SelectedEmojis { get; set; } = new List<string>();
    public bool IsWatched { get; set; }
    public DateTime? DateWatched { get; set; }
    public DateTime? DateReviewed { get; set; }
}