using System.Diagnostics;
using InfraTool.Configuration;
using InfraTool.ServiceInstaller;
using InfraTool.Helpers;
using Spectre.Console.Cli;

namespace InfraTool.Cli;

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
                   Name = "infratool",
                   DisplayName = "infratool",
                   Start = StartType.Auto
               };
    }
}