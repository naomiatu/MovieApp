using Microsoft.Maui.Controls;
using MovieApp;
using System;
using System.Threading.Tasks;

namespace MovieProject;

public partial class SplashPage : ContentPage
{
    private bool _isAnimating = false;

    public SplashPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!_isAnimating)
        {
            _isAnimating = true;

            // Start animations and movie loading in parallel
            var animationTask = AnimateIntro();
            var loadingTask = PreloadAllDataAsync();

            await Task.WhenAll(animationTask, loadingTask);

            _isAnimating = false;
        }
    }

    private async Task PreloadAllDataAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("🎬 Starting complete data preload...");
            System.Diagnostics.Debug.WriteLine($"💾 Memory before load: {GC.GetTotalMemory(false) / 1024 / 1024} MB");

            // Load all movies through centralized service (ONE TIME ONLY)
            var dataService = MovieDataService.Instance;
            var movies = await dataService.GetMoviesAsync();

            System.Diagnostics.Debug.WriteLine($"✅ Preloaded {movies?.Count ?? 0} movies");
            System.Diagnostics.Debug.WriteLine($"💾 Memory after load: {GC.GetTotalMemory(false) / 1024 / 1024} MB");

            // Clear old image cache to prevent memory buildup
            ImageCacheManager.ClearAllCache();

            System.Diagnostics.Debug.WriteLine("✅ All data preloaded successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ Data preload failed (app will still work): {ex.Message}");

            // Force cleanup on failure
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
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

        // Only animate for a limited time to avoid memory buildup
        int iterations = 0;
        const int MAX_ITERATIONS = 10;

        while (iterations < MAX_ITERATIONS && ContentStack.Opacity > 0)
        {
            foreach (var emoji in emojis)
            {
                var delay = random.Next(0, 500);
                _ = Task.Run(async () =>
                {
                    await Task.Delay(delay);
                    try
                    {
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            if (emoji != null && emoji.IsLoaded)
                            {
                                await emoji.TranslateTo(0, -10, 1000, Easing.SinInOut);
                                await emoji.TranslateTo(0, 0, 1000, Easing.SinInOut);
                            }
                        });
                    }
                    catch
                    {
                        // Ignore animation errors if page is disposed
                    }
                });
            }

            await Task.Delay(2000);
            iterations++;
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

            // Log final memory state
            var dataService = MovieDataService.Instance;
            System.Diagnostics.Debug.WriteLine($"💾 Memory before navigation: {GC.GetTotalMemory(false) / 1024 / 1024} MB");
            System.Diagnostics.Debug.WriteLine($"✅ {dataService.Count} movies ready for use");

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
        await AnimateLoadingDots();

        // Wait a bit for effect
        await Task.Delay(800);

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
                    try
                    {
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            if (dot != null && dot.IsLoaded)
                            {
                                await dot.ScaleTo(1.3, 200);
                                await dot.ScaleTo(1.0, 200);
                            }
                        });
                    }
                    catch
                    {
                        // Ignore animation errors
                    }
                });
                await Task.Delay(150);
            }
        }
    }

    private async Task ShakeView(View view)
    {
        try
        {
            for (int i = 0; i < 3; i++)
            {
                await view.TranslateTo(-10, 0, 50);
                await view.TranslateTo(10, 0, 50);
            }
            await view.TranslateTo(0, 0, 50);
        }
        catch
        {
            // Ignore animation errors
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Stop any ongoing animations
        ContentStack?.CancelAnimations();

        System.Diagnostics.Debug.WriteLine("🧹 SplashPage disposed");
    }
}