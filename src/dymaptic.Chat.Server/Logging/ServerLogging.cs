using Serilog;
using Serilog.AspNetCore;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Filters;
using System.Diagnostics;
using System.Reflection;
using System.Security.Claims;

namespace dymaptic.Chat.Server.Logging;

/// <summary>
///     Encapsulates the logic to create the root <see cref="Serilog.ILogger" /> instance
/// </summary>
/// <see cref="https://github.com/datalust/dotnet6-serilog-example">
///     />
///     <see cref="https://github.com/datalust/dotnet6-serilog-example/blob/dev/Program.cs" />
internal static class ServerLogging
{
    /// <summary>
    ///     The directory where Log files are stored
    /// </summary>
    internal static string LogsDirectory { get; private set; } = "";

    /// <summary>
    ///     Initial Bootstrap logging setup
    /// </summary>
    public static void Bootstrap()
    {
        LoggerConfiguration config = new LoggerConfiguration().WriteTo.Console();

#if DEBUG
        config.WriteTo.Debug();

        SelfLog.Enable(msg =>
        {
            Debug.WriteLine($"Serilog.SelfLog: {msg}");
        });
#endif

        // Set Serilog's static Log.Logger, used for later configuration and Program.cs
        Log.Logger = config.CreateBootstrapLogger();

        // Immediately add our unhandled exception catcher
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainOnUnhandledException;
    }

    public static void Dispose()
    {
        // Unattach our event handler
        AppDomain.CurrentDomain.UnhandledException -= OnCurrentDomainOnUnhandledException;

        // Flush + close all streams, dispose anything disposable
        Log.CloseAndFlush();

        // Disable SelfLog last (to catch errors with CloseAndFlush)
        SelfLog.Disable();
    }

    /// <summary>
    ///     Configures builder.Host to use Serilog as the backing for all ILogger operations
    /// </summary>
    public static void ConfigureHostUseSerilog(HostBuilderContext context,
        LoggerConfiguration loggerConfiguration)
    {
        IConfigurationSection? loggingConfig = context.Configuration
            .GetRequiredSection("Logging");

        // Enrichment
        loggerConfiguration.Enrich.FromLogContext();

        /* Log Event Levels
         * In appsettings.json -> Logging -> LogLevel,
         * additional LogEventLevel overrides can be added:
         * - "namespace": "LogLevel"   <- The minimum logging level for events generated in the namespace.
         */
        IConfigurationSection? levelConfig = loggingConfig.GetRequiredSection("LogLevel");

        // Minimum level
        string defaultLevel = levelConfig.GetValue<string>("Default");

        loggerConfiguration.MinimumLevel
            .Is(ParseLogEventLevel(defaultLevel, LogEventLevel.Warning));

        // Additional overrides
        foreach (IConfigurationSection? child in levelConfig.GetChildren())
        {
            if (child.Key == "Default") continue;

            loggerConfiguration.MinimumLevel
                .Override(child.Key, ParseLogEventLevel(child.Value, LogEventLevel.Warning));
        }

        string[]? excludedUrls = loggingConfig.GetSection("ExcludedUrlStrings").Get<string[]>();

        if (excludedUrls is not null && excludedUrls.Any())
        {
            loggerConfiguration.Filter.ByExcluding(Matching.WithProperty<string>("RequestPath", v =>
                excludedUrls.Any(u => v.Contains(u, StringComparison.OrdinalIgnoreCase))));
        }

        // Destinations

        // Get a cleaned up application name to use as the log file name
        string appName = Assembly.GetEntryAssembly()?.GetName().Name ?? "";
        int i = appName.LastIndexOf('.') + 1;

        if (i > 0)
        {
            appName = appName[i..];
        }

        string? configLogDirectory = loggingConfig.GetValue<string>("LoggingFolder", "Logs");
        LogsDirectory = configLogDirectory.CreateOrReturnFullyQualifiedPath();

        // A timestamp will be appended after the last underscore
        // due to the RollingInterval specified below.
        string xmlLogPath = Path.Combine(LogsDirectory, $"{appName}_Log_.xml");

        loggerConfiguration
            .WriteTo.Console()
#if DEBUG
            .WriteTo.Debug()
#endif

            // Write to a rolling XML log file
            .WriteTo.File(new XmlTextFormatter(),
                rollingInterval: RollingInterval.Day,
                path: xmlLogPath);
    }

    /// <summary>
    ///     Configures HTTP Request Logging
    /// </summary>
    /// <param name="options"></param>
    public static void RequestLogging(RequestLoggingOptions options)
    {
        // Extra details
        options.IncludeQueryInRequestPath = true;

        // Add our Identity Claims
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            if (httpContext.User.Identity is ClaimsIdentity claimsIdentity)
            {
                foreach (Claim claim in claimsIdentity.Claims)
                {
                    // Get and cleanup the type
                    string type = claim.Type;
                    int i = type.LastIndexOf('/') + 1;

                    if ((i > 0) && (i < type.Length))
                    {
                        type = type[i..];
                    }

                    // DiagnosticContext is automatically added to log events during the request
                    diagnosticContext.Set($"User_{type}", claim.Value);
                }
            }
        };
    }

    private static void OnCurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs args)
    {
        if (args.ExceptionObject is Exception ex)
        {
            Log.Logger.Fatal(ex, "AppDomain.CurrentDomain.UnhandledException({@Sender}, {IsTerminating})",
                sender, args.IsTerminating);
        }
        else
        {
            Log.Logger.Fatal("AppDomain.CurrentDomain.UnhandledException({@Sender}, {@Args})",
                sender, args);
        }
    }

    /// <summary>
    ///     Converts <paramref name="text" /> to a <see cref="LogEventLevel" />
    /// </summary>
    /// <param name="text">
    ///     The text to parse.
    ///     If it matches a <see cref="LogEventLevel" /> or <see cref="LogLevel" /> value,
    ///     it will be returned.
    ///     Otherwise, <paramref name="defaultLevel" /> will be.
    /// </param>
    /// <param name="defaultLevel">
    ///     The fallback <see cref="LogEventLevel" /> to return if the <paramref name="text" /> doesn't
    ///     parse to a valid value.
    /// </param>
    private static LogEventLevel ParseLogEventLevel(string text, LogEventLevel defaultLevel)
    {
        if (Enum.TryParse(text, true, out LogEventLevel logEventLevel))
        {
            return logEventLevel;
        }

        if (Enum.TryParse(text, true, out LogLevel logLevel))
        {
            return logLevel switch
            {
                LogLevel.Trace => LogEventLevel.Verbose,
                LogLevel.Debug => LogEventLevel.Debug,
                LogLevel.Information => LogEventLevel.Information,
                LogLevel.Warning => LogEventLevel.Warning,
                LogLevel.Error => LogEventLevel.Error,
                LogLevel.Critical => LogEventLevel.Fatal,
                LogLevel.None => LogEventLevel.Verbose,
                _ => defaultLevel
            };
        }

        return defaultLevel;
    }

    private static LogEventLevel ToLogEventLevel(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => LogEventLevel.Verbose,
            LogLevel.Debug => LogEventLevel.Debug,
            LogLevel.Information => LogEventLevel.Information,
            LogLevel.Warning => LogEventLevel.Warning,
            LogLevel.Error => LogEventLevel.Error,
            LogLevel.Critical => LogEventLevel.Fatal,
            _ => default(LogEventLevel)
        };
    }
}