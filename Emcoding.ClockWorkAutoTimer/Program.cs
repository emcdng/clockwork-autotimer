using Emcoding.ClockWorkAutoTimer;
using Emcoding.ClockWorkAutoTimer.Options;
using Emcoding.ClockWorkAutoTimer.Services;
using Emcoding.ClockWorkAutoTimer.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

class Program
{
    static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File($"logs/{Guid.NewGuid().ToString("N")}.log")
            .CreateLogger();

        try
        {
            Log.Information("ClockWork AutoTimer started...");
            
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .Build();
        
            var services = new ServiceCollection();

            services.Configure<ClockWorkAutoTimerOptions>(config.GetSection(nameof(ClockWorkAutoTimerOptions)));

            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog();
            });
            services.AddSingleton<ICsvService, CsvService>();
            services.AddSingleton<IClockworkService, ClockworkService>();
            services.AddSingleton<ITokenProvider, TokenProvider>();
            services.AddTransient<App>();
            
            var provider = services.BuildServiceProvider();

            // Run the app
            var app = provider.GetRequiredService<App>();
            await app.Run(args[0]);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ClockWork AutoTimer failed.");
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}

