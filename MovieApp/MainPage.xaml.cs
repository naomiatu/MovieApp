namespace MovieApp;

public partial class MainPage : ContentPage
{

    public MainPage()
    {
        InitializeComponent();

        string userName = Preferences.Get("username", "Guest");
        WelcomeLabel.Text = $"Hello, {userName}!";
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Refresh the username each time the page appears
        string userName = Preferences.Get("username", "Guest");
        WelcomeLabel.Text = $"Hello, {userName}!";
    }


}