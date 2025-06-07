using LiteDB;
using PictureHamster.Share.Enums;
using PictureHamster.Share.Models;

namespace PictureHamster.App.Services;

/// <summary>
/// 管理图片信息存储
/// </summary>
public class ImageStorageService
{
    #region 属性和字段

    /// <summary>
    /// 数据库实例
    /// </summary>
    public ILiteDatabase Database { get; init; }

    /// <summary>
    /// 已导入的图片列表
    /// </summary>
    public List<ImageItem> ImageItems { get; init; }

    /// <summary>
    /// 已导入的图片路径
    /// </summary>
    public HashSet<string> ImagePaths { get; init; }

    /// <summary>
    /// 已导入过的目录列表
    /// </summary>
    public List<DirectoryItem> DirectoryItems { get; init; }

    /// <summary>
    /// 已导入过的目录路径字典
    /// </summary>
    public Dictionary<string, DirectoryItem> DirectoryPathDict { get; init; }

    /// <summary>
    /// 当前分类信息列表
    /// </summary>
    public List<CategoryItem> CategoryItems { get; private set; }

    /// <summary>
    /// 类别名称字典
    /// </summary>
    public Dictionary<string, CategoryItem> CategoryNameDict { get; private set; }

    /// <summary>
    /// 类别名称映射列表
    /// </summary>
    public List<CategoryMapping> CategoryMappings { get; private set; }

    /// <summary>
    /// 类别名称映射字典 Key: 类别名称, Value: 映射的主类别名称
    /// </summary>
    public Dictionary<string, string> CategoryMappingDict { get; private set; }

    /// <summary>
    /// 支持的图片后缀名
    /// </summary>
    public static HashSet<string> SupportsImageExtensions { get; } = [".png", ".jpg", ".jpeg", ".gif", ".bmp"];

    #endregion

    public ImageStorageService(ILiteDatabase liteDatabase)
    {
        Database = liteDatabase;

        // 加载类别映射
        CategoryMappings = LoadCategoryMappings(liteDatabase);
        CategoryMappingDict = RefreshCategoryMappingDict();

        // 加载所有图片信息
        ImageItems = LoadImages(liteDatabase);
        ImagePaths = [.. ImageItems.Select(i => i.Path)];

        // 从图片信息中加载目录信息
        DirectoryItems = LoadDirectoryItems();
        DirectoryPathDict = DirectoryItems
            .ToDictionary(DirectoryItems => DirectoryItems.Path, DirectoryItems => DirectoryItems);

        // 刷新图片的映射类别
        RefreshImageMappingCategories();

        // 从图片信息中加载分类信息
        CategoryNameDict = [];
        CategoryItems = [];
        RefreshCategoryItems();
    }

    #region 保存操作

    /// <summary>
    /// 尝试添加图片到导入列表，重复的地址不添加
    /// </summary>
    /// <param name="imagePath">图片完整路径</param>
    /// <param name="imageItem">返回的图片新对象</param>
    /// <returns>是否成功添加</returns>
    public bool TrySaveImage(string imagePath, out ImageItem? imageItem)
    {
        // 和现有数据重复时不添加
        if (ImagePaths.Contains(imagePath))
        {
            imageItem = null;
            return false;
        }

        imageItem = new ImageItem
        {
            Name = Path.GetFileName(imagePath),
            Path = imagePath,
            DirectoryPath = Path.GetDirectoryName(imagePath) ?? string.Empty,
            LastModifyTime = File.GetLastWriteTime(imagePath),
        };

        // 没有对应目录时，添加目录
        if (DirectoryPathDict.TryGetValue(imageItem.DirectoryPath, out var directoryItem))
        {
            directoryItem.ImageItems.Add(imageItem);
        }
        else
        {
            directoryItem = new DirectoryItem
            {
                Path = imageItem.DirectoryPath,
                ImageItems = [imageItem]
            };

            DirectoryItems.Add(directoryItem);
            DirectoryPathDict.Add(directoryItem.Path, directoryItem);
        }

        ImageItems.Add(imageItem);
        ImagePaths.Add(imagePath);
        Database.GetCollection<ImageItem>().Insert(imageItem);

        return true;
    }

