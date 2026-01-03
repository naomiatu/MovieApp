using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using MovieProject;

namespace MovieApp;



    public partial class Settings : ContentPage, INotifyPropertyChanged
    {
        private bool _isDarkTheme;
        private Color _backgroundColor;
        private Color _textColor;
        private Color _subtextColor;
        private Color _cardBackgroundColor;
        private Color _iconBackgroundColor;
        private Color _sectionHeaderColor;

        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set
            {
                _isDarkTheme = value;
                UpdateThemeColors();
                OnPropertyChanged();
            }
        }

        public Color BackgroundColor
        {
            get => _backgroundColor;
            set { _backgroundColor = value; OnPropertyChanged(); }
        }

        public Color TextColor
        {
            get => _textColor;
            set { _textColor = value; OnPropertyChanged(); }
        }

        public Color SubtextColor
        {
            get => _subtextColor;
            set { _subtextColor = value; OnPropertyChanged(); }
        }

        public Color CardBackgroundColor
        {
            get => _cardBackgroundColor;
            set { _cardBackgroundColor = value; OnPropertyChanged(); }
        }

        public Color IconBackgroundColor
        {
            get => _iconBackgroundColor;
            set { _iconBackgroundColor = value; OnPropertyChanged(); }
        }

        public Color SectionHeaderColor
        {
            get => _sectionHeaderColor;
            set { _sectionHeaderColor = value; OnPropertyChanged(); }
        }

        public Settings()
        {
            InitializeComponent();

            // Load saved theme preference
            IsDarkTheme = Preferences.Get("IsDarkTheme", true);
            ThemeSwitch.IsToggled = IsDarkTheme;

            BindingContext = this;
        }

        private void UpdateThemeColors()
        {
            if (IsDarkTheme)
            {
                // Dark Theme Colors
                BackgroundColor = Color.FromArgb("#2d2d2d");
                TextColor = Colors.White;
                SubtextColor = Color.FromArgb("#999999");
                CardBackgroundColor = Color.FromArgb("#3d3d3d");
                IconBackgroundColor = Color.FromArgb("#4d4d4d");
                SectionHeaderColor = Color.FromArgb("#888888");
            }
            else
            {
                // Light Theme Colors
                BackgroundColor = Color.FromArgb("#F5F5F5");
                TextColor = Colors.Black;
                SubtextColor = Color.FromArgb("#666666");
                CardBackgroundColor = Colors.White;
                IconBackgroundColor = Color.FromArgb("#F0F0F0");
                SectionHeaderColor = Color.FromArgb("#888888");
            }

            // Save preference
            Preferences.Set("IsDarkTheme", IsDarkTheme);
        }

        private void ThemeSwitch_Toggled(object sender, ToggledEventArgs e)
        {
            IsDarkTheme = e.Value;
        }

        private async void Back_Clicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }

        private async void LeaveFeedback_Tapped(object sender, EventArgs e)
        {
            await DisplayAlert("Leave Feedback", "This would open a feedback form.", "OK");
        }

    private async void ClearCache_Tapped(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert(
            "Clear Cache",
            "This will reset all app settings. Continue?",
            "Yes",
            "No");

        if (!confirm)
            return;

        Preferences.Clear();

        await DisplayAlert(
            "Success",
            "Cache cleared successfully.",
            "OK");

        // Optional: force logout after cache clear
        Application.Current.CloseWindow(
            Application.Current.Windows[0]);

        Application.Current.OpenWindow(
            new Window(new NavigationPage(new SplashPage())));
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

        // Clear user data
        Preferences.Remove("username");
        Preferences.Remove("IsDarkTheme");

        // Recreate the window so CreateWindow() runs again
        Application.Current.CloseWindow(
            Application.Current.Windows[0]);

        Application.Current.OpenWindow(
            new Window(new NavigationPage(new SplashPage())));
    }


    public new event PropertyChangedEventHandler? PropertyChanged;

        protected new void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
