﻿using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace COLID.SearchService.WebApi
{
    public partial class Startup
    {
        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>The service collection</param>
        public void ConfigureLocalServices(IServiceCollection services)
        {
            ConfigureServices(services);
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app">The application builder</param>
        public void ConfigureLocal(IApplicationBuilder app)
        {
            app.UseDeveloperExceptionPage();
            Configure(app);
        }
    }
}
