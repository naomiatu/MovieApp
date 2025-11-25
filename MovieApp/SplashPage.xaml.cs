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
    }

    private void StartButton_Click(object sender, EventArgs e)
    {
        string name = NameEntry.Text?.Trim();

        if (!string.IsNullOrEmpty(name))
        {
           
            Preferences.Set("username", name);

        
            var window = Application.Current?.Windows[0];
            if (window != null)
            {
                window.Page = new AppShell();
            }
        }
        else
        {
            DisplayAlert("Input Required", "Please enter your name.", "OK");
        }
    }
}
