using InfraWatcher.Configuration;
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

        foreach (var group in config.Groups)
        {
            var local = group;
            app.MapGet($"/api/{local.Name}", async () => await _watcher.Execute(local));            
        }

        await app.RunAsync($"http://localhost:{config.Server.Port}");
        
        return 0;
    }
}