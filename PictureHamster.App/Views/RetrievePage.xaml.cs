using PictureHamster.App.ViewModels;

namespace PictureHamster.App.Views;

public partial class RetrievePage : ContentPage
{
    public RetrievePage(RetrievePageViewModel retrievePageViewModel)
    {
        BindingContext = retrievePageViewModel;
        retrievePageViewModel.Init();
        InitializeComponent();
    }
}