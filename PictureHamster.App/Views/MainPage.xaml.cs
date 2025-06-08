using UraniumUI.Pages;

namespace PictureHamster.App.Views;

public partial class MainPage : UraniumContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    private async void ImportImageButton_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(ImportPage)}");
    }

    private async void ClassifyImageButton_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(ClassifyPage)}");
    }

    private async void RetrieveImageButton_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(RetrievePage)}");
    }
}
