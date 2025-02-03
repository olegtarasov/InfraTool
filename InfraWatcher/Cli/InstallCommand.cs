using Spectre.Console.Cli;
using SystemDServiceInstaller = InfraWatcher.ServiceInstaller.SystemDServiceInstaller;

namespace InfraWatcher.Cli;

public class InstallCommand : SystemdCommandBase
{
    private readonly ILoggerFactory _loggerFactory;
    //private readonly ILogger<InstallCommand> _logger;
    
    public InstallCommand(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
//        _logger = _loggerFactory.CreateLogger<InstallCommand>();
    }

    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var installer = new SystemDServiceInstaller(GetServiceMetadata(), _loggerFactory.CreateLogger<SystemDServiceInstaller>());

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