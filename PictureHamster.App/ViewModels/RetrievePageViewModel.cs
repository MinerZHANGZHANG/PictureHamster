using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PictureHamster.App.Services;
using PictureHamster.Share.Models;
using System.Collections.ObjectModel;
using UraniumUI.Dialogs;

namespace PictureHamster.App.ViewModels;

public partial class RetrievePageViewModel(ImageStorageService imageStorageService, IDialogService dialogService) : ObservableObject, IViewModel
{
    #region 属性和字段

    /// <summary>
    /// 查询文本
    /// </summary>
    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }
    private string _searchText = string.Empty;

    /// <summary>
    /// 搜索结果列表
    /// </summary>
    public ObservableCollection<ImageItem> SearchResults
    {
        get => _searchResults;
        set => SetProperty(ref _searchResults, value);
    }
    private ObservableCollection<ImageItem> _searchResults = [];

    #endregion

    public void Init()
    {}

    #region Command

    /// <summary>
    /// 搜索符合条件的图片
    /// </summary>
    [RelayCommand]
    public void SearchImages()
    {
        if (string.IsNullOrEmpty(SearchText))
        {
            return;
        }
        SearchResults = [.. imageStorageService.FindImageItems(_searchText)];
    }

    /// <summary>
    /// 查看大图
    /// </summary>
    /// <param name="path">图片地址</param>
    [RelayCommand]
    public async Task LookupImage(string path)
    {
        if(string.IsNullOrEmpty(path))
        {
            return;
        }

        var view = new ScrollView
        {
            Content = new Image
            {
                Source = ImageSource.FromFile(path),
                Aspect = Aspect.AspectFit,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center
            }
        };

        await dialogService.DisplayViewAsync("查看大图", view);
    }

    /// <summary>
    /// 分享图片
    /// </summary>
    /// <param name="path">图片地址</param>
    [RelayCommand]
    public async Task ShareImage(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        await Microsoft.Maui.ApplicationModel.DataTransfer.Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "分享图片",
            File = new ShareFile(path)
        });
    }

    #endregion
}
