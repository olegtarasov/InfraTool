using InfraTool.Configuration;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Spectre.Console.Cli;

namespace InfraTool.Cli;

public class ServeCommand : AsyncCommand
{
    private readonly ComparisonEngine _comparisonEngine;

    public ServeCommand(ComparisonEngine comparisonEngine)
    {
        _comparisonEngine = comparisonEngine;
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
                    throw new BadHttpRequestException($"Group not found: {comparisonName}");
                }
                return await _comparisonEngine.Execute(group);
            });            
        }

        await app.RunAsync($"http://localhost:{config.Server.Port}");
        
        return 0;
    }
}