    /// <summary>
    /// 更新图片的关键词
    /// </summary>
    /// <param name="imageItem">图片</param>
    /// <param name="keyWords">关键字</param>
    /// <param name="isUserOperation">是否是用户手动进行的操作</param>
    /// <returns>是否更新成功</returns>
    public bool UpdateImageKeywords(ImageItem imageItem, IEnumerable<string> keyWords, bool isUserOperation = false)
    {
        imageItem.CategorySource = isUserOperation ? CategorySource.User : CategorySource.AI;
        imageItem.KeyWords = [.. keyWords];

        return Database.GetCollection<ImageItem>().Update(imageItem);
    }

    /// <summary>
    /// 更新图片描述信息
    /// </summary>
    /// <param name="imageItem">图片</param>
    /// <param name="description">描述</param>
    /// <param name="isUserOperation">是否是用户手动进行的操作</param>
    /// <returns>是否更新成功</returns>
    public bool UpdateImageDescription(ImageItem imageItem, string description, bool isUserOperation = false)
    {
        imageItem.CategorySource = isUserOperation ? CategorySource.User : CategorySource.AI;
        imageItem.Description = description;

        return Database.GetCollection<ImageItem>().Update(imageItem);
    }

    /// <summary>
    /// 更新图片的分类信息
    /// </summary>
    /// <param name="oldImageItem">类别未更新的图片对象</param>
    /// <param name="newCategories">新的类别</param>
    /// <param name="isUserOperation">是否是用户进行的操作</param>
    /// <returns>是否更新成功</returns>
    public bool UpdateImageCategories(ImageItem oldImageItem, IEnumerable<string> newCategories, bool isUserOperation = false)
    {
        oldImageItem.CategorySource = isUserOperation ? CategorySource.User : CategorySource.AI;
        UpdateCategoryMappings(oldImageItem, oldImageItem.Categories, newCategories);
        oldImageItem.Categories = [.. newCategories];

        return Database.GetCollection<ImageItem>().Update(oldImageItem);
    }

    /// <summary>
    /// 更新图片的分类信息
    /// </summary>
    /// <param name="oldCategories">旧类别</param>
    /// <param name="newImageItem">已被赋值新类别的图片对象</param>
    /// <param name="isUserOperation">是否是用户操作</param>
    /// <returns></returns>
    public bool UpdateImageCategories(IEnumerable<string> oldCategories, ImageItem newImageItem, bool isUserOperation = false)
    {
        newImageItem.CategorySource = isUserOperation ? CategorySource.User : CategorySource.AI;
        UpdateCategoryMappings(newImageItem, oldCategories, newImageItem.Categories);

        return Database.GetCollection<ImageItem>().Update(newImageItem);
    }

    /// <summary>
    /// 更新图片的分类映射信息，并刷新相关的类别映射字典和图片类别信息
    /// </summary>
    public int UpdateImageCategoryMappings(IEnumerable<CategoryMapping> categoryMappings)
    {
        CategoryMappings = [.. categoryMappings];
        RefreshCategoryMappingDict();
        RefreshImageMappingCategories();
        RefreshImageCategories();
        return Database.GetCollection<CategoryMapping>().Upsert(CategoryMappings);
    }

    #endregion

    #region 查询操作

    /// <summary>
    /// 获取目录下的所有图片路径
    /// </summary>
    /// <param name="directoryPath">目录</param>
    public static IEnumerable<string> LoadDirectoryImagePaths(string directoryPath)
    {
        if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
        {
            return [];
        }

        return Directory.GetFiles(directoryPath)
            .Where(x => SupportsImageExtensions.Contains(Path.GetExtension(x)));
    }

    /// <summary>
    /// 根据类别/关键字/描述查找图片
    /// </summary>
    /// <param name="queryText">查询文本</param>
    /// <returns>查询到的图片</returns>
    public IEnumerable<ImageItem> FindImageItems(string queryText)
    {
        if (string.IsNullOrEmpty(queryText))
        {
            return [];
        }
        queryText = queryText.Trim();
        var result = Database.GetCollection<ImageItem>().Find(i =>
            i.Categories.Any(c => c.Contains(queryText))
            || i.KeyWords.Any(k => k.Contains(queryText))
            || i.Description.Contains(queryText))
            ;

        return result;
    }

    /// <summary>
    /// 根据类别名称获取图片列表
    /// </summary>
    /// <param name="categoryName">类别名称</param>
    /// <returns>图片</returns>
    public IEnumerable<ImageItem> GetImageItemsByCategory(string categoryName)
    {
        if (string.IsNullOrEmpty(categoryName) || !CategoryNameDict.TryGetValue(categoryName, out var categoryItem))
        {
            return [];
        }
        return categoryItem.ImageItems;
    }

