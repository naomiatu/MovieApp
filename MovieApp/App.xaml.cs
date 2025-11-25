using MovieProject;

namespace MovieApp;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        Page rootPage;

       
        if (Preferences.ContainsKey("username"))
        {
            
            rootPage = new AppShell();
        }
        else
        {
        
            rootPage = new NavigationPage(new SplashPage());
        }

        return new Window(rootPage);
    }
}