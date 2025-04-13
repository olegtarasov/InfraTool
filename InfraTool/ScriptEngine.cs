using InfraTool.Configuration;

namespace InfraTool;

public class ScriptEngine
{
    private readonly ILogger<ScriptEngine> _logger;

    public ScriptEngine(ILogger<ScriptEngine> logger)
    {
        _logger = logger;
    }

    public async Task<string[]> Execute(ScriptConfig config)
    {
        var lines = await config.Execute.GetLines(new Dictionary<string, string>());
        return lines;
    }
}