    /// <summary>
    /// 按最新的映射刷新图片类别
    /// </summary>
    public void RefreshImageCategories()
    {
        CategoryNameDict = [];
        CategoryItems = [];
        RefreshCategoryItems();
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 刷新图片类别
    /// </summary>
    private void RefreshCategoryItems()
    {
        foreach (var imageItem in ImageItems)
        {
            if (imageItem.Categories.Count == 0)
            {
                continue;
            }

            foreach (var categoryName in imageItem.MapppingCategories)
            {
                if (string.IsNullOrEmpty(categoryName))
                {
                    continue;
                }

                if (CategoryNameDict.TryGetValue(categoryName, out var categoryItem))
                {
                    categoryItem.ImageItems.Add(imageItem);
                }
                else
                {
                    categoryItem = new CategoryItem
                    {
                        Name = categoryName,
                        ImageItems = [imageItem]
                    };

                    CategoryItems.Add(categoryItem);
                    CategoryNameDict.Add(categoryName, categoryItem);
                }
            }
        }
    }

    /// <summary>
    /// 加载类别映射信息
    /// </summary>
    private static List<CategoryMapping> LoadCategoryMappings(ILiteDatabase liteDatabase)
    {
        return [.. liteDatabase.GetCollection<CategoryMapping>()
            .FindAll()];
    }

    /// <summary>
    /// 从图片信息中加载目录信息
    /// </summary>
    private List<DirectoryItem> LoadDirectoryItems()
    {
        return [.. ImageItems
            .GroupBy(i => i.DirectoryPath)
            .Select(g => new DirectoryItem
            {
                Path = g.Key,
                ImageItems = [.. g]
            })];
    }

    /// <summary>
    /// 加载图片信息
    /// </summary>
    private List<ImageItem> LoadImages(ILiteDatabase liteDatabase)
    {
        return [.. liteDatabase.GetCollection<ImageItem>()
            .FindAll()
            .OrderByDescending(i => i.LastModifyTime)];
    }

    /// <summary>
    /// 刷新图片的映射类别
    /// </summary>
    private void RefreshImageMappingCategories()
    {
        foreach (var imageItem in ImageItems)
        {
            imageItem.MapppingCategories = [];
            foreach (var category in imageItem.Categories)
            {
                if (CategoryMappingDict.TryGetValue(category, out var mappingCategory))
                {
                    imageItem.MapppingCategories.Add(mappingCategory);
                }
                else
                {
                    imageItem.MapppingCategories.Add(category);
                }
            }
        }
    }

    /// <summary>
    /// 将图片对象从旧的分类映射到新的分类
    /// </summary>
    /// <param name="oldCategories">旧分类</param>
    /// <param name="newCategories">新分类</param>
    /// <param name="imageItem">图片对象</param>
    private void UpdateCategoryMappings(ImageItem imageItem, IEnumerable<string> oldCategories, IEnumerable<string> newCategories)
    {
        // 从旧分类移除
        foreach (var oldCategory in oldCategories)
        {
            if (CategoryNameDict.TryGetValue(oldCategory, out var categoryItem))
            {
                categoryItem.ImageItems.Remove(imageItem);
            }
        }

        // 添加到新分类
        foreach (var categoryName in newCategories)
        {
            if (string.IsNullOrEmpty(categoryName))
            {
                continue;
            }

            // 如果分类已经存在，则添加图片到分类
            if (CategoryNameDict.TryGetValue(categoryName, out var categoryItem))
            {
                categoryItem.ImageItems.Add(imageItem);
            }
            else
            {
                categoryItem = new CategoryItem
                {
                    Name = categoryName,
                    ImageItems = [imageItem]
                };
                CategoryItems.Add(categoryItem);
                CategoryNameDict.Add(categoryName, categoryItem);
            }
        }
    }

    /// <summary>
    /// 刷新类别映射字典
    /// </summary>
    /// <returns></returns>
    private Dictionary<string, string> RefreshCategoryMappingDict()
    {
        Dictionary<string, string> categoryMappingDict = [];
        foreach (var mapping in CategoryMappings)
        {
            foreach (var subCategory in mapping.MappedCategories)
            {
                categoryMappingDict.TryAdd(subCategory, mapping.Name);
            }
        }
        return categoryMappingDict;
    }

    #endregion
}
