using Microsoft.Maui.Controls;
using MovieApp;
using System;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MovieProject;

public partial class SplashPage : ContentPage
{
    public SplashPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await AnimateIntro();
    }

    private async Task AnimateIntro()
    {
        // Start with everything invisible/scaled down
        AppTitle.Opacity = 0;
        AppTitle.Scale = 0.5;
        AppName.Opacity = 0;
        AppName.TranslationY = 20;
        TagLine.Opacity = 0;
        InputStack.Opacity = 0;
        InputStack.TranslationY = 30;
        FloatingEmojis.Opacity = 0;

        // Animate logo icon with bounce
        await Task.Delay(300);
        var scaleTask = AppTitle.ScaleTo(1.2, 400, Easing.CubicOut);
        var fadeTask = AppTitle.FadeTo(1, 400);
        await Task.WhenAll(scaleTask, fadeTask);
        await AppTitle.ScaleTo(1.0, 200, Easing.CubicIn);

        // Animate app name sliding in
        await Task.Delay(200);
        var nameSlide = AppName.TranslateTo(0, 0, 500, Easing.CubicOut);
        var nameFade = AppName.FadeTo(1, 500);
        await Task.WhenAll(nameSlide, nameFade);

        // Animate tagline
        await Task.Delay(100);
        await TagLine.FadeTo(1, 400);

        // Animate input section
        await Task.Delay(200);
        var inputSlide = InputStack.TranslateTo(0, 0, 600, Easing.CubicOut);
        var inputFade = InputStack.FadeTo(1, 600);
        await Task.WhenAll(inputSlide, inputFade);

        // Animate floating emojis
        await Task.Delay(100);
        await FloatingEmojis.FadeTo(1, 400);

        // Start floating animation for emojis
        _ = AnimateFloatingEmojis();
    }

    private async Task AnimateFloatingEmojis()
    {
        var emojis = new[] { Emoji1, Emoji2, Emoji3, Emoji4, Emoji5 };
        var random = new Random();

        while (true)
        {
            foreach (var emoji in emojis)
            {
                var delay = random.Next(0, 500);
                _ = Task.Run(async () =>
                {
                    await Task.Delay(delay);
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await emoji.TranslateTo(0, -10, 1000, Easing.SinInOut);
                        await emoji.TranslateTo(0, 0, 1000, Easing.SinInOut);
                    });
                });
            }

            await Task.Delay(2000);
        }
    }

    private async void StartButton_Click(object sender, EventArgs e)
    {
        string name = NameEntry.Text?.Trim();

        if (!string.IsNullOrEmpty(name))
        {
            // Save username using modern Preferences API
            Preferences.Default.Set("username", name);

            // Animate button press
            await StartButton.ScaleTo(0.95, 50);
            await StartButton.ScaleTo(1.0, 50);

            // Show loading animation
            await AnimateExit();

            // Navigate to main app using .NET MAUI 9 pattern
            if (Application.Current != null && Application.Current.Windows.Count > 0)
            {
                Application.Current.Windows[0].Page = new AppShell();
            }
        }
        else
        {
            // Shake the input to indicate error
            await ShakeView(NameEntry);
            await DisplayAlert("Input Required", "Please enter your name to continue.", "OK");
        }
    }

    private async Task AnimateExit()
    {
        // Hide input stack
        var hideInput = InputStack.FadeTo(0, 300);
        var slideInput = InputStack.TranslateTo(0, -30, 300, Easing.CubicIn);

        // Show loading dots
        LoadingDots.IsVisible = true;
        LoadingDots.Opacity = 0;
        await Task.WhenAll(hideInput, slideInput);

        await LoadingDots.FadeTo(1, 200);

        // Animate loading dots
        _ = AnimateLoadingDots();

        // Wait a bit for effect
        await Task.Delay(1000);

        // Fade out everything
        await ContentStack.FadeTo(0, 400);
    }

    private async Task AnimateLoadingDots()
    {
        var dots = new[] { Dot1, Dot2, Dot3 };

        for (int i = 0; i < 3; i++)
        {
            foreach (var dot in dots)
            {
                _ = Task.Run(async () =>
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await dot.ScaleTo(1.3, 200);
                        await dot.ScaleTo(1.0, 200);
                    });
                });
                await Task.Delay(150);
            }
        }
    }

    private async Task ShakeView(View view)
    {
        for (int i = 0; i < 3; i++)
        {
            await view.TranslateTo(-10, 0, 50);
            await view.TranslateTo(10, 0, 50);
        }
        await view.TranslateTo(0, 0, 50);
    }
}