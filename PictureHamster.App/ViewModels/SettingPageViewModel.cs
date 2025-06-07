using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiteDB;
using PictureHamster.App.Services;
using PictureHamster.Share.Models;
using UraniumUI.Dialogs;

namespace PictureHamster.App.ViewModels;

public partial class SettingPageViewModel(ImageStorageService imageStorageService,IDialogService dialogService) : ObservableObject, IViewModel
{
    /// <summary>
    /// ���ӳ��
    /// </summary>
    public ObservableCollection<CategoryMapping> MainCategories
    {
        get => _mainCategories;
        set => SetProperty(ref _mainCategories, value);
    }
    private ObservableCollection<CategoryMapping> _mainCategories = [];

    /// <summary>
    /// ��ǰѡ��������
    /// </summary>
    public CategoryMapping? SelectedMainCategory
    {
        get => _selectedMainCategory;
        set
        {
            SetProperty(ref _selectedMainCategory, value);
            if (value == null)
            {
                SubCategories = [];
            }
            else
            {
                SubCategories = [.. value.MappedCategories];
            }
        }
    }
    private CategoryMapping? _selectedMainCategory;

    /// <summary>
    /// ��ǰѡ��������Ӧ�������
    /// </summary>
    public ObservableCollection<string> SubCategories
    {
        get => _subCategories;
        set => SetProperty(ref _subCategories, value);
    }
    private ObservableCollection<string> _subCategories = [];

    /// <summary>
    /// ����������ı�
    /// </summary>
    public string NewMainCategoryText
    {
        get => _newMainCategoryText;
        set => SetProperty(ref _newMainCategoryText, value);
    }
    private string _newMainCategoryText = string.Empty;

    /// <summary>
    /// ����������ı�
    /// </summary>
    public string NewSubCategoryText
    {
        get => _newSubCategoryText;
        set => SetProperty(ref _newSubCategoryText, value);
    }
    private string _newSubCategoryText = string.Empty;

    public void Init()
    {
        MainCategories = [.. imageStorageService.CategoryMappings];
    }

    [RelayCommand]
    private void AddMainCategory()
    {
        var mainCategoryText = NewMainCategoryText?.Trim();
        if (!string.IsNullOrWhiteSpace(mainCategoryText) &&
            !MainCategories.Any(c => c.Name == mainCategoryText))
        {
            MainCategories.Add(new CategoryMapping { Name = mainCategoryText });
            NewMainCategoryText = string.Empty;

            SelectedMainCategory = MainCategories.LastOrDefault();
        }
    }

    [RelayCommand]
    private void DeleteMainCategory(CategoryMapping? category)
    {
        if (category != null)
        {
            MainCategories.Remove(category);
            if (SelectedMainCategory == category)
            {
                SelectedMainCategory = MainCategories.FirstOrDefault();
            }
        }
    }

    [RelayCommand]
    private void AddSubCategory()
    {
        if (SelectedMainCategory == null)
        {
            return;
        }

        var subCategoryText = NewSubCategoryText?.Trim();
        if (!string.IsNullOrWhiteSpace(subCategoryText) &&
            !SubCategories.Contains(subCategoryText))
        {
            SubCategories.Add(subCategoryText);
            NewSubCategoryText = string.Empty;

            SelectedMainCategory.MappedCategories = [.. SubCategories];
        }
    }

    [RelayCommand]
    private void DeleteSubCategory(string category)
    {
        if (SelectedMainCategory == null)
        {
            return;
        }

        if (category != null)
        {
            SubCategories.Remove(category);
            SelectedMainCategory.MappedCategories = [.. SubCategories];
        }
    }

    [RelayCommand]
    private void SaveCategoryMappings()
    {
        imageStorageService.UpdateImageCategoryMappings(MainCategories);
        dialogService.ConfirmAsync("����ɹ�", "�ѱ������ӳ�����á�");
    }
}