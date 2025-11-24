namespace MovieApp;

public partial class MainPage : ContentPage
{

    public MainPage(string userName)
    {
        InitializeComponent();
        WelcomeLabel.Text = $"Hello, {userName}!";
    }
    
    
}