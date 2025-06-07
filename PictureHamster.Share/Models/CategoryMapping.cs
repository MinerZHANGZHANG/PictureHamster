namespace PictureHamster.Share.Models;

/// <summary>
/// 类别映射
/// </summary>
public class CategoryMapping
{
    /// <summary>
    /// 主类别名称
    /// </summary>
    public required string Name { get; set; } = string.Empty;

    /// <summary>
    /// 子类别名称列表
    /// </summary>
    public List<string> MappedCategories { get; set; } = [];
}
