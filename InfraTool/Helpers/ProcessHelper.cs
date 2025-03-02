using System.Diagnostics;
using InfraTool.Shell;

namespace InfraTool.Helpers;

/// <summary>
/// Helpers to work with processes
/// </summary>
public static class ProcessHelper
{
    /// <summary>
    /// Runs a process synchronously and captures its output and optionally error stream.
    /// </summary>
    public static async Task<(OutputLine[] Output, int ExitCode)> RunAndGetOutput(string fileName, string arguments = "", string workDir = "", bool includeError = false)
    {
        var info = new ProcessStartInfo(fileName, arguments)
                   {
                       RedirectStandardError = includeError,
                       RedirectStandardOutput = true,
                       WorkingDirectory = workDir
                   };
        var process = Process.Start(info) ?? throw new InvalidOperationException("Failed to start the process");
        var result = new List<OutputLine>();

        process.OutputDataReceived += (sender, args) =>
        {
            if (args.Data != null)
                result.Add(new(OutputType.Output, args.Data));
        };
        process.BeginOutputReadLine();

        if (includeError)
        {
            process.ErrorDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                    result.Add(new(OutputType.Error, args.Data));
            };
            process.BeginErrorReadLine();
        }

        await process.WaitForExitAsync();

        return (result.ToArray(), process.ExitCode);
    }
}