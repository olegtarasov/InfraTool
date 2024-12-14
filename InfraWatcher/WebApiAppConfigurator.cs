using System.Diagnostics;
using Common.Contracts.Helpers;
using Common.Host.Builder;
using Serilog;
using Serilog.Sinks.Grafana.Loki;

namespace InfraWatcher;

public class WebApiAppConfigurator : HostConfiguratorBase
{
    public override void ConfigureSerilogMain(
        LoggerConfiguration configuration,
        HostBuilderContext hostBuilderContext,
        IServiceProvider serviceProvider,
        IConfigurationContext context)
    {
        var config = WatcherConfig.Load();
        if (config.Server.Loki != null)
        {
            if (config.Server.Loki.Login.IsNullOrEmpty() || config.Server.Loki.Password.IsNullOrEmpty())
            {
                Logger.Warning("Loki login or password are not defined in config/secrets.yaml. Skipping Loki sink");
                return;
            }

            configuration.WriteTo.GrafanaLoki("https://loki.olegtarasov.me",
                new LokiLabel[]
                {
                    new() { Key = "job", Value = "InfraWatcher" },
                    new() { Key = "nodename", Value = "pretender-services" }
                },
                new[] { "level" },
                new LokiCredentials { Login = config.Server.Loki.Login, Password = config.Server.Loki.Password });
        }
    }

    public override void ConfigureHost(IHostBuilder hostBuilder, IConfigurationContext configurationContext)
    {
        hostBuilder.UseSystemd();
        hostBuilder.ConfigureAppConfiguration(x => x.AddInMemoryCollection(
            new Dictionary<string, string?> { { "LogAsJson", "False" } }));
    }

    public override void ConfigureWebHost(IWebHostBuilder hostBuilder, IConfigurationContext configurationContext)
    {
        var config = WatcherConfig.Load();
        
        hostBuilder.UseUrls($"http://*:{config.Server.Port}");
    }

    public override void ConfigureServices(
        HostBuilderContext context,
        IServiceCollection services,
        IConfigurationContext configurationContext)
    {
        services.AddSingleton<VariableHolder>();
    }
}