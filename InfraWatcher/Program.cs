using System.Diagnostics;
using Common.Contracts.Helpers;
using Common.Contrib.ServiceInstaller;
using Common.Host.Cli;
using Common.Host.Web;
using InfraWatcher.Cli;
using Serilog;
using Spectre.Console.Cli;

namespace InfraWatcher;

internal class Program
{
    public static int Main(string[] args)
    {
        return args.Length > 0 ? RunCommandLine(args) : RunWebApi(args);
    }

    private static int RunCommandLine(string[] args)
    {
        return CliApp.Create(args)
            .AddConfigurator(new CliAppConfigurator())
            .WithCliConfiguration(config =>
            {
                config.AddCommand<InstallCommand>("install");
                config.AddCommand<UninstallCommand>("uninstall");
                config.SetExceptionHandler((e, _) =>
                {
                    Log.ForContext<Program>().LogException(e);
                });
            })
            .RunWithExitCode();
    }

    private static int RunWebApi(string[] args)
    {
        return WebApp.Create(args)
            .AddConfigurator(new WebApiAppConfigurator())
            .RunWithExitCode();
    }
}