using LiteDB;
using PictureHamster.Share.Enums;
using PictureHamster.Share.Models;

namespace PictureHamster.App.Services;

/// <summary>
/// 管理图片信息存储
/// </summary>
public class ImageStorageService
{
    public ILiteDatabase Database { get; init; }

    public List<ImageItem> ImageItems { get; init; }

    public HashSet<string> ImagePaths { get; init; }

    public List<DirectoryItem> DirectoryItems { get; init; }

    public Dictionary<string, DirectoryItem> DirectoryPathDict { get; init; }

    public List<CategoryItem> CategoryItems { get; init; }

    public Dictionary<string, CategoryItem> CategoryNameDict { get; init; }

    /// <summary>
    /// 支持的图片后缀名
    /// </summary>
    public static HashSet<string> SupportsImageExtensions { get; } = [".png", ".jpg", ".jpeg", ".gif", ".bmp"];

    public ImageStorageService(ILiteDatabase liteDatabase)
    {
        Database = liteDatabase;

        ImageItems = [.. liteDatabase.GetCollection<ImageItem>()
            .FindAll()
            .OrderByDescending(i => i.LastModifyTime)];

        ImagePaths = [.. ImageItems.Select(i => i.Path)];

        DirectoryItems = [.. ImageItems
            .GroupBy(i => i.DirectoryPath)
            .Select(g => new DirectoryItem
            {
                Path = g.Key,
                ImageItems = [.. g]
            })];

        DirectoryPathDict = DirectoryItems
            .ToDictionary(DirectoryItems => DirectoryItems.Path, DirectoryItems => DirectoryItems);

        CategoryNameDict = [];
        CategoryItems = [];
        foreach (var imageItem in ImageItems)
        {
            if (imageItem.Categories.Count == 0)
            {
                continue;
            }

            foreach (var categoryName in imageItem.Categories)
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

    public bool UpdateImageKeywords(ImageItem oldImageItem, IEnumerable<string> newKeyWords, bool isUserOperation = false)
    {
        oldImageItem.CategorySource = isUserOperation ? CategorySource.User : CategorySource.AI;
        oldImageItem.KeyWords = [.. newKeyWords];
        Database.GetCollection<ImageItem>().Update(oldImageItem);
        return true;
    }

    public bool UpdateImageDescription(ImageItem oldImageItem, string newDescription, bool isUserOperation = false)
    {
        oldImageItem.CategorySource = isUserOperation ? CategorySource.User : CategorySource.AI;
        oldImageItem.Description = newDescription;
        Database.GetCollection<ImageItem>().Update(oldImageItem);
        return true;
    }

    public bool UpdateImageCategories(ImageItem oldImageItem, IEnumerable<string> newCategories, bool isUserOperation = false)
    {
        oldImageItem.CategorySource = isUserOperation ? CategorySource.User : CategorySource.AI;

        // 移除旧的分类中的图片
        foreach (var oldCategory in oldImageItem.Categories)
        {
            if (CategoryNameDict.TryGetValue(oldCategory, out var categoryItem))
            {
                categoryItem.ImageItems.Remove(oldImageItem);
            }
        }
        // 添加图片到分类
        oldImageItem.Categories = [.. newCategories];
        foreach (var categoryName in newCategories)
        {
            if (string.IsNullOrEmpty(categoryName))
            {
                continue;
            }
            // 如果分类已经存在，则添加图片到分类
            if (CategoryNameDict.TryGetValue(categoryName, out var categoryItem))
            {
                categoryItem.ImageItems.Add(oldImageItem);
            }
            else
            {
                categoryItem = new CategoryItem
                {
                    Name = categoryName,
                    ImageItems = [oldImageItem]
                };
                CategoryItems.Add(categoryItem);
                CategoryNameDict.Add(categoryName, categoryItem);
            }
        }
        Database.GetCollection<ImageItem>().Update(oldImageItem);
        return true;
    }

    public bool UpdateImageCategories(IEnumerable<string> oldCategories, ImageItem newImageItem, bool isUserOperation = false)
    {
        newImageItem.CategorySource = isUserOperation ? CategorySource.User : CategorySource.AI;

        // 移除旧的分类中的图片
        foreach (var oldCategory in oldCategories)
        {
            if (CategoryNameDict.TryGetValue(oldCategory, out var categoryItem))
            {
                categoryItem.ImageItems.Remove(newImageItem);
            }
        }

        // 添加图片到分类
        foreach (var categoryName in newImageItem.Categories)
        {
            if (string.IsNullOrEmpty(categoryName))
            {
                continue;
            }

            // 如果分类已经存在，则添加图片到分类
            if (CategoryNameDict.TryGetValue(categoryName, out var categoryItem))
            {
                categoryItem.ImageItems.Add(newImageItem);
            }
            else
            {
                categoryItem = new CategoryItem
                {
                    Name = categoryName,
                    ImageItems = [newImageItem]
                };
                CategoryItems.Add(categoryItem);
                CategoryNameDict.Add(categoryName, categoryItem);
            }
        }

        Database.GetCollection<ImageItem>().Update(newImageItem);

        return true;
    }

    public IEnumerable<ImageItem> FindImageItems(string queryText)
    {
        if(string.IsNullOrEmpty(queryText))
        {
            return [];
        }
        queryText = queryText.Trim();
        var result= Database.GetCollection<ImageItem>().Find(i =>
            i.Categories.Any(c => c.Contains(queryText))
            || i.KeyWords.Any(k => k.Contains(queryText))
            || i.Description.Contains(queryText))
            ;

        return result;
    }


    public IEnumerable<ImageItem> GetImageItemsByCategory(string categoryName)
    {
        if (string.IsNullOrEmpty(categoryName) || !CategoryNameDict.TryGetValue(categoryName, out var categoryItem))
        {
            return [];
        }
        return categoryItem.ImageItems;
    }
}
