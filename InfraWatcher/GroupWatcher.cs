using System.Collections.Concurrent;
using InfraWatcher.Shell;

namespace InfraWatcher;

public record Item(string Name, string Status, string? Actual, string? Expected);

public class GroupWatcher
{
    private const string Error = "error";
    
    private readonly ILogger<GroupWatcher> _logger;
    
    public GroupWatcher(ILogger<GroupWatcher> logger)
    {
        _logger = logger;
    }

    public async Task<Item[]> Execute(GroupConfig config)
    {
        var bag = new ConcurrentBag<Item>();
        
        await Parallel.ForEachAsync(config.Items, async (item, _) =>
        {
            if (item.Comparer == null && config.Comparer == null)
            {
                _logger.LogError("Comparer is not defined for item {Item}, and parent group also doesn't have a comparer defined", item.Name);
                bag.Add(new Item(item.Name, Error, null, null));
                return;
            }
            
            string? actual = null, expected = null;
            string? status = null;
            var variables = new Dictionary<string, string?>();
            
            if (item.Variables != null)
            {
                foreach (var varDef in item.Variables)
                {
                    var command = new ShellCommand(varDef.Value, variables);
                    var (output, result) = await command.RunAsync();
                    
                    if (result != 0)
                    {
                        _logger.LogError("Variable {Variable} command exited with result code {Code}", varDef.Key, result);
                        bag.Add(new Item(item.Name, Error, null, null));
                        return;
                    }

                    string? value = output.FirstOrDefault(x => x.Type == OutputType.Output)?.Text;
                    if (string.IsNullOrEmpty(value))
                    {
                        _logger.LogError("Variable {Variable} command didn't return a value", varDef.Key);
                        bag.Add(new Item(item.Name, Error, null, null));
                        return;
                    }

                    variables[varDef.Key] = value;
                }
            }

            try
            {
                actual = await GetValue(item.Actual, variables);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to get actual value for item '{Item}'", item.Name);
                status = Error;
            }

            try
            {
                expected = await GetValue(item.Expected, variables);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to get expected value for item '{Item}'", item.Name);
                status = Error;
            }

            if (status == null)
            {
                status = (item.Comparer
                          ?? config.Comparer
                          ?? throw new InvalidOperationException("Comparer is not set"))
                    .Compare(actual, expected);
            }

            bag.Add(new(item.Name, status, actual, expected));
        });

        return bag.ToArray();
    }
    
    /// <summary>
    /// Tries to parse a version from an array of lines that we get from retriever.
    /// </summary>
    /// <remarks>
    /// The parsing logic is as follows:
    /// 1. If there are processors defined, all lines are fed to processors in the order they are defined in config.
    /// 2. If a processor detects a match, it is returned regardless of whether it matches version format or not.
    /// 3. If processors don't find anything, lines are parsed one by one with <see cref="Version.TryParse(System.ReadOnlySpan{char},out System.Version?)"/>.
    ///    If there is a match, that line is returned.
    /// </remarks>
    private async Task<string?> GetValue(VersionConfig item, IDictionary<string, string?>? variables)
    {
        var lines = await item.Retriever.GetLines(variables);
        if (lines.Length == 0)
            return null;

        foreach (var processor in item.Processors)
        {
            if (processor.TryParseVersion(lines, out var version))
                return version;
        }

        // If processors didn't find anything, just try to parse lines as they are
        foreach (var line in lines)
        {
            if (Version.TryParse(line, out _))
                return line;
        }

        return null;
    }
}