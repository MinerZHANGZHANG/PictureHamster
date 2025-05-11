using PictureHamster.App.ViewModels;

namespace PictureHamster.App.Views;

public partial class ImportPage : ContentPage
{
    public ImportPage(ImportPageViewModel importPageViewModel)
    {
        InitializeComponent();
        BindingContext = importPageViewModel;
        importPageViewModel.Init();
    }
}