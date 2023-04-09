using System.Text.Json;
using avaness.StatsServer.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace avaness.StatsServer
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Server
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); })
                .ConfigureLogging(builder => builder.AddJsonConsole(options =>
                {
                    options.IncludeScopes = false;
                    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";
                    options.JsonWriterOptions = new JsonWriterOptions
                    {
#if DEBUG
                        Indented = true
#else
                        Indented = false
#endif
                    };
                }))
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IStatsDatabase, StatsDatabase>();
                    services.AddHostedService<PersistenceService>();
                    services.AddMvc().AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);
                });
    }
}