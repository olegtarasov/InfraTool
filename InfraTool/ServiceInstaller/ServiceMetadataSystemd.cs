namespace InfraTool.ServiceInstaller;

/// <summary>
/// The implementing class contains service metadata for the system daemon.
/// </summary>
public sealed class ServiceMetadataSystemd : ServiceMetadata
{
    /// <summary>
    /// Environmnet variables to be defined for the service.
    /// </summary>
    public string[] EnvironmentVariables { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// Gets or sets a user account name that will be used to run the service. If not specified,
    /// </summary>
    public string? User { get; set; }

    /// <summary>
    /// Gets or sets a user group that will be used to run the service.
    /// </summary>
    public string? Group { get; set; }

}