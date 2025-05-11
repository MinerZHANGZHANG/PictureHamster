using PictureHamster.App.ViewModels;

namespace PictureHamster.App.Views;

public partial class CategoryPage : ContentPage
{
    public CategoryPage(CategoryPageViewModel categoryPageViewModel)
    {
        InitializeComponent();
        BindingContext = categoryPageViewModel;
        categoryPageViewModel.Init();
    }
}