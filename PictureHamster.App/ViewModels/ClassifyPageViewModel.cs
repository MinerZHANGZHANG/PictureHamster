using Azure;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiteDB;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using PictureHamster.App.Services;
using PictureHamster.App.Views;
using PictureHamster.Share.Enums;
using PictureHamster.Share.Models;
using System.ComponentModel;
using System.Text;
using UraniumUI.Dialogs;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace PictureHamster.App.ViewModels;

// 禁用SKEXP0070警告(Ollama 聊天完成连接器目前是实验性的)
#pragma warning disable SKEXP0070

public partial class ClassifyPageViewModel(ImageStorageService imageStorageService, PreferencesService preferencesService, IDialogService dialogService, ILiteDatabase database) : ObservableObject, IViewModel
{
    #region 字段和属性

    /// <summary>
    /// AI服务接口类型
    /// </summary>
    public List<AIApiType> ApiServiceTypes { get; set; } =
    [
        AIApiType.Ollama,
        AIApiType.OpenAI,
    ];

    /// <summary>
    /// 当前选中的AI服务接口类型
    /// </summary>
    public AIApiType ApiServiceType
    {
        get => _apiServiceType;
        set
        {
            SetProperty(ref _apiServiceType, value);
        }
    }
    private AIApiType _apiServiceType = AIApiType.None;

    /// <summary>
    /// AI服务接口地址
    /// </summary>
    public string ApiServiceUrl
    {
        get => _apiServiceUrl;
        set => SetProperty(ref _apiServiceUrl, value);
    }
    private string _apiServiceUrl = "http://localhost:11434/v1/chat/completions";

    /// <summary>
    /// AI服务接口密钥
    /// </summary>
    public string ApiServiceKey
    {
        get => _apiServiceKey;
        set => SetProperty(ref _apiServiceKey, value);
    }
    private string _apiServiceKey = string.Empty;

    /// <summary>
    /// 模型名称
    /// </summary>
    public string ModelId
    {
        get => _modelId;
        set
        {
            SetProperty(ref _modelId, value);
            OnPropertyChanged(nameof(CurrentModelSetting));
        }
    }
    private string _modelId = string.Empty;

    /// <summary>
    /// 模型设置列表
    /// </summary>
    public List<ModelSetting> ModelSettings { get; set; } = [];

    /// <summary>
    /// 当前选中的模型设置
    /// </summary>
    public ModelSetting CurrentModelSetting => ModelSettings.FirstOrDefault(x => x.ModelId == ModelId)
        ?? new ModelSetting()
        {
            ModelId = ModelId,
        };

    /// <summary>
    /// 当前导入的图片，通过文件夹分组的列表
    /// </summary>
    public List<DirectoryItem> DirectoryItems
    {
        get => _directoryItems;
        set => SetProperty(ref _directoryItems, value);
    }
    private List<DirectoryItem> _directoryItems = [];

    /// <summary>
    /// 分类进度
    /// </summary>
    public float ClassifyProgress
    {
        get => _classifyProgress;
        set => SetProperty(ref _classifyProgress, value);
    }
    private float _classifyProgress = 0f;

    /// <summary>
    /// 是否正在分类
    /// </summary>
    public bool IsClassifying
    {
        get => _isClassifying;
        set => SetProperty(ref _isClassifying, value);
    }
    private bool _isClassifying = false;

    /// <summary>
    /// 是否结束分类请求
    /// </summary>
    private bool _isRequestEndClassify = false;

    /// <summary>
    /// 是否跳过已经分类的图片
    /// </summary>
    public bool IsSkipClassifiedImage
    {
        get => _isSkipClassifiedImage;
        set => SetProperty(ref _isSkipClassifiedImage, value);
    }
    private bool _isSkipClassifiedImage = true;

    /// <summary>
    /// 是否跳过手动分类的图片
    /// </summary>
    public bool IsSkipClassifiedImageByHand
    {
        get => _isSkipClassifiedImageByHand;
        set => SetProperty(ref _isSkipClassifiedImageByHand, value);
    }
    private bool _isSkipClassifiedImageByHand = true;

    #endregion

    /// <summary>
    /// 初始化
    /// </summary>
    public void Init()
    {
        // 加载图片信息
        DirectoryItems = imageStorageService.DirectoryItems;

        // 加载接口信息
        ApiServiceKey = preferencesService.AIServiceSetting.ApiKey;
        ApiServiceType = preferencesService.AIServiceSetting.ApiType;
        ApiServiceUrl = preferencesService.AIServiceSetting.ApiUrl;
        ModelId = preferencesService.AIServiceSetting.ModelId;

        // 加载模型设置
        ModelSettings = [.. database.GetCollection<ModelSetting>()
            .FindAll()
            .OrderByDescending(x => x.ModelId)];

        PropertyChanged += ClassifyPageViewModel_PropertyChanged;
    }

