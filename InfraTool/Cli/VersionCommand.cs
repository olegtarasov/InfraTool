using System.Reflection;
using Spectre.Console.Cli;

namespace InfraTool.Cli;

public class VersionCommand : AsyncCommand
{
    public override Task<int> ExecuteAsync(CommandContext context)
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version
            ?? throw new InvalidOperationException("Failed to get version");
        Console.WriteLine($"infratool v{version.Major}.{version.Minor}.{version.Build}");
        return Task.FromResult(0);
    }
}