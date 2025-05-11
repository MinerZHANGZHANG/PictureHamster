using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PictureHamster.App.Services;
using PictureHamster.App.Views;
using PictureHamster.Share.Models;

namespace PictureHamster.App.ViewModels;

public partial class CategoryPageViewModel(ImageStorageService imageStorageService) : ObservableObject, IViewModel
{
    /// <summary>
    /// 搜索框文本
    /// </summary>
    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }
    private string _searchText = string.Empty;

    /// <summary>
    /// 类别列表
    /// </summary>
    public ObservableCollection<CategoryItem> Categories
    {
        get => _categories;
        set => SetProperty(ref _categories, value);
    }
    private ObservableCollection<CategoryItem> _categories = [];

    public void Init()
    {
        // 初始化类别列表
        Categories = [.. imageStorageService.CategoryItems.Take(10)];
    }

    /// <summary>
    /// 搜索符合条件的类别
    /// </summary>
    [RelayCommand]
    public void SearchCategories()
    {
        if (string.IsNullOrEmpty(SearchText))
        {
            Categories = [.. imageStorageService.CategoryItems];

        }
        else
        {
            Categories = [..imageStorageService.CategoryItems
                .Where(c => c.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                ];
        }
    }

    /// <summary>
    /// 导航到类别详情
    /// </summary>
    [RelayCommand]
    private async Task ToDetailsPage(CategoryItem category)
    {
        if (category == null)
        {
            // 如果类别为空，则不进行导航
            return;
        }

        // 导航到类别详情页，并传递类别名称
        await Shell.Current.GoToAsync($"//{nameof(CategoryDetailsPage)}?{nameof(CategoryDetailsPage.CategoryName)}={category.Name}");
    }
}
