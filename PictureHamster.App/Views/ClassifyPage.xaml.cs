using PictureHamster.App.ViewModels;

namespace PictureHamster.App.Views;

public partial class ClassifyPage : ContentPage
{
    public ClassifyPage(ClassifyPageViewModel classifyPageViewModel)
    {
        InitializeComponent();
        BindingContext = classifyPageViewModel;
        classifyPageViewModel.Init();
    }
}