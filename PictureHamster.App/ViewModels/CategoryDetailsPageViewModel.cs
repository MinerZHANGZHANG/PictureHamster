using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PictureHamster.App.Services;
using PictureHamster.App.Utils;
using PictureHamster.Share.Models;
using UraniumUI.Dialogs;

namespace PictureHamster.App.ViewModels;

/// <summary>
/// 类别详情页视图模型
/// </summary>
public partial class CategoryDetailsPageViewModel(IDialogService dialogService, ImageStorageService imageStorageService) : ObservableObject, IViewModel
{
    /// <summary>
    /// 页面类别
    /// </summary>
    public string PageCategoryName
    {
        get => _pageCategory;
        set
        {
            SetProperty(ref _pageCategory, value);
            OnPropertyChanged(nameof(PageTitle));
        }
    }
    private string _pageCategory = string.Empty;

    /// <summary>
    /// 页面标题
    /// </summary>
    public string PageTitle => $"类别页面-{PageCategoryName}";

    /// <summary>
    /// 中间部分是否显示单幅图片
    /// </summary>
    public bool IsShowSingleImage => SelectedImageItem != null;

    /// <summary>
    /// 中间部分是否显示图片列表
    /// </summary>
    public bool IsShowImages => SelectedImageItem == null && ImageItemsPageCollection.Any();

    /// <summary>
    /// 中间部分是否显示空白提示
    /// </summary>
    public bool IsShowEmptyTip => !IsShowImages && !IsShowSingleImage;

    /// <summary>
    /// 当前选中的图片
    /// </summary>
    public ImageItem? SelectedImageItem
    {
        get => _selectedImageItem;
        set
        {
            SetProperty(ref _selectedImageItem, value);
            UpdateInputOnSelectedImage(value);
            if (value != null)
            {
                SmallImageItemsPageCollection.SetPageIndexByItem(value);
            }
            OnPropertyChanged(nameof(IsShowSingleImage));
            OnPropertyChanged(nameof(IsShowImages));
            OnPropertyChanged(nameof(IsShowEmptyTip));
        }
    }
    private ImageItem? _selectedImageItem;

    /// <summary>
    /// 类别下所有图片分页
    /// </summary>
    public PageCollection<ImageItem> ImageItemsPageCollection
    {
        get => _imageItemsPageCollection;
        set
        {
            SetProperty(ref _imageItemsPageCollection, value);
            OnPropertyChanged(nameof(IsShowSingleImage));
            OnPropertyChanged(nameof(IsShowImages));
            OnPropertyChanged(nameof(IsShowEmptyTip));
        }
    }
    private PageCollection<ImageItem> _imageItemsPageCollection = [];

    /// <summary>
    /// 更小的图片类别分页，展示单副图片
    /// </summary>
    public PageCollection<ImageItem> SmallImageItemsPageCollection
    {
        get => _smallImageItemsPageCollection;
        set
        {
            SetProperty(ref _smallImageItemsPageCollection, value);
        }
    }
    private PageCollection<ImageItem> _smallImageItemsPageCollection = [];

    /// <summary>
    /// 图片类别输入文本
    /// </summary>
    public string CategoryInputText
    {
        get => _categoryInputName;
        set => SetProperty(ref _categoryInputName, value);
    }
    private string _categoryInputName = string.Empty;

    /// <summary>
    /// 图片关键信息输入文本
    /// </summary>
    public string KeywordInputText
    {
        get => _keywordInputText;
        set => SetProperty(ref _keywordInputText, value);
    }
    private string _keywordInputText = string.Empty;

    /// <summary>
    /// 图片详情信息输入文本
    /// </summary>
    public string DescriptionText
    {
        get => _descriptionText;
        set => SetProperty(ref _descriptionText, value);
    }
    private string _descriptionText = string.Empty;

    /// <summary>
    /// 图片类别列表
    /// </summary>
    public ObservableCollection<string> ImageCategories
    {
        get => _imageCategories;
        set => SetProperty(ref _imageCategories, value);
    }
    private ObservableCollection<string> _imageCategories = [];

    /// <summary>
    /// 图片关键信息列表
    /// </summary>
    public ObservableCollection<string> ImageKeywords
    {
        get => _imageKeywords;
        set => SetProperty(ref _imageKeywords, value);
    }
    private ObservableCollection<string> _imageKeywords = [];

    /// <summary>
    /// 初始化类别详情页
    /// </summary>
    /// <param name="categoryName"></param>
    public void Init(string? categoryName)
    {
        if (!string.IsNullOrEmpty(categoryName))
        {
            PageCategoryName = categoryName;
            LoadCategoryImageItems(categoryName);
        }
    }

    /// <summary>
    /// 加载类别下的图片
    /// </summary>
    /// <param name="categoryName">类别名称</param>
    private void LoadCategoryImageItems(string categoryName)
    {
        SelectedImageItem = null;
        var imageItems = imageStorageService.GetImageItemsByCategory(categoryName);
        ImageItemsPageCollection = new PageCollection<ImageItem>(imageItems
            , 0
            , 9);
        SmallImageItemsPageCollection = new PageCollection<ImageItem>(imageItems
            , 0
            , 1);
    }

    /// <summary>
    /// 显示操作介绍
    /// </summary>
    [RelayCommand]
    public async Task DisplayInfo()
    {
        StackLayout view;
        if (IsShowImages)
        {
            view = new StackLayout()
            {
                Children =
                {
                    new Label { Text = "你可以使用这个界面下方按钮修改图片的分类信息", FontSize = 16, HorizontalOptions = LayoutOptions.Center },
                },
                Spacing = 8,
                Padding = new Thickness(4)
            };
        }
        else
        {
            view = new StackLayout()
            {
                Children =
                {
                    new Label { Text = "你可以点击图片查看具体信息，或使用下方的按钮导出类别下的图片", FontSize = 16, HorizontalOptions = LayoutOptions.Center },
                },
                Spacing = 8,
                Padding = new Thickness(4)
            };
        }

        await dialogService.DisplayViewAsync("操作介绍", view);
    }

