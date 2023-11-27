using Common.Contrib.ServiceInstaller;
using Common.Host.Builder;

namespace UpdateWatcher;

public class CliAppConfigurator : HostConfiguratorBase
{
    public override void ConfigureServices(
        HostBuilderContext context,
        IServiceCollection services,
        IConfigurationContext configurationContext)
    {
        services.AddTransient<SystemDServiceInstaller>();
    }
}