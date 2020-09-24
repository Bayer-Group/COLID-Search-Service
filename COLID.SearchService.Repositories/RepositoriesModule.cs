using COLID.SearchService.DataModel.Configuration;
using COLID.SearchService.Repositories.Implementation;
using COLID.SearchService.Repositories.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace COLID.SearchService.Repositories
{
    public static class RepositoriesModule
    {
        /// <summary>
        /// This will register all the supported functionality by Repositories module.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> object for registration.</param>
        /// <param name="configuration">The <see cref="IConfiguration"/> object for registration.</param>
        public static IServiceCollection AddRepositoriesModule(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<ElasticSearchOptions>(configuration.GetSection(nameof(ElasticSearchOptions)));
            services.AddSingleton<IElasticSearchRepository, ElasticSearchRepository>();

            return services;
        }
    }
}
