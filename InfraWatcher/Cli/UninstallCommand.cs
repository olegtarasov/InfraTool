using Spectre.Console.Cli;
using SystemDServiceInstaller = InfraWatcher.ServiceInstaller.SystemDServiceInstaller;

namespace InfraWatcher.Cli;

public class UninstallCommand : SystemdCommandBase
{
    private readonly ILoggerFactory _loggerFactory;
    
    public UninstallCommand(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var installer = new SystemDServiceInstaller(GetServiceMetadata(), _loggerFactory.CreateLogger<SystemDServiceInstaller>());

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