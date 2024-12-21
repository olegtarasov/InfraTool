using System.Text.Json;
using System.Text.Json.Serialization;
using Spectre.Console.Cli;

namespace InfraWatcher.Cli;

public class VersionsCommand : AsyncCommand
{
    private readonly VersionWatcher _versionWatcher;

    public VersionsCommand(VersionWatcher versionWatcher)
    {
        _versionWatcher = versionWatcher;
    }

    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var result = await _versionWatcher.GetVersions();
        var options = new JsonSerializerOptions
                      {
                          PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                      };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
        Console.WriteLine(JsonSerializer.Serialize(result, options));

        return 0;
    }
}