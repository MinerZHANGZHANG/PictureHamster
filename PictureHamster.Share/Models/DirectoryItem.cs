using PictureHamster.Share.Enums;

namespace PictureHamster.Share.Models;

public record DirectoryItem
{
    public bool IsSelected { get; set; } = false;
    public required string Path { get; set; }

    /// <summary>
    /// 文件夹下包含的图片列表
    /// </summary>
    public List<ImageItem> ImageItems { get; set; } = [];

    /// <summary>
    /// 文件夹名称
    /// </summary>
    public string Name => Path.Split(System.IO.Path.DirectorySeparatorChar).Last();

    /// <summary>
    /// 获取文件夹下最新的一幅图片
    /// </summary>
    public ImageItem LatestPicture => ImageItems
                    .OrderByDescending(x => x.LastModifyTime)
                    .FirstOrDefault()
                    ?? new ImageItem
                    {
                        Name = "文件夹不包含图片",
                        Path = string.Empty,
                        DirectoryPath = string.Empty,
                        LastModifyTime = DateTime.MinValue
                    };

    /// <summary>
    /// 显示图片数量文本
    /// </summary>
    public string ImageCountText => ImageItems.Count() >= 1000
        ? "999+"
        : ImageItems.Count().ToString();

    /// <summary>
    /// 显示已分类图片占比文本
    /// </summary>
    public string AlreadyClassifiedImageCountText => $"{ImageItems.Count(x => x.CategorySource != CategorySource.Unknown)}/{ImageItems.Count()}";
}
