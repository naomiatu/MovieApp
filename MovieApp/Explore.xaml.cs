using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieApp;

public partial class Explore : ContentPage
{
    public Explore()
    {
        BackgroundColor = Colors.Black;
        Title = "Explore";
        Content = new VerticalStackLayout
        {
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label
                {
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    Text = "Explore", TextColor = Colors.White,
                    FontFamily = "OpenSans-SemiBold",
                    FontSize = 40,
                    FontAttributes = FontAttributes.Bold,

                }
            }
        };

    }
}