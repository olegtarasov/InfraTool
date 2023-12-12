using Common.Contrib.Helpers;
using Common.Contrib.Shell;

namespace UpdateWatcher.Retrievers;

public class CmdRetriever : RetrieverBase, IVersionRetriever
{
    public required string Cmd { get; set; }
    public string WorkDir { get; set; } = string.Empty;

    public override async Task<Version?> GetVersion(IDictionary<string, string?>? variables)
    {
        var command = new ShellCommand(Cmd, variables);
        var lines = (await command.RunAsync(WorkDir)).Output
            .Select(x => x.Text).ToArray();
        
        if (lines.Length == 0)
            return null;
        
        // Try the processors
        if (Processors.Length != 0)
        {
            foreach (var processor in Processors)
            {
                if (processor.TryParseVersion(lines, out var version))
                    return version;
            }
        }
        
        // Maybe will just parse the version
        foreach (var line in lines)
        {
            
            if (Version.TryParse(line, out var version))
                return version;
        }

        return null;
    }
}