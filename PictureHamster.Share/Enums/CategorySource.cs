using System.ComponentModel;

namespace PictureHamster.Share.Enums;

/// <summary>
/// 标记图片的分类来源
/// </summary>
public enum CategorySource
{
    [Description("未知")]
    Unknown,
    [Description("用户")]
    User,
    [Description("LLM模型")]
    AI,
}
