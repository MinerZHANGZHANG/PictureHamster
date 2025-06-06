﻿using PictureHamster.Share.Enums;

namespace PictureHamster.Share.Models;

/// <summary>
/// 图片信息
/// </summary>
public class ImageItem
{
    public required string Name { get; set; }
    public required string Path { get; set; }
    public required string DirectoryPath { get; set; }
    public DateTime LastModifyTime { get; set; }
    public DateTime ClassifiedTime { get; set; }
    public bool IsSelected { get; set; } = false;

    /// <summary>
    /// 图片的原始分类，可能有多个，按照可能性从大到小排序
    /// </summary>
    public List<string> Categories { get; set; } = [];

    /// <summary>
    /// 图片的原始分类经过映射处理后的分类
    /// </summary>
    public List<string> MapppingCategories { get; set; } = [];

    public string CategoriesText => string.Join(",", MapppingCategories);

    /// <summary>
    /// 图片的关键信息
    /// </summary>
    public List<string> KeyWords { get; set; } = [];
    public string KeyWordsText => string.Join(",", KeyWords);

    /// <summary>
    /// 图片内容描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 图片类别来源
    /// </summary>
    public CategorySource CategorySource { get; set; } = CategorySource.Unknown;
}
