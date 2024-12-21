using System.Text.Json;
using System.Text.Json.Serialization;
using Spectre.Console.Cli;

namespace InfraWatcher.Cli;

public class RunSettings : CommandSettings
{
    [CommandArgument(0, "<GROUP>")]
    public string Group { get; set; } = string.Empty;
}


public class RunCommand : AsyncCommand<RunSettings>
{
    private readonly GroupWatcher _groupWatcher;

    public RunCommand(GroupWatcher groupWatcher)
    {
        _groupWatcher = groupWatcher;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, RunSettings settings)
    {
        var config = WatcherConfig.Load();
        var group = config.Groups.FirstOrDefault(x => x.Name == settings.Group);
        if (group == null)
        {
            throw new ArgumentException($"Group {settings.Group} not found in config");
        }
        var result = await _groupWatcher.Execute(group);
        var options = new JsonSerializerOptions
                      {
                          PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                      };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
        Console.WriteLine(JsonSerializer.Serialize(result, options));

        return 0;
    }
}