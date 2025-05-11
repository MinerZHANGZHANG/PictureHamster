using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PictureHamster.Share.Models;

/// <summary>
/// 模型设置
/// </summary>
public record ModelSetting
{
    public required string ModelId { get; set; }
    public float Temperature { get; set; } = 0.8f;
    public float TopP { get; set; } = 0.9f;
    public int TopK { get; set; } = 40;
    public string Prompt { get; set; } = "请仔细分析这幅图像，图像可能是各种类型，" +
        "包括但不限于风景、截图、人物照片、物品图片、图表等，" +
        "以 Json 格式返回包含类别、关键信息、描述的内容。" +
        "其中类别是对图像所属类型的宽泛概括，" +
        "关键信息是图像中突出的元素或特征，" +
        "描述则是对图像整体视觉呈现的说明。" +
        "例如：\r\n{\r\n\"类别\": [\"类别A\"],\r\n" +
        "\"关键信息\": [\"物品A\"]、[\"环境B\"]、[\"物品C\"],\r\n" +
        "\"描述\": \"图像XXXXX包含XXXX显示XXX等。\"\r\n}";
}
