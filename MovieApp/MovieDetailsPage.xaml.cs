using System;
using Microsoft.Maui.Controls;

namespace MovieApp;

public partial class MovieDetailsPage : ContentPage
{
    public MovieDetailsPage()
    {
        InitializeComponent();
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