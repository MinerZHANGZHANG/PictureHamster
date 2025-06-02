using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiteDB;
using Microsoft.Maui.ApplicationModel;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using PictureHamster.App.Services;
using PictureHamster.App.Views;
using PictureHamster.Share.Enums;
using PictureHamster.Share.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Metadata;
using SixLabors.ImageSharp.Processing;
using UraniumUI.Dialogs;
using Image = SixLabors.ImageSharp.Image;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace PictureHamster.App.ViewModels;

// 禁用SKEXP0070警告(Ollama 聊天完成连接器目前是实验性的)
#pragma warning disable SKEXP0070

public partial class ClassifyPageViewModel(ImageStorageService imageStorageService, PreferencesService preferencesService, IDialogService dialogService, ILiteDatabase database) : ObservableObject, IViewModel
{
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
    public ModelSetting CurrentModelSetting => ModelSettings.FirstOrDefault(x => x.ModelId == ModelId) ?? new ModelSetting()
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

    private void ClassifyPageViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
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
    /// 对选中的图片进行分类
    /// </summary>
    [RelayCommand]
    public async Task ClassifyImages()
    {
        var selectedImages = DirectoryItems
            .Where(dir => dir.IsSelected)
            .SelectMany(dir => dir.ImageItems)
            .Where(img => img.Categories.Count == 0 || !IsSkipClassifiedImage)
            .Where(img => img.CategorySource != CategorySource.User || !IsSkipClassifiedImageByHand)
            ;
        if (!selectedImages.Any())
        {
            return;
        }

        if (!Uri.TryCreate(ApiServiceUrl, UriKind.Absolute, out var uri))
        {
            return;
        }

        IKernelBuilder kernelBuilder = Kernel.CreateBuilder();
        PromptExecutionSettings promptExecutionSettings = new();
        ModelSetting modelSetting = CurrentModelSetting;
        switch (ApiServiceType)
        {
            case AIApiType.Ollama:
                kernelBuilder.AddOllamaChatCompletion(
                    modelId: ModelId,
                    endpoint: uri
                    );
                promptExecutionSettings = new OllamaPromptExecutionSettings()
                {
                    Temperature = modelSetting.Temperature,
                    TopP = modelSetting.TopP,
                    TopK = modelSetting.TopK,
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
                    Temperature = modelSetting.Temperature,
                    TopP = modelSetting.TopP,
                    StopSequences = ["}"],
                };
                break;
            case AIApiType.None:
                return;
            default:
                return;
        }
        Kernel kernel = kernelBuilder.Build();

        int maxPixelCount = 1800000;
        int maxContextSize = 30000;
        ClassifyProgress = 0; 

        // 调用AI服务进行分类
        foreach (var imageItem in selectedImages)
        {
            if (!File.Exists(imageItem.Path) || !Uri.TryCreate(imageItem.Path, UriKind.Absolute, out var imageUri))
            {
                continue;
            }

            //// 读取图片文件
            //using Image image = await Image.LoadAsync(imageItem.Path);
            //int pixelCount = image.Width * image.Height;
            //if (pixelCount > maxPixelCount)
            //{
            //    // 计算缩放比例
            //    double scaleFactor = Math.Sqrt(1800000.0 / pixelCount);
            //    int newWidth = (int)(image.Width * scaleFactor);
            //    int newHeight = (int)(image.Height * scaleFactor);

            //    // 缩放图片
            //    image.Mutate(x => x.Resize(newWidth, newHeight));
            //}

            //var base64String = image.ToBase64String(PngFormat.Instance);
            //if (base64String.Length > maxContextSize)
            //{
            //    // 图片过大，跳过
            //    //continue;
            //}

            //string pngPath = Path.Combine(Path.GetTempPath(), $"{imageItem.Name}.png");
            //await image.SaveAsPngAsync(pngPath);

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
            chatHistory.AddSystemMessage(modelSetting.Prompt);
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
                    continue;
                }

                ImageClassifyResult? classifyResult = JsonSerializer.Deserialize<ImageClassifyResult>(response.Content + "}");
                if (classifyResult == null)
                {
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
                continue;
            }
        }
    }

    /// <summary>
    /// 移动到分类结果页面
    /// </summary>
    [RelayCommand]
    public async Task ToClassificaionResultPage()
    {
        // Navigate to the classification result page
        // This is a placeholder for the actual navigation logic
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
