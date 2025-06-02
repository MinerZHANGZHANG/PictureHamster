using PictureHamster.Share.Enums;
using PictureHamster.Share.Models;
using System.Text.Json;

namespace PictureHamster.App.Services;

/// <summary>
/// 用户首选项
/// </summary>
public class PreferencesService
{
    public AIServiceSetting AIServiceSetting { get; private set; }

    public PreferencesService()
    {
        string aiServiceSettingJson = string.Empty;
        aiServiceSettingJson = Preferences.Default.Get(nameof(Share.Models.AIServiceSetting), string.Empty);

        if (string.IsNullOrEmpty(aiServiceSettingJson))
        {
            AIServiceSetting = new();
        }
        else
        {
            AIServiceSetting = JsonSerializer.Deserialize<AIServiceSetting>(aiServiceSettingJson) ?? new();
        }
    }

    public void UpdateAISettings(AIServiceSetting aiServiceSetting)
    {
        string aiServiceSettingJson = JsonSerializer.Serialize(aiServiceSetting);
        Preferences.Default.Set(nameof(Share.Models.AIServiceSetting), aiServiceSettingJson);
    }
}
