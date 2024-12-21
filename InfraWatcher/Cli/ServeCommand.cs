using Serilog;
using Spectre.Console.Cli;

namespace InfraWatcher.Cli;

public class ServeCommand : AsyncCommand
{
    private readonly GroupWatcher _groupWatcher;

    public ServeCommand(GroupWatcher groupWatcher)
    {
        _groupWatcher = groupWatcher;
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
            app.MapGet($"/api/{local.Name}", async () => await _groupWatcher.Execute(local));            
        }

        await app.RunAsync($"http://localhost:{config.Server.Port}");
        
        return 0;
    }
}