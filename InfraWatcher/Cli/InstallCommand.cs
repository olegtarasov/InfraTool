using System.Diagnostics;
using Common.Contracts.Helpers;
using Common.Contrib.ServiceInstaller;
using Spectre.Console.Cli;

namespace InfraWatcher.Cli;

public class InstallCommand : SystemdCommandBase
{
    private readonly SystemDServiceInstaller.Factory _installerFactory;

    public InstallCommand(SystemDServiceInstaller.Factory installerFactory)
    {
        _installerFactory = installerFactory;
    }

    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var installer = _installerFactory(GetServiceMetadata());

        try
        {
            await installer.RegisterSystemDService();
        }
        catch (Exception e)
        {
            throw new InvalidOperationException("Error registering systemd service. Maybe sudo is missing?\n" + e);
        }

        return 0;
    }
}