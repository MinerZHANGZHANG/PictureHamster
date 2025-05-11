using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PictureHamster.Share.Models;
using System.Collections.ObjectModel;

namespace PictureHamster.App.ViewModels;

partial class RetrievePageViewModel : ObservableObject, IViewModel
{
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

    /// <summary>
    /// 搜索符合条件的图片
    /// </summary>
    [RelayCommand]
    public void SearchImages()
    {

    }

    /// <summary>
    /// 查看大图
    /// </summary>
    /// <param name="path">图片地址</param>
    [RelayCommand]
    public void LookupImage(string path)
    {

    }

    /// <summary>
    /// 分享图片
    /// </summary>
    /// <param name="path">图片地址</param>
    [RelayCommand]
    public void ShareImage(string path)
    {

    }
}
