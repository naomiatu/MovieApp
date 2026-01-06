using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Controls;

namespace MovieApp
{
    /// Global theme manager that provides centralized theme management across the app.
    /// Implements INotifyPropertyChanged to notify all pages when theme changes.
    public class ThemeManager : INotifyPropertyChanged
    {
        private static ThemeManager _instance;
        private bool _isDarkTheme;

        public static ThemeManager Instance => _instance ??= new ThemeManager();

        public event PropertyChangedEventHandler PropertyChanged;

        private ThemeManager()
        {
            // Load saved theme preference (default to dark theme)
            _isDarkTheme = Preferences.Get("IsDarkTheme", true);
        }

        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set
            {
                if (_isDarkTheme != value)
                {
                    _isDarkTheme = value;
                    Preferences.Set("IsDarkTheme", value);
                    OnPropertyChanged();
                    NotifyThemeChanged();
                }
            }
        }

        // Theme Colors
        public Color BackgroundColor => IsDarkTheme
            ? Color.FromArgb("#0F0F0F")
            : Color.FromArgb("#F5F5F5");

        public Color BackgroundGradientStart => IsDarkTheme
            ? Color.FromArgb("#1a1a1a")
            : Color.FromArgb("#FFFFFF");

        public Color BackgroundGradientEnd => IsDarkTheme
            ? Color.FromArgb("#0F0F0F")
            : Color.FromArgb("#F5F5F5");

        public Color CardBackgroundColor => IsDarkTheme
            ? Color.FromArgb("#1a1a1a")
            : Colors.White;

        public Color TextColor => IsDarkTheme
            ? Colors.White
            : Colors.Black;

        public Color SubtextColor => IsDarkTheme
            ? Color.FromArgb("#999999")
            : Color.FromArgb("#666666");

        public Color BorderColor => IsDarkTheme
            ? Color.FromArgb("#333333")
            : Color.FromArgb("#DDDDDD");

        public Color SearchBarBackground => IsDarkTheme
            ? Color.FromArgb("#1a1a1a")
            : Colors.White;

        public Color IconBackgroundColor => IsDarkTheme
            ? Color.FromArgb("#4d4d4d")
            : Color.FromArgb("#F0F0F0");

        public Color SectionHeaderColor => IsDarkTheme
            ? Color.FromArgb("#888888")
            : Color.FromArgb("#888888");

        // Accent colors (remain same in both themes)
        public Color AccentColor => Color.FromArgb("#FFB800");
        public Color GreenAccent => Color.FromArgb("#4CAF50");
        public Color OrangeAccent => Color.FromArgb("#FF6B00");
        public Color BlueAccent => Color.FromArgb("#2196F3");

        private void NotifyThemeChanged()
        {
            OnPropertyChanged(nameof(BackgroundColor));
            OnPropertyChanged(nameof(BackgroundGradientStart));
            OnPropertyChanged(nameof(BackgroundGradientEnd));
            OnPropertyChanged(nameof(CardBackgroundColor));
            OnPropertyChanged(nameof(TextColor));
            OnPropertyChanged(nameof(SubtextColor));
            OnPropertyChanged(nameof(BorderColor));
            OnPropertyChanged(nameof(SearchBarBackground));
            OnPropertyChanged(nameof(IconBackgroundColor));
            OnPropertyChanged(nameof(SectionHeaderColor));
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// Resets theme to default (dark mode)
        public void ResetToDefault()
        {
            IsDarkTheme = true;
        }
    }
}