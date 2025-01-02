using System.Collections.Concurrent;
using InfraWatcher.Comparers;
using InfraWatcher.Configuration;
using InfraWatcher.Shell;

namespace InfraWatcher;

public record Item(string Name, string Status, string? Actual, string? Expected);

public class Watcher
{
    private const string Error = "error";
    
    private readonly ILogger<Watcher> _logger;
    
    public Watcher(ILogger<Watcher> logger)
    {
        _logger = logger;
    }

    public async Task<Item[]> Execute(GroupConfig config)
    {
        var bag = new ConcurrentBag<Item>();
        
        await Parallel.ForEachAsync(config.Items, async (item, _) =>
        {
            if (!ValidateItemConfig(config, item))
            {
                bag.Add(new Item(item.Name, Error, null, null));
                return;
            }

            string? actual = null;
            string? expected = null;
            string? result = null;
            var variables = new Dictionary<string, string?>();
            
            if (item.Variables != null)
            {
                foreach (var varDef in item.Variables)
                {
                    var command = new ShellCommand(varDef.Value, variables);
                    var (output, varResult) = await command.RunAsync();
                    
                    if (varResult != 0)
                    {
                        _logger.LogError("Variable {Variable} command exited with result code {Code}", varDef.Key, varResult);
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
                var actualValues = await GetValues(item.Actual, variables);
                if (actualValues.Length != 1)
                {
                    throw new InvalidOperationException($"Got {actualValues.Length} actual values instead of one.");
                }

                actual = actualValues[0];
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to get actual value for item '{Item}'", item.Name);
                result = Error;
            }

            try
            {
                var expectedValues = await GetValues(item.Expected, variables);
                if (expectedValues.Length != 1)
                {
                    throw new InvalidOperationException($"Got {expectedValues.Length} expected values instead of one.");
                }

                expected = expectedValues[0];
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to get expected value for item '{Item}'", item.Name);
                result = Error;
            }

            if (result == null)
            {
                result = (item.Comparer
                          ?? config.Comparer
                          ?? throw new InvalidOperationException("Comparer is not set"))
                    .Compare(actual, expected);
            }

            bag.Add(new(item.Name, result, actual, expected));
        });

        return bag.ToArray();
    }

    private async Task<string[]> GetValues(VersionConfig item, IDictionary<string, string?>? variables)
    {
        var lines = await item.Retriever.GetLines(variables);

        if (item.Processors.Length == 0)
            return lines;

        foreach (var processor in item.Processors)
        {
            lines = processor.Process(lines);
        }

        return lines;
    }

    private bool ValidateItemConfig(GroupConfig config, ItemConfig item)
    {
        if (item.Actual?.Retriever == null)
        {
            _logger.LogError("Actual retriever is not configured for item {Item}", item.Name);
            return false;
        }
            
        if (item.Expected?.Retriever == null)
        {
            _logger.LogError("Expected retriever is not configured for item {Item}", item.Name);
            return false;
        }
            
        if (item.Comparer == null && config.Comparer == null)
        {
            _logger.LogError("Comparer is not defined for item {Item}, and parent group also doesn't have a comparer defined", item.Name);
            return false;
        }

        return true;
    }
}