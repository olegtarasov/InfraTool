using Spectre.Console.Cli;
using ServiceInstaller_SystemDServiceInstaller = InfraTool.ServiceInstaller.SystemDServiceInstaller;
using SystemDServiceInstaller = InfraTool.ServiceInstaller.SystemDServiceInstaller;

namespace InfraTool.Cli;

public class UninstallCommand : SystemdCommandBase
{
    private readonly ILoggerFactory _loggerFactory;
    
    public UninstallCommand(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var installer = new ServiceInstaller_SystemDServiceInstaller(GetServiceMetadata(), _loggerFactory.CreateLogger<ServiceInstaller_SystemDServiceInstaller>());

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