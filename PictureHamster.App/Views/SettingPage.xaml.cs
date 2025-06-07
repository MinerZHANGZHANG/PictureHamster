using PictureHamster.App.ViewModels;

namespace PictureHamster.App.Views;

public partial class SettingPage : ContentPage
{
	public SettingPage(SettingPageViewModel settingPageViewModel) 
	{
        BindingContext = settingPageViewModel;
        settingPageViewModel.Init();
        InitializeComponent();
	}
}