    #region 辅助方法

    /// <summary>
    /// 属性变化时的处理方法
    /// </summary>
    private void ClassifyPageViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // 监听AI服务设置的变化
        if (e.PropertyName == nameof(ApiServiceType)
            || e.PropertyName == nameof(ApiServiceUrl)
            || e.PropertyName == nameof(ModelId)
            || e.PropertyName == nameof(ApiServiceKey))
        {
            // 更新AI服务设置
            preferencesService.UpdateAISettings(new()
            {
                ApiKey = ApiServiceKey,
                ApiType = ApiServiceType,
                ApiUrl = ApiServiceUrl,
                ModelId = ModelId
            });
        }
    }

    /// <summary>
    /// 尝试创建一个语义内核和提示词执行设置
    /// </summary>
    /// <param name="uri">AI服务地址</param>
    /// <param name="promptExecutionSettings">提示词执行设置</param>
    /// <param name="kernel">语义内核</param>
    /// <param name="errorMessage">错误信息</param>
    /// <returns>是否创建成功</returns>
    private bool TryBuildKernelAndSettings(Uri uri, out PromptExecutionSettings? promptExecutionSettings, out Kernel? kernel, out string errorMessage)
    {
        errorMessage = string.Empty;
        kernel = null;
        promptExecutionSettings = null;

        // 根据模型设置创建语义内核和对应的提示词执行设置
        IKernelBuilder kernelBuilder = Kernel.CreateBuilder();
        switch (ApiServiceType)
        {
            case AIApiType.Ollama:
                kernelBuilder.AddOllamaChatCompletion(
                    modelId: ModelId,
                    endpoint: uri
                    );
                promptExecutionSettings = new OllamaPromptExecutionSettings()
                {
                    Temperature = CurrentModelSetting.Temperature,
                    TopP = CurrentModelSetting.TopP,
                    TopK = CurrentModelSetting.TopK,
                    Stop = ["}"],
                };
                break;
            case AIApiType.OpenAI:
                kernelBuilder.AddOpenAIChatCompletion(
                    modelId: ModelId,
                    apiKey: ApiServiceKey,
                    httpClient: new HttpClient()
                    {
                        BaseAddress = uri
                    });
                promptExecutionSettings = new OpenAIPromptExecutionSettings()
                {
                    Temperature = CurrentModelSetting.Temperature,
                    TopP = CurrentModelSetting.TopP,
                    StopSequences = ["}"],
                };
                break;
            case AIApiType.None:
                errorMessage = "未配置AI接口类型";
                return false;
            default:
                errorMessage = "暂不支持的AI接口类型";
                return false;
        }

        kernel = kernelBuilder.Build();
        return true;
    }

    #endregion

    #region Command

    /// <summary>
    /// 对选中的图片进行分类
    /// </summary>
    [RelayCommand]
    public async Task ClassifyImages()
    {
        // 筛选选中的图片
        var selectedImages = DirectoryItems
            .Where(dir => dir.IsSelected)
            .SelectMany(dir => dir.ImageItems)
            .Where(img => img.Categories.Count == 0 || !IsSkipClassifiedImage)
            .Where(img => img.CategorySource != CategorySource.User || !IsSkipClassifiedImageByHand);

        if (!selectedImages.Any())
        {
            await dialogService.DisplayTextPromptAsync("没有选中的图片", "请先选择需要分类的图片。");
            return;
        }

        if (!Uri.TryCreate(ApiServiceUrl, UriKind.Absolute, out var apiServiceUri))
        {
            await dialogService.DisplayTextPromptAsync("不正确的分类服务地址", $"AI对话接口地址[{ApiServiceUrl}]不是一个有效的Url，请检查配置");
            return;
        }

        if (!TryBuildKernelAndSettings(apiServiceUri, out var promptExecutionSettings, out var kernel, out var errorMessage)
            || kernel == null 
            || promptExecutionSettings == null)
        {
            await dialogService.DisplayTextPromptAsync("无法创建语义内核", errorMessage);
            return;
        }

        ClassifyProgress = 0;
        IsClassifying = true;

        // 调用AI服务进行分类
        StringBuilder classifyFailMessageBuilder = new();
        foreach (var imageItem in selectedImages)
        {
            // 允许终止分类操作
            if (_isRequestEndClassify)
            {
                _isRequestEndClassify = false;
                IsClassifying = false;
                await dialogService.DisplayTextPromptAsync("操作已取消", "您已取消了分类操作。");
                return;
            }

            if (!File.Exists(imageItem.Path) || !Uri.TryCreate(imageItem.Path, UriKind.Absolute, out var imageUri))
            {
                classifyFailMessageBuilder.AppendLine($"图片文件[{imageItem.Path}]不存在或不是有效的Url，跳过该图片。");
                continue;
            }

            string mimeType = "image/png";
            switch (imageItem.Path)
            {
                case string path when path.EndsWith("jpeg", StringComparison.OrdinalIgnoreCase):
                    mimeType = "image/jpeg";
                    break;
                case string path when path.EndsWith("jpg", StringComparison.OrdinalIgnoreCase):
                    mimeType = "image/jpeg";
                    break;
                case string path when path.EndsWith("gif", StringComparison.OrdinalIgnoreCase):
                    mimeType = "image/gif";
                    break;
                case string path when path.EndsWith("bmp", StringComparison.OrdinalIgnoreCase):
                    mimeType = "image/bmp";
                    break;
                case string path when path.EndsWith("webp", StringComparison.OrdinalIgnoreCase):
                    mimeType = "image/webp";
                    break;
                default:
                    break;
            }

            // 调用AI服务进行分类
            ChatHistory chatHistory = [];
            chatHistory.AddSystemMessage(CurrentModelSetting.Prompt);
            chatHistory.AddUserMessage([new ImageContent(await File.ReadAllBytesAsync(imageItem.Path), mimeType)]);

            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
            try
            {
                var response = await chatCompletionService.GetChatMessageContentAsync(
                    chatHistory: chatHistory,
                    kernel: kernel,
                    executionSettings: promptExecutionSettings
                    );
                if (string.IsNullOrEmpty(response.Content))
                {
                    classifyFailMessageBuilder.AppendLine($"图片文件[{imageItem.Path}]未能从{apiServiceUri}获取有效的响应");
                    continue;
                }

                ImageClassifyResult? classifyResult = JsonSerializer.Deserialize<ImageClassifyResult>(response.Content + "}");
                if (classifyResult == null)
                {
                    classifyFailMessageBuilder.AppendLine($"图片文件[{imageItem.Path}]的返回值 {response.Content + "}"} 未能被解析");
                    continue;
                }

                IEnumerable<string> oldCategories = imageItem.Categories.ToArray();

                imageItem.CategorySource = CategorySource.AI;
                imageItem.Categories = classifyResult.类别;
                imageItem.KeyWords = classifyResult.关键信息;
                imageItem.Description = classifyResult.描述;
                imageItem.ClassifiedTime = DateTime.Now;

                // 更新图片分类结果
                imageStorageService.UpdateImageCategories(oldCategories, imageItem);
                ClassifyProgress += 1f / selectedImages.Count();
            }
            catch (Exception ex)
            {
                classifyFailMessageBuilder.AppendLine($"图片文件[{imageItem.Path}]在获取AI接口分类结果过程中发生错误{ex.Message}");
                continue;
            }
        }

        IsClassifying = false;
    }

    /// <summary>
    /// 结束分类请求
    /// </summary>
    [RelayCommand]
    public void CancelClassify()
    {
        _isRequestEndClassify = true;
    }

    /// <summary>
    /// 移动到分类结果页面
    /// </summary>
    [RelayCommand]
    public async Task ToClassificaionResultPage()
    {
        await Shell.Current.GoToAsync($"//{nameof(CategoryPage)}");
    }

    /// <summary>
    /// 打开模型设置对话框
    /// </summary>
    [RelayCommand]
    public async Task OpenModelSettingDialog()
    {
        var modelSetting = CurrentModelSetting;
        var result = await dialogService.DisplayFormViewAsync("模型设置", modelSetting);
        if (result != null)
        {
            var oldSetting = ModelSettings.FirstOrDefault(x => x.ModelId == modelSetting.ModelId);
            if (oldSetting != null)
            {
                oldSetting = oldSetting with
                {
                    ModelId = result.ModelId,
                    Prompt = result.Prompt,
                    Temperature = result.Temperature,
                    TopP = result.TopP,
                    TopK = result.TopK,
                };

                database.GetCollection<ModelSetting>().Update(oldSetting);
            }
            else
            {
                ModelSettings.Add(modelSetting);
                database.GetCollection<ModelSetting>().Insert(modelSetting);
            }
        }
    }

    #endregion
}

public class ImageClassifyResult
{
    /// <summary>
    /// 图片的分类结果
    /// </summary>
    public List<string> 类别 { get; set; } = [];

    /// <summary>
    /// 图片的关键信息
    /// </summary>
    public List<string> 关键信息 { get; set; } = [];

    /// <summary>
    /// 图片内容描述
    /// </summary>
    public string 描述 { get; set; } = string.Empty;
}
