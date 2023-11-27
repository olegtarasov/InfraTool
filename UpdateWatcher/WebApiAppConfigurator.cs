using Common.Host.Builder;

namespace UpdateWatcher;

public class WebApiAppConfigurator : HostConfiguratorBase
{
    public override void ConfigureHost(IHostBuilder hostBuilder, IConfigurationContext configurationContext)
    {
        hostBuilder.UseSystemd();
    }
}