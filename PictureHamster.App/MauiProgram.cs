﻿using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using UraniumUI;
using UraniumUI.Dialogs;
using LiteDB;
using PictureHamster.App.ViewModels;
using PictureHamster.App.Utils;
using PictureHamster.App.Services;
using UraniumUI.Options;

namespace PictureHamster.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseUraniumUI()
            .UseUraniumUIMaterial()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFontAwesomeIconFonts();
            })
            .ConfigureEssentials(essentials =>
            {
                essentials.UseVersionTracking();
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif
        builder.Services.AddCommunityToolkitDialogs();
        builder.Services.AddSingleton<IDialogService, DefaultDialogService>();
        LiteDBHelper.InitMapper();
        builder.Services.AddSingleton<ILiteDatabase>(new LiteDatabase(Path.Combine(FileSystem.AppDataDirectory, "pictureHamster.db")));
        builder.Services.AddSingleton<ImageStorageService>();
        builder.Services.AddSingleton<PreferencesService>();
        builder.Services.Configure<AutoFormViewOptions>(options =>
        {
            options.EditorMapping[typeof(string)] = (property, propertyNameFactory, source) =>
            {
                var editor = new Editor()
                {
                    Placeholder = propertyNameFactory(property),
                };
                editor.SetBinding(Editor.TextProperty, new Binding(property.Name, source: source));
                return editor;
            };
        });

        builder.Services.AddViewModels();
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            File.AppendAllText(Path.Combine(FileSystem.CacheDirectory, "log.txt"), $"{DateTime.Now}: {args.ExceptionObject}");
#if DEBUG
            var debug = File.ReadAllText(Path.Combine(FileSystem.CacheDirectory, "log.txt"));
#endif
        };
        
        return builder.Build();
    }
}
