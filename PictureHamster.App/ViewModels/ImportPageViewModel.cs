using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiteDB;
using PictureHamster.App.Services;
using PictureHamster.App.Utils;
using PictureHamster.Share.Models;
using UraniumUI.Dialogs;

namespace PictureHamster.App.ViewModels;

public partial class ImportPageViewModel(IDialogService dialogService, ImageStorageService imageStorageService) : ObservableObject, IViewModel
{
    #region 字段和属性

    /// <summary>
    /// 是否一并导入所在目录下的所有图片
    /// </summary>
    public bool IsImportAllImageInDirectory
    {
        get => _isImportAllImageInDirectory;
        set => SetProperty(ref _isImportAllImageInDirectory, value);
    }
    private bool _isImportAllImageInDirectory = true;

    /// <summary>
    /// 已经导入的目录信息
    /// </summary>
    public List<DirectoryItem> DirectoryItems
    {
        get => _directoryItems;
        set
        {
            SetProperty(ref _directoryItems, value);
            OnPropertyChanged(nameof(IsShowImages));
            OnPropertyChanged(nameof(IsShowDirectories));
            OnPropertyChanged(nameof(IsShowEmptyTip));

            DirectoryItemsPageCollection = new PageCollection<DirectoryItem>(DirectoryItems.OrderByDescending(dir => dir.LatestPicture.LastModifyTime), 0, 9);
        }
    }
    private List<DirectoryItem> _directoryItems = [];

    /// <summary>
    /// 当前选中的目录
    /// </summary>
    public DirectoryItem? SelectedDirectoryItem
    {
        get => _selectedDirectoryItem;
        set
        {
            SetProperty(ref _selectedDirectoryItem, value);
            OnPropertyChanged(nameof(IsShowImages));
            OnPropertyChanged(nameof(IsShowDirectories));
            OnPropertyChanged(nameof(IsShowEmptyTip));

            OnPropertyChanged(nameof(TopBarButtonCommand));
            OnPropertyChanged(nameof(TopBarButtonText));
            OnPropertyChanged(nameof(TopBarTitle));

            ImageItemsPageCollection = value != null
                  ? new PageCollection<ImageItem>(value.ImageItems.OrderByDescending(i => i.LastModifyTime), 0, 1)
                  : [];
        }
    }
    private DirectoryItem? _selectedDirectoryItem;

    /// <summary>
    /// 所有目录信息分页
    /// </summary>
    public PageCollection<DirectoryItem> DirectoryItemsPageCollection
    {
        get => _directoryItemsPageCollection;
        set
        {
            SetProperty(ref _directoryItemsPageCollection, value);
        }
    }
    private PageCollection<DirectoryItem> _directoryItemsPageCollection = [];

    /// <summary>
    /// 所有图片列表分页
    /// </summary>
    public PageCollection<ImageItem> ImageItemsPageCollection
    {
        get => _imageItemsPageCollection;
        set
        {
            SetProperty(ref _imageItemsPageCollection, value);
        }
    }
    private PageCollection<ImageItem> _imageItemsPageCollection = [];

    /// <summary>
    /// 中间部分是否显示图片
    /// </summary>
    public bool IsShowImages => SelectedDirectoryItem != null;

    /// <summary>
    /// 中间部分是否显示目录
    /// </summary>
    public bool IsShowDirectories => SelectedDirectoryItem == null && DirectoryItems.Count > 0;

    /// <summary>
    /// 中间部分是否显示空白提示
    /// </summary>
    public bool IsShowEmptyTip => !IsShowImages && !IsShowDirectories;

    /// <summary>
    /// 顶部标题栏标题，根据用户是否选中目录而变化
    /// </summary>
    public string TopBarTitle => SelectedDirectoryItem != null
                ? $"{SelectedDirectoryItem.Path}"
                : "未选中目录";

    /// <summary>
    /// 顶部标题栏右侧按钮文本，根据用户是否选中目录而变化
    /// </summary>
    public string TopBarButtonText => SelectedDirectoryItem != null
                ? "返回"
                : "刷新";

    /// <summary>
    /// 顶部标题栏右侧按钮命令，根据用户是否选中目录而变化
    /// </summary>
    public ICommand TopBarButtonCommand => SelectedDirectoryItem != null
                ? ReturnToUnselectedStateCommand
                : RefreshStateCommand;

    #endregion

    #region Command

