using Serilog;
using Spectre.Console.Cli;

namespace InfraWatcher.Cli;

public class ServeCommand : AsyncCommand
{
    private readonly VersionWatcher _versionWatcher;

    public ServeCommand(VersionWatcher versionWatcher)
    {
        _versionWatcher = versionWatcher;
    }

    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var config = WatcherConfig.Load();
        var builder = WebApplication.CreateBuilder();
        Program.RegisterServices(builder.Services);
        var app = builder.Build();

        app.MapGet("/api/versions", async () => await _versionWatcher.GetVersions());

        await app.RunAsync($"http://localhost:{config.Server.Port}");
        
        return 0;
    }
}