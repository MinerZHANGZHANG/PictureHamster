using PictureHamster.App.ViewModels;
using PictureHamster.Share.Models;

namespace PictureHamster.App.Views;

[QueryProperty(nameof(CategoryName), nameof(CategoryName))]
public partial class CategoryDetailsPage : ContentPage
{
    public string? CategoryName { get; set; }

    public CategoryDetailsPage()
    {
        InitializeComponent();
    }

    // New event handler for picture selection
    private void OnPictureSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection?.Count > 0)
        {
            // Delegate handling to the viewmodel
            if (BindingContext is CategoryDetailsPageViewModel vm)
            {
                //vm.SelectPictureCommand.Execute(e.CurrentSelection[0]);
            }
        }
    }
}