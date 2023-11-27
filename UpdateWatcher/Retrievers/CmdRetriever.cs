using Common.Contrib.Helpers;

namespace UpdateWatcher.Retrievers;

public class CmdRetriever : RetrieverBase, IVersionRetriever
{
    public required string Cmd { get; set; }
    public string Args { get; set; } = string.Empty;
    public string WorkDir { get; set; } = string.Empty;

    public override async Task<Version?> GetVersion()
    {
        var lines = (await ProcessHelper.RunAndGetOutput(Cmd, Args, WorkDir, true)).Output
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