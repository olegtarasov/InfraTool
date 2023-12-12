using System.Diagnostics;
using Common.Host.Builder;
using Serilog;
using Serilog.Sinks.Grafana.Loki;

namespace UpdateWatcher;

public class WebApiAppConfigurator : HostConfiguratorBase
{
    public override void ConfigureSerilogCommon(LoggerConfiguration configuration, IConfigurationContext context)
    {
        var secrets = WatcherConfig.LoadSecrets();
        if (!secrets.ContainsKey("loki_login") || !secrets.ContainsKey("loki_password"))
        {
            Logger.Warning("Loki login or password are not defined in config/secrets.yaml. Skipping Loki sink");
            return;
        }

        configuration.WriteTo.GrafanaLoki("https://loki.olegtarasov.me",
            new LokiLabel[]
            {
                new() { Key = "job", Value = "UpdateWatcher" },
                new() { Key = "nodename", Value = "pretender-services" }
            },
            new []{"level"},
            new LokiCredentials { Login = secrets["loki_login"], Password = secrets["loki_password"] });
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