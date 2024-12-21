using System.Collections.Concurrent;
using InfraWatcher.Shell;

namespace InfraWatcher;

public enum UnitStatus {Current, Update, Error}

public record Unit(string Name, UnitStatus Status, string? Local, string? Remote);

public class VersionWatcher
{
    private readonly ILogger<VersionWatcher> _logger;
    
    public VersionWatcher(ILogger<VersionWatcher> logger)
    {
        _logger = logger;
    }

    public async Task<Unit[]> GetVersions()
    {
        var config = WatcherConfig.Load();
        var bag = new ConcurrentBag<Unit>();

        await Parallel.ForEachAsync(config.Items, async (item, _) =>
        {
            string? local = null, remote = null;
            UnitStatus? status = null;
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
                        bag.Add(new Unit(item.Name, UnitStatus.Error, null, null));
                        return;
                    }

                    string? value = output.FirstOrDefault(x => x.Type == OutputType.Output)?.Text;
                    if (string.IsNullOrEmpty(value))
                    {
                        _logger.LogError("Variable {Variable} command didn't return a value", varDef.Key);
                        bag.Add(new Unit(item.Name, UnitStatus.Error, null, null));
                        return;
                    }

                    variables[varDef.Key] = value;
                }
            }

            try
            {
                local = await GetVersion(item.Local, variables);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to get local version for item '{Item}'", item.Name);
                status = UnitStatus.Error;
            }

            try
            {
                remote = await GetVersion(item.Remote, variables);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to get remote version for item '{Item}'", item.Name);
                status = UnitStatus.Error;
            }
            
            if (local == null || remote == null)
                status = UnitStatus.Error;

            if (status == null)
            {
                // Try to interpret results as strict versions
                if (Version.TryParse(local, out var localVersion)
                    && Version.TryParse(remote, out var remoteVersion))
                {
                    status = remoteVersion > localVersion ? UnitStatus.Update : UnitStatus.Current;
                }
                else
                {
                    status = string.Equals(local, remote) ? UnitStatus.Current : UnitStatus.Update;
                }
            }

            bag.Add(new(item.Name, status.Value, local, remote));
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
    private async Task<string?> GetVersion(VersionConfig item, IDictionary<string, string?>? variables)
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