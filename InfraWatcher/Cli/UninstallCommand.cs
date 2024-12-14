using Spectre.Console.Cli;
using SystemDServiceInstaller = InfraWatcher.ServiceInstaller.SystemDServiceInstaller;

namespace InfraWatcher.Cli;

public class UninstallCommand : SystemdCommandBase
{
    private readonly SystemDServiceInstaller.Factory _installerFactory;

    public UninstallCommand(SystemDServiceInstaller.Factory installerFactory)
    {
        _installerFactory = installerFactory;
    }

    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var installer = _installerFactory(GetServiceMetadata());

        try
        {
            await installer.DeleteSystemDService();
        }
        catch (Exception e)
        {
            throw new InvalidOperationException("Error registering systemd service. Maybe sudo is missing?\n" + e);
        }

        return 0;
    }
}