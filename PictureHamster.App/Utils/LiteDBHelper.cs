using LiteDB;
using PictureHamster.Share.Models;

namespace PictureHamster.App.Utils;

internal class LiteDBHelper
{
    public static string DatabasePath { get; set; } = "litedb.Database";

    public static LiteDatabase GetLiteDatabase()
    {
        return new LiteDatabase(DatabasePath);
    }

    public static void InitMapper()
    {
        var mapper = BsonMapper.Global;
        mapper.Entity<ImageItem>()
            .Id(x => x.Path)
            .Ignore(x=>x.IsSelected);
        mapper.Entity<ModelSetting>()
            .Id(x => x.ModelId);
    }
}
