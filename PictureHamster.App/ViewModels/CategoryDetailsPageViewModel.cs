using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PictureHamster.Share.Models;
using UraniumUI.Dialogs;

namespace PictureHamster.App.ViewModels;

/// <summary>
/// 类别详情页视图模型
/// </summary>
public partial class CategoryDetailsPageViewModel(IDialogService dialogService) : ObservableObject, IViewModel
{
    #region Top bar

    /// <summary>
    /// 顶部标题栏标题，根据用户是否选中图片而变化
    /// </summary>
    public string TopBarTitle
    {
        get => _topBarTitle;
        set => SetProperty(ref _topBarTitle, value);
    }
    private string _topBarTitle = "当前未选中图片";

    /// <summary>
    /// 顶部标题栏右侧按钮文本，根据用户是否选中目录而变化
    /// </summary>
    public string TopBarButtonText
    {
        get => _topBarButtonText;
        set => SetProperty(ref _topBarButtonText, value);
    }
    private string _topBarButtonText = "刷新";

    public bool HasImageSelected => ImageItems.Any(i => i.IsSelected);

    /// <summary>
    /// 显示操作介绍
    /// </summary>
    [RelayCommand]
    public async Task DisplayInfo()
    {
        var view = new StackLayout
        {
            Children =
                {
                    new Label { Text = "你可以使用这个界面下方的按钮导入图片", FontSize = 16, HorizontalOptions = LayoutOptions.Center },
                    new Label { Text = "导入的图片可以在后续进行分类", FontSize = 16, HorizontalOptions = LayoutOptions.Center }
                },
            Spacing = 8,
            Padding = new Thickness(4)
        };

        await dialogService.DisplayViewAsync("操作介绍", view);
    }

    /// <summary>
    /// 刷新当前状态，不过目前没有什么状态需要刷新的
    /// </summary>
    [RelayCommand]
    public void RefreshState()
    {
        // Do nothing
    }

    /// <summary>
    /// 返回到未选中状态
    /// </summary>
    [RelayCommand]
    public void ReturnToUnselectedState()
    {
        TopBarTitle = "当前目录: 未选中";
        TopBarButtonText = "刷新";
    }

    #endregion

    #region Middle grid

    /// <summary>
    /// 类别下的图像列表    
    /// </summary>

    public ObservableCollection<ImageItem> ImageItems
    {
        get => _imageItems;
        set
        {
            SetProperty(ref _imageItems, value);
            //SetProperty(ref HasFolders, value.Any());
        }
    }
    private ObservableCollection<ImageItem> _imageItems = [];

    /// <summary>
    /// 当前选中的图片
    /// </summary>
    public ImageItem? SelectedImageItem
    {
        get => _selectedImageItem;
        set
        {
            SetProperty(ref _selectedImageItem, value);
        }
    }
    private ImageItem? _selectedImageItem ;

    /// <summary>
    /// 选中图片
    /// </summary>
    [RelayCommand]
    public void SelectDirectory(ImageItem imageItem)
    {

    }

    /// <summary>
    /// 切换下一个目录或下一幅图片
    /// </summary>
    [RelayCommand]
    public void Next()
    {

    }

    /// <summary>
    /// 切换上一个目录或上一幅图片
    /// </summary>
    [RelayCommand]
    public void Previous()
    {

    }

    #endregion

    #region Bottom

    /// <summary>
    /// 图片类别输入文本
    /// </summary>
    public string CategoryNameText
    {
        get => _categoryName;
        set => SetProperty(ref _categoryName, value);
    }
    private string _categoryName=string.Empty;

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
    public string ImageDescriptionText
    {
        get => _imageDescriptionText;
        set => SetProperty(ref _imageDescriptionText, value);
    }
    private string _imageDescriptionText = string.Empty;

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

    }

    /// <summary>
    /// 新增图片的关键信息
    /// </summary>
    [RelayCommand]
    public void AddImageKeywords()
    {

    }

    /// <summary>
    /// 移除图片的关键信息
    /// </summary>
    [RelayCommand]
    public void RemoveImageKeyword()
    {

    }

    /// <summary>
    /// 更新图片的详情信息
    /// </summary>
    [RelayCommand]
    public void UpdateImageDescription()
    {

    }

    #endregion
}