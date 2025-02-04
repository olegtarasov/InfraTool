using InfraWatcher.Cli;
using InfraWatcher.Helpers;
using InfraWatcher.ServiceInstaller;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Sinks.SystemConsole.Themes;
using Spectre.Console.Cli;

namespace InfraWatcher;

internal class Program
{
    public static int Main(string[] args)
    {
        var registrations = new ServiceCollection();
        RegisterServices(registrations);
        var registrar = new TypeRegistrar(registrations);
        var app = new CommandApp(registrar);
        app.Configure(config =>
        {
            config.AddCommand<RunCommand>("run");
            config.AddCommand<ServeCommand>("serve");
            config.AddCommand<InstallCommand>("install");
            config.AddCommand<UninstallCommand>("uninstall");
            config.AddCommand<UpdateCommand>("update");
            config.AddCommand<VersionCommand>("version");
            config.SetExceptionHandler((e, _) =>
            {
                Log.ForContext<Program>().Error(e, "Unhandled exception");
            });
        });
        return app.Run(args);
    }

    internal static void RegisterServices(IServiceCollection services)
    {
        services.AddLogging();
        services.AddSerilog(ConfigureLogger);

        services.AddTransient<SystemDServiceInstaller>();
        services.AddTransient<Watcher>();
    }

    internal static void ConfigureLogger(LoggerConfiguration config)
    {
        var level = LogEventLevel.Debug;
        string? logLevel = Environment.GetEnvironmentVariable("LOGLEVEL");
        if (!string.IsNullOrEmpty(logLevel))
        {
            if (!Enum.TryParse(logLevel, true, out level))
                level = LogEventLevel.Debug;
        }

        // config.MinimumLevel.Is(level)
        //     .MinimumLevel.Override("Microsoft", LogEventLevel.Warning);

        config.Enrich.FromLogContext()
            .Enrich.WithExceptionDetails();

        config.WriteTo.Console(
            theme: ConsoleTheme.None,
            outputTemplate: LoggingConfigurationExtensions.ConsoleTemplate,
            standardErrorFromLevel: LogEventLevel.Warning);
    }
}