using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using MovieApp;

namespace MovieProject;

public partial class SplashPage : ContentPage
{
    public SplashPage()
    {
        InitializeComponent();

        // Check if a name is already saved
        if (Preferences.ContainsKey("username"))
        {
            string savedName = Preferences.Get("username", "");
            OpenMainPage(savedName);
        }
    }

    private void StartButton_Click(object sender, EventArgs e)
    {
        string name = NameEntry.Text?.Trim();

        if (!string.IsNullOrEmpty(name))
        {
            // Save the name using Preferences
            Preferences.Set("username", name);

            OpenMainPage(name);
        }
        else
        {
            DisplayAlert("Input Required", "Please enter your name.", "OK");
        }
    }

    private async void OpenMainPage(string name)
    {
        await Navigation.PushAsync(new MainPage(name));
        // Optionally remove SplashPage from the navigation stack
        Navigation.RemovePage(this);
    }
}
