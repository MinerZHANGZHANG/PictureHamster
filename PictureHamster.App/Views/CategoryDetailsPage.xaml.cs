using PictureHamster.App.ViewModels;

namespace PictureHamster.App.Views;

[QueryProperty(nameof(CategoryName), nameof(CategoryName))]
public partial class CategoryDetailsPage : ContentPage
{
    public string? CategoryName

    {
        get { return _categoryName; }
        set 
        {
            if (_categoryName != value)
            { 
                _categoryName = value;
                viewModel.Init(value);
            } 
        }
    }
    private string? _categoryName = string.Empty;

    private CategoryDetailsPageViewModel viewModel;

    public CategoryDetailsPage(CategoryDetailsPageViewModel categoryDetailsPageViewModel)
    {
        InitializeComponent();
        viewModel = categoryDetailsPageViewModel;
        BindingContext = categoryDetailsPageViewModel;
    }
}