    /// <summary>
    /// 刷新当前状态，不过目前没有什么状态需要刷新的
    /// </summary>
    [RelayCommand]
    public void RefreshState()
    {
        LoadCategoryImageItems(PageCategoryName);
    }

    /// <summary>
    /// 返回到未选中状态
    /// </summary>
    [RelayCommand]
    public void ReturnToUnselectedState()
    {
        SelectedImageItem = null;
    }

    /// <summary>
    /// 切换下一个目录或下一幅图片
    /// </summary>
    [RelayCommand]
    public void Next()
    {
        if (SelectedImageItem == null)
        {
            ImageItemsPageCollection.NextPage();
        }
        else
        {
            SmallImageItemsPageCollection.NextPage();
        }
    }

    /// <summary>
    /// 切换上一个目录或上一幅图片
    /// </summary>
    [RelayCommand]
    public void Previous()
    {
        if (SelectedImageItem == null)
        {
            ImageItemsPageCollection.PreviousPage();
        }
        else
        {
            SmallImageItemsPageCollection.PreviousPage();
        }
    }


    /// <summary>
    /// 导出分类后的图片
    /// </summary>
    [RelayCommand]
    public void ExportImages()
    {

    }

    /// <summary>
    /// 查看导出目录
    /// </summary>
    [RelayCommand]
    public void ViewExportedDirectory()
    {

    }

    /// <summary>
    /// 更新图片的分类
    /// </summary>
    [RelayCommand]
    public void UpdateImageCategory()
    {
        if (string.IsNullOrEmpty(CategoryInputText))
        {
            return;
        }

        if (SelectedImageItem == null)
        {
            return;
        }

        imageStorageService.UpdateImageCategories(SelectedImageItem, [CategoryInputText], true);

        SelectedImageItem = null;
        LoadCategoryImageItems(PageCategoryName);
    }

    /// <summary>
    /// 新增图片的类别
    /// </summary>
    [RelayCommand]
    public void AddImageCategories()
    {
        if (string.IsNullOrEmpty(CategoryInputText) || ImageCategories.Contains(CategoryInputText))
        {
            return;
        }

        if (SelectedImageItem == null)
        {
            return;
        }

        ImageCategories.Add(KeywordInputText);
        imageStorageService.UpdateImageCategories(SelectedImageItem, [CategoryInputText], true);
    }

    /// <summary>
    /// 移除图片的类别信息
    /// </summary>
    [RelayCommand]
    public async Task RemoveImageCategory(string? category)
    {
        if (string.IsNullOrEmpty(category) || !ImageCategories.Contains(category))
        {
            return;
        }

        if (SelectedImageItem == null)
        {
            return;
        }

        if (ImageCategories.Count == 1)
        {
            await dialogService.DisplayTextPromptAsync("删除类别失败", "已分类的图片至少需要有一个类别");
            return;
        }

        if (!await dialogService.ConfirmAsync("移除图片类别", $"你确定要删除[{category}]吗?"))
        {
            return;
        }

        ImageCategories.Remove(category);
        imageStorageService.UpdateImageCategories(SelectedImageItem, [CategoryInputText], true);

        // 如果删除的类别是当前选中的类别，则清空选中图片
        if (category == PageCategoryName)
        {
            SelectedImageItem = null;
            LoadCategoryImageItems(PageCategoryName);
        }
    }

    /// <summary>
    /// 新增图片的关键信息
    /// </summary>
    [RelayCommand]
    public void AddImageKeywords()
    {
        if (string.IsNullOrEmpty(KeywordInputText) || ImageKeywords.Contains(KeywordInputText))
        {
            return;
        }

        if (SelectedImageItem == null)
        {
            return;
        }

        ImageKeywords.Add(KeywordInputText);
        imageStorageService.UpdateImageKeywords(SelectedImageItem, ImageKeywords, true);
    }

    /// <summary>
    /// 移除图片的关键信息
    /// </summary>
    [RelayCommand]
    public async Task RemoveImageKeyword(string? keyword)
    {
        if (string.IsNullOrEmpty(keyword) || !ImageKeywords.Contains(keyword))
        {
            return;
        }

        if (SelectedImageItem == null)
        {
            return;
        }

        if (!await dialogService.ConfirmAsync("移除关键信息", $"你确定要删除[{keyword}]吗?"))
        {
            return;
        }

        ImageKeywords.Remove(keyword);
        imageStorageService.UpdateImageKeywords(SelectedImageItem, ImageKeywords, true);
    }

    /// <summary>
    /// 更新图片的详情信息
    /// </summary>
    [RelayCommand]
    public void UpdateImageDescription()
    {
        if (SelectedImageItem == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(DescriptionText))
        {
            return;
        }

        imageStorageService.UpdateImageDescription(SelectedImageItem, DescriptionText, true);
    }

    private void UpdateInputOnSelectedImage(ImageItem? imageItem)
    {
        if (imageItem == null)
        {
            CategoryInputText = string.Empty;
            DescriptionText = string.Empty;
            ImageKeywords.Clear();
            return;
        }
        DescriptionText = imageItem.Description;
        ImageKeywords = [.. imageItem.KeyWords];
        ImageCategories = [.. imageItem.Categories];
    }

}