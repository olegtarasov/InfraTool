namespace InfraTool.Shell;

/// <summary>
/// A collection of output lines from a shell command along with an exit code.
/// </summary>
public record CommandOutput(OutputLine[] Output, int ExitCode)
{
    /// <summary>
    /// Output lines from standard output stream.
    /// </summary>
    public IEnumerable<string> StdOut => Output.Where(x => x.Type == OutputType.Output).Select(x => x.Text);

    /// <summary>
    /// Output lines from standard error stream.
    /// </summary>
    public IEnumerable<string> StdErr => Output.Where(x => x.Type == OutputType.Error).Select(x => x.Text);
}