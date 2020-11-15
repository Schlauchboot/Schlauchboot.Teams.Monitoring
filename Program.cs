using System;
using Serilog;
using System.IO;
using Serilog.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Schlauchboot.Teams.Monitoring
{
    public class Program
    {
        public static void Main(string[] args)
        {
            #region LogDirectorySetup

            if (!Directory.Exists($"{AppDomain.CurrentDomain.BaseDirectory}\\Logs"))
            {
                Directory.CreateDirectory($"{AppDomain.CurrentDomain.BaseDirectory}\\Logs");
            }
            if (!Directory.Exists($"{AppDomain.CurrentDomain.BaseDirectory}\\Reports"))
            {
                Directory.CreateDirectory($"{AppDomain.CurrentDomain.BaseDirectory}\\Reports");
            }

            #endregion

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) => Host.CreateDefaultBuilder(args)
            .UseSerilog((context, configuration) => configuration
                .Enrich.FromLogContext()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .WriteTo.File($"{AppDomain.CurrentDomain.BaseDirectory}\\Logs\\ServiceLog_{DateTime.Now.ToString("MM_dd_yyyy_HH_mm")}.txt")
            )
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<Worker>();
            })
            .UseWindowsService();
    }
}
