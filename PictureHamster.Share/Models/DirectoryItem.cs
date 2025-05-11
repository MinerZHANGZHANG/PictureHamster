using PictureHamster.Share.Enums;

namespace PictureHamster.Share.Models;

public record DirectoryItem
{
    public bool IsSelected { get; set; } = false;
    public required string Path { get; set; }

    /// <summary>
    /// �ļ����°�����ͼƬ�б�
    /// </summary>
    public List<ImageItem> ImageItems { get; set; } = [];

    /// <summary>
    /// �ļ�������
    /// </summary>
    public string Name => Path.Split(System.IO.Path.DirectorySeparatorChar).Last();

    /// <summary>
    /// ��ȡ�ļ��������µ�һ��ͼƬ
    /// </summary>
    public ImageItem LatestPicture => ImageItems
                    .OrderByDescending(x => x.LastModifyTime)
                    .FirstOrDefault()
                    ?? new ImageItem
                    {
                        Name = "�ļ��в�����ͼƬ",
                        Path = string.Empty,
                        DirectoryPath = string.Empty,
                        LastModifyTime = DateTime.MinValue
                    };

    /// <summary>
    /// ��ʾͼƬ�����ı�
    /// </summary>
    public string ImageCountText => ImageItems.Count() >= 1000
        ? "999+"
        : ImageItems.Count().ToString();

    /// <summary>
    /// ��ʾ�ѷ���ͼƬռ���ı�
    /// </summary>
    public string AlreadyClassifiedImageCountText => $"{ImageItems.Count(x => x.CategorySource != CategorySource.Unknown)}/{ImageItems.Count()}";
}
