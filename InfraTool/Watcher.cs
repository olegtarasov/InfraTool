using System.Collections.Concurrent;
using InfraTool.Configuration;
using InfraTool.Helpers;
using InfraTool.Shell;

namespace InfraTool;

public record Item(string Name, string Status, string? Actual, string? Expected);

public record StatusItem(string Status, int Count);

public record WatcherResult(Item[] Items, StatusItem[] StatusCount);

public class Watcher
{
    private const string Error = "error";
    
    private readonly ILogger<Watcher> _logger;
    
    public Watcher(ILogger<Watcher> logger)
    {
        _logger = logger;
    }

    public async Task<WatcherResult> Execute(GroupConfig config)
    {
        var bag = new ConcurrentBag<Item>();

        var groupVars = await GetVariables(config.Variables);
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
            var variables = new Dictionary<string, string>(groupVars);
            
            if (item.Variables != null)
            {
                var itemVars = await GetVariables(item.Variables);
                
                // Item vars override group vars
                foreach (var itemVar in itemVars)
                    variables[itemVar.Key] = itemVar.Value;
            }

            try
            {
                if (item.Actual == null)
                    throw new InvalidOperationException("Actual source is not configured");
                
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
                if (item.Expected == null)
                    throw new InvalidOperationException("Expected source is not configured");
                
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

        var statusCount = bag.GroupBy(x => x.Status).Select(x => new StatusItem(x.Key, x.Count())).ToArray();
            
        return new(bag.ToArray(), statusCount);
    }

    private async Task<Dictionary<string, string>> GetVariables(Dictionary<string, InputConfig>? definitions)
    {
        var result = new Dictionary<string, string>();
        if (definitions == null)
            return result;
        foreach (var definition in definitions)
        {
            try
            {
                var values = await GetValues(definition.Value, result);
                if (values.Length == 0)
                {
                    result[definition.Key] = string.Empty;
                    continue;
                }

                result[definition.Key] = values.Length == 1 ? values[0] : values.Aggregate((s, s1) => s + Environment.NewLine + s1);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to get value for variable {Name}", definition.Key);
            }
        }

        return result;
    }

    private async Task<string[]> GetValues(InputConfig item, IDictionary<string, string>? variables)
    {
        if (item.Retriever == null)
            throw new InvalidOperationException("Retriever is not configured");
            
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