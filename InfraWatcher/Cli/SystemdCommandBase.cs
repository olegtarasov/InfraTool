using System.Diagnostics;
using Common.Contracts.Helpers;
using Common.Contrib.ServiceInstaller;
using Spectre.Console.Cli;

namespace InfraWatcher.Cli;

public abstract class SystemdCommandBase : AsyncCommand
{
    protected ServiceMetadataSystemd GetServiceMetadata()
    {
        string? fileName = Process.GetCurrentProcess().MainModule?.FileName;
        if (fileName.IsNullOrEmpty())
        {
            throw new InvalidOperationException("Error: could not get path to the executable");
        }

        return new()
               {
                   FileName = fileName,
                   EnvironmentVariables = new[]
                                          {
                                              "ASPNETCORE_ENVIRONMENT=Production",
                                              "ASPNETCORE_URLS=http://*:5015"
                                          },
                   Name = "InfraWatcher",
                   DisplayName = "InfraWatcher",
                   Start = StartType.Auto
               };
    }
}