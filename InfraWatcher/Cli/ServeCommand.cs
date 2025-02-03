using InfraWatcher.Configuration;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Spectre.Console.Cli;

namespace InfraWatcher.Cli;

public class ServeCommand : AsyncCommand
{
    private readonly Watcher _watcher;

    public ServeCommand(Watcher watcher)
    {
        _watcher = watcher;
    }

    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var config = WatcherConfig.Load();
        var builder = WebApplication.CreateBuilder();
        Program.RegisterServices(builder.Services);
        var app = builder.Build();

        foreach (var groupName in config.Groups.Select(x => x.Name).ToArray())
        {
            app.MapGet($"/api/{groupName}", async () =>
            {
                // We are reloading the config on every request so that we don't need to restart the
                // server after config is changed. If groups are added or deleted, server needs to be
                // restarted.
                var localConfig = WatcherConfig.Load();
                var group = localConfig.Groups.FirstOrDefault(x => x.Name == groupName);
                if (group == null)
                {
                    throw new BadHttpRequestException($"Group not found: {groupName}");
                }
                return await _watcher.Execute(group);
            });            
        }

        await app.RunAsync($"http://localhost:{config.Server.Port}");
        
        return 0;
    }
}