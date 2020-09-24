using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace COLID.SearchService.WebApi
{
    /// <summary>
    /// Class containing Main method.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// The Main method.
        /// </summary>
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
