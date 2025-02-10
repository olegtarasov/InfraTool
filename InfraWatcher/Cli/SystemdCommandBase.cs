using System.Diagnostics;
using InfraWatcher.Configuration;
using InfraWatcher.Helpers;
using InfraWatcher.ServiceInstaller;
using Spectre.Console.Cli;

namespace InfraWatcher.Cli;

public abstract class SystemdCommandBase : AsyncCommand
{
    protected ServiceMetadataSystemd GetServiceMetadata()
    {
        var config = WatcherConfig.Load();
        string? fileName = Process.GetCurrentProcess().MainModule?.FileName;
        if (fileName.IsNullOrEmpty())
        {
            throw new InvalidOperationException("Error: could not get path to the executable");
        }

        return new()
               {
                   FileName = fileName + " serve",
                   WorkingDirectory = AppContext.BaseDirectory,
                   EnvironmentVariables = new[]
                                          {
                                              "ASPNETCORE_ENVIRONMENT=Production",
                                              $"ASPNETCORE_URLS=http://*:{config.Server.Port}"
                                          },
                   Name = "infrawatcher",
                   DisplayName = "infrawatcher",
                   Start = StartType.Auto
               };
    }
}