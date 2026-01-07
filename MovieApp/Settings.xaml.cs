using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MovieProject;

namespace MovieApp
{
    public partial class Settings : ContentPage, INotifyPropertyChanged
    {
        private readonly ThemeManager _themeManager;

        public Settings()
        {
            InitializeComponent();

            // Get theme manager instance
            _themeManager = ThemeManager.Instance;

            // Set initial switch state
            ThemeSwitch.IsToggled = _themeManager.IsDarkTheme;

            // Bind to theme manager
            BindingContext = _themeManager;
        }

        private void ThemeSwitch_Toggled(object sender, ToggledEventArgs e)
        {
            // Update theme manager - this will notify all pages
            _themeManager.IsDarkTheme = e.Value;
        }

        private async void Back_Clicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }

        private async void ClearCache_Tapped(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert(
                "Clear Cache",
                "This will reset all your reviews, watched movies, and preferences. Continue?",
                "Yes",
                "No");

            if (!confirm)
                return;

            try
            {
                // Clear all preferences
                Preferences.Clear();

                // Clear secure storage (reviews, watched movies, etc.)
                SecureStorage.RemoveAll();

                // Reset theme to default
                _themeManager.ResetToDefault();

                // Clear static caches if they exist
                try
                {
                    // Clear MainPage review cache
                    MainPage.InvalidateReviewCache();
                }
                catch { }

                await DisplayAlert(
                    "Success",
                    "Cache cleared successfully!",
                    "OK");

                // Navigate back to main page smoothly without closing/reopening windows
                Application.Current.MainPage = new AppShell();
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Error",
                    $"Failed to clear cache: {ex.Message}",
                    "OK");
            }
        }

        private async void SignOut_Tapped(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert(
                "Sign Out",
                "Are you sure you want to sign out?",
                "Yes",
                "No");

            if (!confirm)
                return;

            try
            {
                // Clear username
                Preferences.Remove("username");

                // Clear secure storage (reviews, watched movies, etc.)
                SecureStorage.RemoveAll();

                // Clear static caches
                try
                {
                    MainPage.InvalidateReviewCache();
                }
                catch { }

                await DisplayAlert(
                    "Signed Out",
                    "You have been signed out successfully.",
                    "OK");

                // Navigate to splash page
                Application.Current.MainPage = new NavigationPage(new SplashPage());
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Error",
                    $"Failed to sign out: {ex.Message}",
                    "OK");
            }
        }

        public new event PropertyChangedEventHandler? PropertyChanged;

        protected new void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}