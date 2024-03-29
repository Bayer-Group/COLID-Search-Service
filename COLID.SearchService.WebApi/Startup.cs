using System.IO.Compression;
using System.Net.Http;
using COLID.Exception;
using COLID.Identity;
using COLID.MessageQueue;
using COLID.SearchService.Repositories;
using COLID.SearchService.Services;
using COLID.StatisticsLog;
using COLID.Swagger;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace COLID.SearchService.WebApi
{
    public partial class Startup
    {
        /// <summary>
        /// Create the object of <see cref="Startup"/>.
        /// </summary>
        /// <param name="configuration">The object of <see cref="IConfiguration"/>.</param>
        /// <param name="env">The object of <see cref="IHostingEnvironment"/>.</param>
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
        }

        /// <summary>
        /// Gets the <see cref="IConfiguration"/> object.
        /// </summary>
        public IConfiguration Configuration { get; private set; }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> object.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<BrotliCompressionProviderOptions>(o => { o.Level = CompressionLevel.Fastest; });

            services.AddResponseCompression(options =>
            {
                options.Providers.Add<BrotliCompressionProvider>();
            });
            
            services.AddCors();
            services.AddControllers().AddNewtonsoftJson();
            services.AddHttpContextAccessor();
            services.AddHttpClient("NoProxy").ConfigurePrimaryHttpMessageHandler(() =>
            {
                return new HttpClientHandler
                {
                    UseProxy = false,
                    Proxy = null
                };
            });
            services.AddHealthChecks();

            services.AddIdentityModule(Configuration);
            services.AddColidSwaggerGeneration(Configuration);
            services.AddSingleton(Configuration);
            services.AddMessageQueueModule(Configuration);
            services.AddStatisticsLogModule(Configuration);
            services.AddServicesModule(Configuration);
            services.AddRepositoriesModule(Configuration);
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> object.</param>
        public void Configure(IApplicationBuilder app)
        {
            app.UseResponseCompression();            
            app.UseExceptionMiddleware();
            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseCors(
                options => options.SetIsOriginAllowed(x => _ = true)
                .AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod()
            );

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health");
                endpoints.MapControllers();
            });

            app.UseColidSwaggerUI(Configuration);

            app.UseMessageQueueModule(Configuration);
        }
    }
}
