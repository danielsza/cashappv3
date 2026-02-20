using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CashDrawer.ServerControl.Services;
using CashDrawer.ServerControl.Models;

namespace CashDrawer.ServerControl
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=" + new string('=', 58));
            Console.WriteLine("Cash Drawer - Server Control Service");
            Console.WriteLine("Lightweight service control helper");
            Console.WriteLine("Version 1.0");
            Console.WriteLine("=" + new string('=', 58));
            Console.WriteLine();

            try
            {
                var host = CreateHostBuilder(args).Build();
                await host.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error: {ex.Message}");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    config
                        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                        .AddEnvironmentVariables()
                        .AddCommandLine(args);
                })
                .ConfigureServices((context, services) =>
                {
                    services.Configure<ControlServiceConfig>(
                        context.Configuration.GetSection("ControlService"));
                    services.AddHostedService<ControlService>();
                    services.AddHostedService<ControlDiscoveryService>();
                })
                .ConfigureLogging((context, logging) =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.AddDebug();
                    logging.SetMinimumLevel(LogLevel.Information);
                })
                .UseWindowsService(); // Allow running as Windows Service
    }
}
