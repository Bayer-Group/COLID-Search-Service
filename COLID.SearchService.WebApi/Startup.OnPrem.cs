using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace COLID.SearchService.WebApi
{
    public partial class Startup
    {
        public void ConfigureOnPremServices(IServiceCollection services)
        {
            ConfigureServices(services);
        }

        public void ConfigureOnPrem(IApplicationBuilder app)
        {
            // Following problem still exists with .NET Core 3.1 (latest test on 2020-07-22):
            // Accessing TLS secured APIs with the new SocketsHttpHandler introduced in .NET Core 2.1 results into exception "No such host is known".
            // Setting the flag "System.Net.Http.UseSocketsHttpHandler" to false deactivates the usage of the new SocketsHttpHandler and activates the older HttpClientHandler class instead.
            // See https://docs.microsoft.com/en-us/dotnet/api/system.net.http.socketshttphandler?view=netcore-3.1#remarks
            AppContext.SetSwitch("System.Net.Http.UseSocketsHttpHandler", false);

            Configure(app);
        }
    }
}
