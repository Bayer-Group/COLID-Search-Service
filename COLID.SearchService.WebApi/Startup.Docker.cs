using Microsoft.Extensions.DependencyInjection;

namespace COLID.SearchService.WebApi
{
    public partial class Startup
    {
        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> object.</param>
        public void ConfigureDockerServices(IServiceCollection services)
        {
            ConfigureServices(services);
        }
    }
}
