using DPBlazorMapLibrary.JsInterops.Events;
using DPBlazorMapLibrary.JsInterops.LeftletDefaultIconFactories;
using DPBlazorMapLibrary.JsInterops.Maps;
using Microsoft.Extensions.DependencyInjection;

namespace DPBlazorMapLibrary
{
    public static class MapDependencyInjection
    {
        public static IServiceCollection AddMapService(this IServiceCollection services)
        {
            AddJsInterops(services);
            AddFactorys(services);
            return services;
        }
        
        private static void AddJsInterops(IServiceCollection services)
        {
            services.AddTransient<IMapJsInterop, MapJsInterop>();
            services.AddTransient<IEventedJsInterop, EventedJsInterop>();
            services.AddTransient<IIconFactoryJsInterop, IconFactoryJsInterop>();
        }
        private static void AddFactorys(IServiceCollection services)
        {
            services.AddTransient<LayerFactory>();
            services.AddTransient<IIconFactory, IconFactory>();
        }
    }
}
