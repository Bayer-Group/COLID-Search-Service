using COLID.SearchService.Services.Implementation;
using COLID.SearchService.Services.Interface;
using COLID.MessageQueue.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace COLID.SearchService.Services
{
    public static class ServicesModule
    {
        /// <summary>
        /// This will register all the supported functionality by Services module.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> object for registration.</param>
        /// <param name="configuration">The <see cref="IConfiguration"/> object for registration.</param>
        public static IServiceCollection AddServicesModule(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddTransient<IStatusService, StatusService>();

            services.AddSingleton<Implementation.SearchService>();
            services.AddSingleton<ISearchService>(x => x.GetRequiredService<Implementation.SearchService>());

            services.AddSingleton<DocumentService>();
            services.AddSingleton<IDocumentService>(x => x.GetRequiredService<DocumentService>());
            services.AddSingleton<IMessageQueueReceiver>(x => x.GetRequiredService<DocumentService>());

            services.AddTransient<IIndexService, IndexService>();
            return services;
        }
    }
}
