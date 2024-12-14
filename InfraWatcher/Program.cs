using InfraWatcher.Cli;
using Serilog;
using Spectre.Console.Cli;
using SystemDServiceInstaller = InfraWatcher.ServiceInstaller.SystemDServiceInstaller;
using TypeRegistrar = InfraWatcher.Cli.TypeRegistrar;

namespace InfraWatcher;

internal class Program
{
    public static int Main(string[] args)
    {
        return args.Length > 0 ? RunCommandLine(args) : RunWebApi(args);
    }

    private static int RunCommandLine(string[] args)
    {
        var registrations = new ServiceCollection();
        RegisterServices(registrations);
        var registrar = new TypeRegistrar(registrations);
        var app = new CommandApp(registrar);
        app.Configure(config =>
        {
            config.AddCommand<InstallCommand>("install");
            config.AddCommand<UninstallCommand>("uninstall");
            config.SetExceptionHandler((e, _) =>
            {
                Log.ForContext<Program>().Error(e, "Unhandled exception");
            });
        });
        return app.Run(args);
    }

    private static int RunWebApi(string[] args)
    {
        // return WebApp.Create(args)
        //     .AddConfigurator(new WebApiAppConfigurator())
        //     .RunWithExitCode();
        return 0;
    }

    private static void RegisterServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddTransient<SystemDServiceInstaller>();
        serviceCollection.AddTransient<VersionWatcher>();
    }
}