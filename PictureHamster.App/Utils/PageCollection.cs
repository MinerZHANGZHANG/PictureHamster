using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections;

namespace PictureHamster.App.Utils;

/// <summary>
/// 分页集合类
/// </summary>
/// <typeparam name="T"></typeparam>
public class PageCollection<T> : ObservableObject, IEnumerable<T>
{
    public IEnumerable<T> AllItems { get; set; } = [];
    public int PageIndex { get; set; } = 0;
    public int PageSize { get; set; } = 1;

    public int PageCount => AllItems.Count() / PageSize + (AllItems.Count() % PageSize > 0 ? 1 : 0);
    public int TotalCount => AllItems.Count();

    public IEnumerable<T> CurrentPageItems => AllItems
        .Skip(PageIndex * PageSize)
        .Take(PageSize);

    public T? FirstPageItem => CurrentPageItems.FirstOrDefault();

    public PageCollection()
    {}

    /// <summary>
    /// 创建分页集合
    /// </summary>
    /// <param name="items"></param>
    /// <param name="pageIndex"></param>
    /// <param name="pageSize"></param>
    /// <exception cref="ArgumentException"></exception>
    public PageCollection(IEnumerable<T> items, int pageIndex, int pageSize)
    {
        if (pageSize <= 0)
        {
            throw new ArgumentException("PageSize must be greater than 0");
        }

        AllItems = items ?? [];
        PageIndex = pageIndex;
        PageSize = pageSize;

        OnPropertyChanged(nameof(CurrentPageItems));
        OnPropertyChanged(nameof(FirstPageItem));
    }

    public void NextPage()
    {
        if (PageIndex < PageCount - 1)
        {
            PageIndex++;
            OnPropertyChanged(nameof(CurrentPageItems));
            OnPropertyChanged(nameof(FirstPageItem));
        }
    }

    public void PreviousPage()
    {
        if (PageIndex > 0)
        {
            PageIndex--;
            OnPropertyChanged(nameof(CurrentPageItems));
            OnPropertyChanged(nameof(FirstPageItem));
        }
    }

    /// <summary>
    /// 设置当前页索引为指定项所在的页
    /// </summary>
    /// <param name="item">特定项</param>
    public void SetPageIndexByItem(T item)
    {
        var index = AllItems.ToList().IndexOf(item);
        if (index >= 0)
        {
            PageIndex = index / PageSize;
            OnPropertyChanged(nameof(CurrentPageItems));
            OnPropertyChanged(nameof(FirstPageItem));
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        return AllItems.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)AllItems).GetEnumerator();
    }
}
