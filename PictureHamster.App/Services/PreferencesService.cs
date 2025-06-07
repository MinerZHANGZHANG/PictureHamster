using PictureHamster.Share.Models;
using System.Text.Json;

namespace PictureHamster.App.Services;

/// <summary>
/// 用户首选项
/// </summary>
public class PreferencesService
{
    /// <summary>
    /// AI服务设置
    /// </summary>
    public AIServiceSetting AIServiceSetting { get; private set; }

    public PreferencesService()
    {
        AIServiceSetting = LoadAIServiceSetting();
    }

    /// <summary>
    /// 从应用首选项存储中加载AI服务设置
    /// </summary>
    private AIServiceSetting LoadAIServiceSetting()
    {
        string aiServiceSettingJson = string.Empty;
        aiServiceSettingJson = Preferences.Default.Get(nameof(Share.Models.AIServiceSetting), string.Empty);

        if (string.IsNullOrEmpty(aiServiceSettingJson))
        {
            return new();
        }
        else
        {
            return JsonSerializer.Deserialize<AIServiceSetting>(aiServiceSettingJson) ?? new();
        }
    }

    /// <summary>
    /// 更新用户首选项中的 AI 服务设置
    /// </summary>
    /// <param name="aiServiceSetting">AI服务设置</param>
    public void UpdateAISettings(AIServiceSetting aiServiceSetting)
    {
        string aiServiceSettingJson = JsonSerializer.Serialize(aiServiceSetting);
        Preferences.Default.Set(nameof(Share.Models.AIServiceSetting), aiServiceSettingJson);
    }
}
