using InfraTool.Configuration;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Spectre.Console.Cli;

namespace InfraTool.Cli;

public class ServeCommand : AsyncCommand
{
    private readonly ComparisonEngine _comparisonEngine;
    private readonly ScriptEngine _scriptEngine;

    public ServeCommand(ComparisonEngine comparisonEngine, ScriptEngine scriptEngine)
    {
        _comparisonEngine = comparisonEngine;
        _scriptEngine = scriptEngine;
    }

    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var config = WatcherConfig.Load();
        var builder = WebApplication.CreateBuilder();
        Program.RegisterServices(builder.Services);
        var app = builder.Build();

        foreach (var comparisonName in config.Comparisons.Select(x => x.Name).ToArray())
        {
            app.MapGet($"/api/{comparisonName}", async () =>
            {
                // We are reloading the config on every request so that we don't need to restart the
                // server after config is changed. If groups are added or deleted, server needs to be
                // restarted.
                var localConfig = WatcherConfig.Load();
                var group = localConfig.Comparisons.FirstOrDefault(x => x.Name == comparisonName);
                if (group == null)
                {
                    throw new BadHttpRequestException($"Comparison not found: {comparisonName}");
                }
                return await _comparisonEngine.Execute(group);
            });            
        }

        foreach (var scriptName in config.Scripts.Select(x => x.Name).ToArray())
        {
            app.MapGet($"/api/{scriptName}", async () =>
            {
                var localConfig = WatcherConfig.Load();
                var script = localConfig.Scripts.FirstOrDefault(x => x.Name == scriptName);
                if (script == null)
                {
                    throw new BadHttpRequestException($"Script not found: {scriptName}");
                }
                
                return await _scriptEngine.Execute(script);
            });
        }

        string host = string.IsNullOrEmpty(config.Server.Host) ? "localhost" : config.Server.Host;

        await app.RunAsync($"http://{host}:{config.Server.Port}");
        
        return 0;
    }
}