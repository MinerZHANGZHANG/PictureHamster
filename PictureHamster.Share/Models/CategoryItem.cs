namespace PictureHamster.Share.Models;

/// <summary>
/// 类别信息
/// </summary>
public class CategoryItem
{
    public required string Name { get; set; }
    public List<ImageItem> ImageItems { get; set; } = [];

    public int ImageCount => ImageItems.Count;

    /// <summary>
    /// 最新一幅被分类的图片
    /// </summary>
    public ImageItem LatestImage => ImageItems
        .OrderByDescending(x => x.ClassifiedTime)
        .FirstOrDefault()
        ?? new ImageItem
        {
            Name = "类别下不包含图片",
            Path = string.Empty,
            DirectoryPath = string.Empty,
            ClassifiedTime = DateTime.MinValue,
        };
}

