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

    private readonly bool _isSupportSecureStorage = true;

    public PreferencesService()
    {
        string aiServiceSettingJson = string.Empty;
        try
        {
            aiServiceSettingJson = SecureStorage.Default.GetAsync(nameof(Share.Models.AIServiceSetting)).Result ?? string.Empty;
        }
        catch (Exception)
        {
            _isSupportSecureStorage = false;
        }
        finally
        {
            // SecureStorage 不支持时，使用 Preferences 进行存储
            if (!_isSupportSecureStorage)
            {
                aiServiceSettingJson = Preferences.Default.Get(nameof(Share.Models.AIServiceSetting), string.Empty);
            }
        }

        if (string.IsNullOrEmpty(aiServiceSettingJson))
        {
            AIServiceSetting = new();
        }
        else
        {
            AIServiceSetting = JsonSerializer.Deserialize<AIServiceSetting>(aiServiceSettingJson) ?? new();
        }
    }

    public async Task UpdateAISettings(AIServiceSetting aiServiceSetting)
    {
        string aiServiceSettingJson = JsonSerializer.Serialize(aiServiceSetting);

        if (_isSupportSecureStorage)
        {
            await SecureStorage.Default.SetAsync(nameof(Share.Models.AIServiceSetting), aiServiceSettingJson);
        }
        else
        {
            Preferences.Default.Set(nameof(Share.Models.AIServiceSetting), aiServiceSettingJson);
        }
    }
}
