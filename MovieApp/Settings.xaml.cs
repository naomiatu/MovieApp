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

        private async void LeaveFeedback_Tapped(object sender, EventArgs e)
        {
            bool result = await DisplayAlert(
                "Leave Feedback",
                "Would you like to send feedback to the developers?",
                "Yes",
                "Cancel");

            if (result)
            {
                await DisplayAlert(
                    "Thank You!",
                    "Your feedback helps us improve the app.",
                    "OK");
            }
        }

        private async void ClearCache_Tapped(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert(
                "Clear Cache",
                "This will reset all app settings and preferences. Continue?",
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

                await DisplayAlert(
                    "Success",
                    "Cache cleared successfully. Please restart the app.",
                    "OK");

                // Restart the app
                Application.Current?.CloseWindow(Application.Current.Windows[0]);
                Application.Current?.OpenWindow(new Window(new NavigationPage(new SplashPage())));
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
                // Clear user-specific data but keep theme preference
                Preferences.Remove("username");
                SecureStorage.RemoveAll();

                await DisplayAlert(
                    "Signed Out",
                    "You have been signed out successfully.",
                    "OK");

                // Return to splash/login page
                Application.Current?.CloseWindow(Application.Current.Windows[0]);
                Application.Current?.OpenWindow(new Window(new NavigationPage(new SplashPage())));
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