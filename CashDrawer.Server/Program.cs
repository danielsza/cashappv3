using System;
using System.IO;
using System.Threading.Tasks;
using CashDrawer.Server.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace CashDrawer.Server
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=" + new string('=', 58));
            Console.WriteLine("Cash Drawer Server - C# Native Version");
            Console.WriteLine("Version 3.10.25");
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

        static IHostBuilder CreateHostBuilder(string[] args)
        {
            // Create error queue instance early (needed for Serilog sink)
            var errorQueue = new ErrorNotificationQueue(
                Microsoft.Extensions.Logging.Abstractions.NullLogger<ErrorNotificationQueue>.Instance);
            
            // Config file path - use ProgramData for writable location
            var programDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "CashDrawer");
            var configPath = Path.Combine(programDataPath, "appsettings.json");
            
            // Ensure directory exists
            if (!Directory.Exists(programDataPath))
                Directory.CreateDirectory(programDataPath);
            
            // If config doesn't exist in ProgramData, copy from app directory
            if (!File.Exists(configPath))
            {
                var appDirConfig = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
                if (File.Exists(appDirConfig))
                {
                    File.Copy(appDirConfig, configPath);
                    Console.WriteLine($"Copied default config to: {configPath}");
                }
            }
            
            Console.WriteLine($"Using config: {configPath}");
            
            return Host.CreateDefaultBuilder(args)
                .UseSerilog((context, services, configuration) =>
                {
                    // Log directory in ProgramData
                    var logDir = Path.Combine(programDataPath, "Logs");
                    
                    if (!Directory.Exists(logDir))
                        Directory.CreateDirectory(logDir);
                    
                    configuration
                        .MinimumLevel.Information()
                        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                        .WriteTo.Console()
                        .WriteTo.File(
                            Path.Combine(logDir, "Errors_.log"),  // Separate file for errors!
                            rollingInterval: RollingInterval.Day,
                            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} | {SourceContext} | {Level:u3} | {Message:lj}{NewLine}{Exception}",
                            restrictedToMinimumLevel: LogEventLevel.Warning  // Only write warnings and errors
                        )
                        .WriteTo.Sink(new ErrorNotificationSink(errorQueue), LogEventLevel.Warning); // Capture errors for notifications
                })
                .ConfigureAppConfiguration((context, config) =>
                {
                    config
                        .AddJsonFile(configPath, optional: false, reloadOnChange: true)
                        .AddEnvironmentVariables()
                        .AddCommandLine(args);
                })
                .ConfigureServices((context, services) =>
                {
                    // Configuration
                    services.Configure<Shared.Models.ServerConfig>(
                        context.Configuration.GetSection("Server"));
                    services.Configure<Shared.Models.SecurityConfig>(
                        context.Configuration.GetSection("Security"));

                    // Services (use the same errorQueue instance from above)
                    services.AddSingleton(errorQueue);
                    services.AddSingleton<UserService>();
                    services.AddSingleton<SerialPortService>();
                    services.AddSingleton<TransactionLogger>();
                    services.AddHostedService<TcpServerService>();
                    services.AddHostedService<DiscoveryService>();
                    services.AddHostedService<PeerSyncService>();
                })
                .UseWindowsService(); // Allow running as Windows Service
        }
    }
}
