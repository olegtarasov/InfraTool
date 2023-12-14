using System.Collections.Concurrent;
using Common.Contrib.Shell;
using Common.Host.Web.Api;
using Microsoft.AspNetCore.Mvc;

namespace UpdateWatcher.Controllers;

public enum UnitStatus {Current, Update, Error}

public record Unit(string Name, UnitStatus Status, Version Local, Version Remote);

[Route("/api/updates")]
public class UpdatesController : ApiControllerBase
{
    private readonly ILogger<UpdatesController> _logger;
    private readonly VariableHolder _variableHolder;

    public UpdatesController(ILogger<UpdatesController> logger, VariableHolder variableHolder)
    {
        _logger = logger;
        _variableHolder = variableHolder;
    }

    [HttpGet]
    [Route("")]
    public async Task<Unit[]> GetVersions()
    {
        var config = WatcherConfig.Load();
        var bag = new ConcurrentBag<Unit>();

        await Parallel.ForEachAsync(config.Items, async (item, _) =>
        {
            Version? local = null, remote = null;
            UnitStatus? status = null;

            if (item.Variables != null)
            {
                foreach (var varDef in item.Variables)
                {
                    var command = new ShellCommand(varDef.Value, _variableHolder.Variables);
                    var (output, result) = await command.RunAsync();
                    
                    if (result != 0)
                    {
                        _logger.LogError("Variable {Variable} command exited with result code {Code}", varDef.Key, result);
                        bag.Add(new Unit(item.Name, UnitStatus.Error, new(), new()));
                        return;
                    }

                    string? value = output.FirstOrDefault(x => x.Type == OutputType.Output)?.Text;
                    if (string.IsNullOrEmpty(value))
                    {
                        _logger.LogError("Variable {Variable} command didn't return a value", varDef.Key);
                        bag.Add(new Unit(item.Name, UnitStatus.Error, new(), new()));
                        return;
                    }

                    _variableHolder.Variables[varDef.Key] = value;
                }
            }

            try
            {
                local = await GetVersion(item.Local, _variableHolder.Variables);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to get local version for item '{Item}'", item.Name);
                status = UnitStatus.Error;
            }

            try
            {
                remote = await GetVersion(item.Remote, _variableHolder.Variables);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to get remote version for item '{Item}'", item.Name);
                status = UnitStatus.Error;
            }
            
            if (local == null || remote == null)
                status = UnitStatus.Error;

            status ??= remote > local ? UnitStatus.Update : UnitStatus.Current;
            
            bag.Add(new(item.Name, status.Value, local ?? new(), remote ?? new()));
        });

        return bag.ToArray();
    }

    private async Task<Version?> GetVersion(VersionConfig item, IDictionary<string, string?>? variables)
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
            if (Version.TryParse(line, out var version))
                return version;
        }

        return null;
    }
}