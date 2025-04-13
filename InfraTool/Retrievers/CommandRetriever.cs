using InfraTool.Helpers;
using InfraTool.Shell;

namespace InfraTool.Retrievers;

public class CommandRetriever : ILinesRetriever
{
    public required string Command { get; set; }
    public string Chdir { get; set; } = string.Empty;
    public bool IncludeError { get; set; } = false;

    public async Task<string[]> GetLines(IDictionary<string, string>? variables)
    {
        var commands = SplitCommands(Command);
        if (commands.Length == 0)
            return [];

        var result = new List<string>();
        foreach (string cmd in commands)
        {
            var command = new ShellCommand(cmd, variables);
            var (output, _) = await command.RunAsync(Chdir);
            result.AddRange(IncludeError
                ? output.Select(x => x.Text)
                : output.Where(x => x.Type == OutputType.Output).Select(x => x.Text));
        }

        return result.ToArray();
    }

    private string[] SplitCommands(string commands)
    {
        var result = new List<string>();
        using (var reader = new StringReader(commands))
        {
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                string command = line.Trim();
                if (!command.IsNullOrEmpty())
                    result.Add(command);
            }
        }

        return result.ToArray();
    }
}