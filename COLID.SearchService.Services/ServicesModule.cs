using COLID.MessageQueue.Services;
using COLID.SearchService.Services.Implementation;
using COLID.SearchService.Services.Interface;
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

            services.AddTransient<IRemoteSimilarityService, RemoteSimilarityService>();
            services.AddTransient<ISearchService, Implementation.SearchService>();
            //services.AddSingleton<Implementation.SearchService>();
            //services.AddSingleton<ISearchService>(x => x.GetRequiredService<Implementation.SearchService>());
            services.AddTransient<IRemoteCarrot2Service, RemoteCarrot2Service>();
            services.AddSingleton<DocumentService>();
            services.AddSingleton<IDocumentService>(x => x.GetRequiredService<DocumentService>());
            services.AddSingleton<IMessageQueueReceiver>(x => x.GetRequiredService<DocumentService>());

            services.AddTransient<IndexService>();
            services.AddTransient<IIndexService>(x => x.GetRequiredService<IndexService>());
            services.AddTransient<IMessageQueueReceiver> (x => x.GetRequiredService<IndexService>());

            services.AddTransient<IUserService, UserService>();

            return services;
        }
    }
}
