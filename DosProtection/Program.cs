using DosProtection.DosProtection.API.Services;
using DosProtection.DosProtection.Core.Constants;
using DosProtection.DosProtection.Core.Events;
using DosProtection.DosProtection.Core.Interfaces;
using DosProtection.DosProtection.Infrastructure.Implementations;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;

internal class Program
{
    private static WebApplication app;
    private static IConfigurationRoot configuration;
    private static Logger logger;

    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Host.UseSerilog();

        // Build configuration.
        BuildConfiguration();

        // Validate the configuration file.
        ValidateConfiguration(configuration);

        // Configure Serilog.
        logger = ConfigureSerilog();

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(logger);
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Configure configuration and logging for dependency injection.
        builder.Services.AddSingleton<IConfiguration>(configuration);
        builder.Services.AddSerilog(logger);

        // Configure instances for dependency injection.
        builder.Services.AddMemoryCache();
        builder.Services.AddSingleton<IDosProtectionService, DosProtectionService>();
        builder.Services.AddTransient<IDosProtectionClient, DosProtectionClient>();
        builder.Services.AddSingleton<KeyPressEvent>();

        app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();
        app.MapControllers();

        // Instantiate local instance of the singleton KeyPressEvent
        var processEvent = app.Services.GetRequiredService<KeyPressEvent>();

        // Start a separate task to listen for the exit event
        Task.Run(() =>
        {
            processEvent.KeyPressReceived += HandleKeyPress;
        });

        await app.RunAsync();
    }

    /// <summary>
    /// Handles the HTTP request event serving as a key press mock and gracefully shuts down the application.
    /// </summary>
    private static void HandleKeyPress(object? sender, KeyPressEventArgs ea)
    {
        try
        {
            logger.Information($"[Program:HandleKeyPress] Received key: {ea.Key}");

            // Check if the received key is the exit key.
            if (string.Equals(ea.Key, configuration[ConfigConstants.EXIT_KEY], StringComparison.OrdinalIgnoreCase))
            {
                logger.Information("[Program:HandleKeyPress] Received exit key.");
                logger.Information("[Program:HandleKeyPress] Performing a controlled and graceful shutdown.");

                // Gracefully exit the application, providing running tasks a time-window to complete before exiting the program.
                var hostApplicationLifetime = app.Services.GetService<IHostApplicationLifetime>();
                hostApplicationLifetime?.StopApplication();
            }
            else
            {
                logger.Information("[Program:HandleKeyPress] The received key is not the exit key. Ignoring.");
            }
        }
        catch (Exception e)
        {
            logger.Error($"[Program:HandleKeyPress] An error occurred while handling the key signal: {e}");
        }
    }

    /// <summary>
    /// Validates the parsable variables in configuration.
    /// </summary>
    /// <exception cref="FormatException"></exception>
    static void ValidateConfiguration(IConfiguration configuration)
    {
        if (!int.TryParse(configuration[ConfigConstants.MAX_REQUESTS_PER_FRAME], out _))
        {
            throw new FormatException("[Program:ValidateConfiguration] MAX_REQUESTS_PER_FRAME value is not configured properly. Shutting down.");
        }

        if (!int.TryParse(configuration[ConfigConstants.TIME_FRAME_THRESHOLD], out _))
        {
            throw new FormatException("[Program:ValidateConfiguration] TIME_FRAME_THRESHOLD value is not configured properly. Shutting down.");
        }
    }

    private static void BuildConfiguration()
    {
        configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
    }

    private static Logger ConfigureSerilog()
    {
        return new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.File(path: configuration[ConfigConstants.LOG_PATH])
            .CreateLogger();
    }
}