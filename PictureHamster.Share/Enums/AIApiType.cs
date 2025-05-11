
using System.ComponentModel;

namespace PictureHamster.Share.Enums;

public enum AIApiType
{
    [Description("无")]
    None = 0,
    [Description("Ollama")]
    Ollama,
    [Description("OpenAI")]
    OpenAI,
}
