using System.Text.Json;
using System.Text.Json.Serialization;
using InfraWatcher.Configuration;
using Spectre.Console.Cli;

namespace InfraWatcher.Cli;

public class RunSettings : CommandSettings
{
    [CommandArgument(0, "<GROUP>")]
    public string Group { get; set; } = string.Empty;
}


public class RunCommand : AsyncCommand<RunSettings>
{
    private readonly Watcher _watcher;

    public RunCommand(Watcher watcher)
    {
        _watcher = watcher;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, RunSettings settings)
    {
        var config = WatcherConfig.Load();
        var group = config.Groups.FirstOrDefault(x => x.Name == settings.Group);
        if (group == null)
        {
            throw new ArgumentException($"Group {settings.Group} not found in config");
        }
        var result = await _watcher.Execute(group);
        var options = new JsonSerializerOptions
                      {
                          PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                      };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
        Console.WriteLine(JsonSerializer.Serialize(result, options));

        return 0;
    }
}