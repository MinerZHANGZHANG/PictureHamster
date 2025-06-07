
namespace PictureHamster.App.ViewModels;

/// <summary>
/// 汇总所有ViewModel的管理类
/// </summary>
internal static class ViewModelManager
{
    public static IServiceCollection AddViewModels(this IServiceCollection services)
    {
        // 扫描程序集中的所有ViewModel类型并注册
        var viewModelTypes = typeof(IViewModel).Assembly.GetTypes()
           .Where(type => typeof(IViewModel).IsAssignableFrom(type) && !type.IsAbstract);

        foreach (var viewModelType in viewModelTypes)
        {
            services.AddTransient(viewModelType);
        }
        
        return services;
    }
}

/// <summary>
/// ViewModel的接口，目前仅用于方便反射获取
/// </summary>
interface IViewModel
{

}