    /// <summary>
    /// 显示操作介绍
    /// </summary>
    [RelayCommand]
    public async Task DisplayInfo()
    {
        View? content = null;

        // 当选中目录时，展示所选目录信息
        if (SelectedDirectoryItem != null)
        {
            content = new StackLayout
            {
                Children =
                {
                    new Label { Text = $"当前目录: {SelectedDirectoryItem.Path}", FontSize = 16, HorizontalOptions = LayoutOptions.Center },
                    new Label { Text = $"图片数量: {SelectedDirectoryItem.ImageItems.Count}", FontSize = 16, HorizontalOptions = LayoutOptions.Center },
                },
                Spacing = 8,
                Padding = new Thickness(4)
            };
        }
        // 当没有选中目录时，展示已导入目录和图片数量
        else
        {
            content = new StackLayout
            {
                Children =
                {
                    new Label { Text = $"当前已导入目录: {DirectoryItems.Count}", FontSize = 16, HorizontalOptions = LayoutOptions.Center },
                    new Label { Text = $"当前已导入图片: {DirectoryItems.Sum(dir=>dir.ImageItems.Count())}", FontSize = 16, HorizontalOptions = LayoutOptions.Center },
                },
                Spacing = 8,
                Padding = new Thickness(4)
            };
        }

        // TODO:如果快速多次点击OK按钮，会报错"attempt transition task final state when already completed"
        await dialogService.DisplayViewAsync("信息", content);
    }

    /// <summary>
    /// 刷新当前状态，不过目前没有什么状态需要刷新的
    /// </summary>
    [RelayCommand]
    public void RefreshState()
    {
        LoadImages();
    }

    /// <summary>
    /// 返回到未选中状态
    /// </summary>
    [RelayCommand]
    public void ReturnToUnselectedState()
    {
        SelectedDirectoryItem = null;
    }

    /// <summary>
    /// 切换下一个目录或下一幅图片
    /// </summary>
    [RelayCommand]
    public void Next()
    {
        if (SelectedDirectoryItem == null)
        {
            _directoryItemsPageCollection.NextPage();
        }
        else
        {
            _imageItemsPageCollection.NextPage();
        }
    }

    /// <summary>
    /// 切换上一个目录或上一幅图片
    /// </summary>
    [RelayCommand]
    public void Previous()
    {
        if (_selectedDirectoryItem == null)
        {
            _directoryItemsPageCollection.PreviousPage();
        }
        else
        {
            _imageItemsPageCollection.PreviousPage();
        }
    }

    /// <summary>
    /// 导入图片
    /// </summary>
    [RelayCommand]
    public async Task ImportImages()
    {
        // 打开文件选择器选择一或多张图片
        FileResult? photo = await MediaPicker.Default.PickPhotoAsync();
        if (photo == null)
        {
            return;
        }

        using (var stream = await photo.OpenReadAsync())
        {
           
        }
        var directoryPath = Path.GetDirectoryName(photo.FullPath);
        if (string.IsNullOrEmpty(directoryPath))
        {
            return;
        }

        // 开启开关时，导入所在目录下的所有图片
        if (IsImportAllImageInDirectory)
        {
            SaveImportResult([.. ImageStorageService.LoadDirectoryImagePaths(directoryPath)]);
        }
        // 否则只导入选中的图片
        else
        {
            SaveImportResult([photo.FullPath]);
        }
    }

    /// <summary>
    /// 自动导入现有文件的图片
    /// </summary>
    [RelayCommand]
    public void AutoImportImages()
    {
        // 自动导入已经导入过的地址
        List<string> imagePaths = [];
        foreach (var item in DirectoryItems)
        {
            var result = ImageStorageService.LoadDirectoryImagePaths(item.Path);
            if (result.Any())
            {
                imagePaths.AddRange(result);
            }
        }

        // TODO:对于android设备，自动导入屏幕截图和相册
#if ANDROID
        var cameraDir = "/storage/emulated/0/DCIM/Camera";
        var screenshotsDir1 = "/storage/emulated/0/Pictures/Screenshots";
        var screenshotsDir2 = "/storage/emulated/0/DCIM/Screenshots";

        List<string> autoImportDirs = [cameraDir, screenshotsDir1, screenshotsDir2];

        foreach (var dir in autoImportDirs)
        {
            if (Directory.Exists(dir))
            {
                var paths = ImageStorageService.LoadDirectoryImagePaths(dir);
                if (paths.Any())
                {
                    imagePaths.AddRange(paths);
                }
            }
        }
#endif

        if (imagePaths.Count > 0)
        {
            SaveImportResult([.. imagePaths]);
        }

        
    }

    #endregion

    #region 辅助函数

    /// <summary>
    /// 初始化ViewModel状态
    /// </summary>
    public void Init()
    {
        LoadImages();
    }

    /// <summary>
    /// 加载已导入图片并组织为目录
    /// </summary>
    private void LoadImages()
    {
        DirectoryItems = imageStorageService.DirectoryItems;
    }

    /// <summary>
    /// 保存导入结果
    /// </summary>
    /// <returns>数据库新增数量</returns>
    private void SaveImportResult(IEnumerable<string> imagePaths)
    {
        bool hasChange = false;
        foreach (var path in imagePaths)
        {
            if (imageStorageService.TrySaveImage(path, out var imageItem) || imageItem == null)
            {
                hasChange = true;
            }
        }

        if (hasChange)
        {
            DirectoryItems = imageStorageService.DirectoryItems;
        }
    }

    #endregion
}
