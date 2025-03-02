using System.Diagnostics;
using InfraTool.Helpers;

namespace InfraTool.Shell;

/// <summary>
/// Executes one or several commands using bash on Linux and macOS and cmd on Windows.
/// </summary>
public class ShellCommand
{
    private readonly List<string> _commands = new();
    private readonly IDictionary<string, string>? _environmentVariables;
    
    /// <summary>
    /// Ctor.
    /// </summary>
    public ShellCommand(string command, IDictionary<string, string>? environmentVariables = null)
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS() && !OperatingSystem.IsWindows())
            throw new NotSupportedException("Only Lunux, MacOS and Windows are supported!");
        
        using (var reader = new StringReader(command))
        {
            string? line;
            while ((line = reader.ReadLine()?.Trim()) != null)
            {
                if (string.IsNullOrEmpty(line))
                    continue;
                _commands.Add(line);
            }
        }

        if (_commands.Count == 0)
            throw new ArgumentException("Command is empty", nameof(command));

        _environmentVariables = environmentVariables;
    }

    /// <summary>
    /// Runs shell commands.
    /// </summary>
    public async Task<CommandOutput> RunAsync(string? workingDirectory = null)
    {
        var output = new List<OutputLine>();
        string argument;
        
        if (_commands.Count > 1)
        {
            string? header = null; 
            
            argument = Path.GetTempFileName();

            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                File.SetUnixFileMode(
                    argument,
                    UnixFileMode.UserRead
                    | UnixFileMode.UserWrite
                    | UnixFileMode.UserExecute
                    | UnixFileMode.GroupRead
                    | UnixFileMode.OtherRead);
                header = "#!/bin/bash";
            }
            
            
            using (var writer = new StreamWriter(argument))
            {
                if (header != null)
                    writer.WriteLine(header);
                
                foreach (string command in _commands)
                {
                    writer.WriteLine(command);
                }
            }

            if (OperatingSystem.IsWindows())
                argument = $"/c \"{argument}\"";
        }
        else
        {
            argument = $"{(OperatingSystem.IsWindows() ? "/" : "-")}c \"{_commands[0]}\"";
        }

        var info = new ProcessStartInfo(OperatingSystem.IsWindows() ? "cmd.exe" : "bash", argument)
                   {
                       RedirectStandardError = true,
                       RedirectStandardOutput = true,
                       WorkingDirectory = workingDirectory,
                       UseShellExecute = false,
                   };

        if (_environmentVariables != null)
        {
            info.Environment!.AddRange(_environmentVariables);
        }
        
        var process = new Process{ StartInfo = info };

        process.OutputDataReceived += (sender, args) =>
        {
            if (args.Data != null)
                output.Add(new OutputLine(OutputType.Output, args.Data));
        };

        process.ErrorDataReceived += (sender, args) =>
        {
            if (args.Data != null)
                output.Add(new OutputLine(OutputType.Error, args.Data));
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        if (_commands.Count > 1)
        {
            try
            {
                File.Delete(argument);
            }
            catch
            {
            }
        }

        return new(output.ToArray(), process.ExitCode);
    }
}