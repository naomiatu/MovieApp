using MovieProject;

namespace MovieApp;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        // Use NavigationPage to allow navigation
        MainPage = new NavigationPage(new SplashPage());

    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}