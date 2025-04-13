using System.Text.Json;
using System.Text.Json.Serialization;
using InfraTool.Configuration;
using Spectre.Console.Cli;

namespace InfraTool.Cli;

public class RunSettings : CommandSettings
{
    [CommandArgument(0, "<NAME>")]
    public string Name { get; set; } = string.Empty;
}


public class RunCommand : AsyncCommand<RunSettings>
{
    private readonly ComparisonEngine _comparisonEngine;
    private readonly ScriptEngine _scriptEngine;

    public RunCommand(ComparisonEngine comparisonEngine, ScriptEngine scriptEngine)
    {
        _comparisonEngine = comparisonEngine;
        _scriptEngine = scriptEngine;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, RunSettings settings)
    {
        var config = WatcherConfig.Load();
        var comparison = config.Comparisons.FirstOrDefault(x => x.Name == settings.Name);
        if (comparison == null)
        {
            var script = config.Scripts.FirstOrDefault(x => x.Name == settings.Name);
            if (script == null)
                throw new ArgumentException($"{settings.Name} not found in config");

            await RunScript(script);
        }
        else
        {
            await RunComparison(comparison);
        }
        
        return 0;
    }

    private async Task RunScript(ScriptConfig script)
    {
        var scriptResult = await _scriptEngine.Execute(script);
        foreach (string line in scriptResult)
        {
            Console.WriteLine(line);
        }
    }

    private async Task RunComparison(ComparisonConfig comparison)
    {
        var result = await _comparisonEngine.Execute(comparison);
        var options = new JsonSerializerOptions
                      {
                          PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                      };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
        Console.WriteLine(JsonSerializer.Serialize(result, options));
    }
}