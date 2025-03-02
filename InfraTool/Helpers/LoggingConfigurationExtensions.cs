using System.Reflection;
using System.Web;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace InfraTool.Helpers;

/// <summary>
/// Extension methods that configure logging.
/// </summary>
public static class LoggingConfigurationExtensions
{
    /// <summary>
    /// Console ouput format.
    /// </summary>
    public enum ConsoleFormat
    {
        /// <summary>
        /// Colored text.
        /// </summary>
        TextColored,

        /// <summary>
        /// Plain text.
        /// </summary>
        TextPlain
    }

    /// <summary>
    /// File logger message template.
    /// </summary>
    public const string FileTemplate =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}][{SourceContext}] {Message:lj}{NewLine}{Exception}";

    /// <summary>
    /// Console logger message template.
    /// </summary>
    public const string ConsoleTemplate =
        "{Timestamp:HH:mm:ss} [{Level:u4}] {Message:lj}{NewLine}{Exception}";

    /// <summary>
    /// Adds console logging sink. Write in JSON for logstash-beeworks
    /// </summary>
    public static LoggerConfiguration AddSystemConsoleSink(
        this LoggerConfiguration config,
        ConsoleFormat format)
    {
        return config.WriteTo.Console(
            theme: format == ConsoleFormat.TextColored ? SystemConsoleTheme.Colored : ConsoleTheme.None,
            outputTemplate: ConsoleTemplate);
    }

    /// <summary>
    /// Adds file logging sink deriving file name from specified assembly.
    /// </summary>
    public static LoggerConfiguration AddFileSink(this LoggerConfiguration config, Assembly hostAssembly)
    {
        string? assemblyName = hostAssembly.GetName().Name;
        return AddFileSink(config, Path.Combine("logs", $"{assemblyName ?? "log"}.txt"));
    }

    /// <summary>
    /// Adds file logging sink using specified file name.
    /// </summary>
    public static LoggerConfiguration AddFileSink(this LoggerConfiguration config, string fileName)
    {
        return config.WriteTo.File(
            fileName,
            outputTemplate: FileTemplate,
            fileSizeLimitBytes: 1024 * 1024 * 5,
            retainedFileCountLimit: 10,
            rollOnFileSizeLimit: true);
    }
}