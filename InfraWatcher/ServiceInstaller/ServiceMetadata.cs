namespace InfraWatcher.ServiceInstaller;

/// <summary>
/// The implementing class contains service metadata with Name, DisplayName, Start and EventLogLevelMinimal properties.
/// </summary>
public abstract class ServiceMetadata
{
    /// <summary>
    /// Target executable path.
    /// </summary>
    public required string FileName { get; set; }

    /// <summary>
    /// Working directory (optional).
    /// </summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// Gets or sets the system strong name for the service.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the display name for the service.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the service's start type.
    /// Should be one of the <see cref="StartType"/> values.
    /// </summary>
    public string Start { get; set; } = StartType.Auto;

    /// <summary>
    /// Gets or sets the minimal logging level for the class.
    /// </summary>
    public LogLevel EventLogLevelMinimal { get; set; } = LogLevel.Debug;

}