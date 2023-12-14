using Common.Contrib.Helpers;
using Common.Contrib.Shell;

namespace UpdateWatcher.Retrievers;

public class CmdRetriever : ILinesRetriever
{
    public required string Cmd { get; set; }
    public string WorkDir { get; set; } = string.Empty;
    public bool IncludeError { get; set; } = false;

    public async Task<string[]> GetLines(IDictionary<string, string?>? variables)
    {
        var command = new ShellCommand(Cmd, variables);
        var (output, _) = await command.RunAsync(WorkDir);
        return IncludeError
            ? output.Select(x => x.Text).ToArray()
            : output.Where(x => x.Type == OutputType.Output).Select(x => x.Text).ToArray();
    }
}