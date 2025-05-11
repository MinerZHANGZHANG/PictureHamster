using PictureHamster.Share.Enums;

namespace PictureHamster.Share.Models;

public class AIServiceSetting
{
    /// <summary>
    /// 接口类型
    /// </summary>
    public AIApiType ApiType { get; set; } = AIApiType.None;

    /// <summary>
    /// 接口地址
    /// </summary>
    public string ApiUrl { get; set; } = string.Empty;

    /// <summary>
    /// 接口密钥
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// 模型名称
    /// </summary>
    public string ModelId { get; set; } = string.Empty;
}
