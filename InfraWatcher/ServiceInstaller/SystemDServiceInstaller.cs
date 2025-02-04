using System.Runtime.InteropServices;
using System.Text;
using InfraWatcher.Helpers;

namespace InfraWatcher.ServiceInstaller;

/// <summary>
/// A tool to install .NET services as Linux systemd.
/// </summary>
public class SystemDServiceInstaller
{
    private ILogger<SystemDServiceInstaller>? _logger;
    
    private readonly ServiceMetadataSystemd _metadata;

    /// <summary>
    /// Initializes the instance.
    /// </summary>
    /// <param name="serviceMetadata">Service medatada.</param>
    /// <param name="logger">Logger.</param>
    /// <exception cref="ArgumentException">Metadata Name property is null.</exception>
    /// <exception cref="ArgumentNullException">Argument is null.</exception>
    public SystemDServiceInstaller(ServiceMetadataSystemd serviceMetadata, ILogger<SystemDServiceInstaller>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(serviceMetadata);

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            throw new PlatformNotSupportedException();

        if (serviceMetadata.Name.IsNullOrEmpty())
            throw new ArgumentException("Service name is required");

        _metadata = serviceMetadata;
        _logger = logger;
    }

    /// <summary>
    /// Registers a systemd service in Linux OS.
    /// </summary>
    /// <returns><see cref="ValueTask"/> completed when the service is registered or an exception is thrown.</returns>
    /// <exception cref="InvalidOperationException">LinuxName property not set in metadata.</exception>
    public async ValueTask RegisterSystemDService()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            throw new PlatformNotSupportedException();

        if (string.IsNullOrEmpty(_metadata.User))
        {
            var (user, _) = await ProcessHelper.RunAndGetOutput("bash", "-c logname");
            if (user.Length != 1)
                throw new InvalidOperationException("Failed to get current user name");
            _metadata.User = user[0];
            
            _logger?.LogInformation("User was not set explicitly, defaulting to current user {User}", _metadata.User);
        }

        if (string.IsNullOrEmpty(_metadata.Group))
        {
            _metadata.Group = _metadata.User;
            _logger?.LogInformation("Group was not set explicitly, defaulting to current user group {Group}", _metadata.Group);
        }

        string defaultEnvironment = string.Empty;
        string environment = _metadata.EnvironmentVariables.Aggregate(
            defaultEnvironment, (acc, s) => acc + "\nEnvironment=\"" + s + "\"");
        
        var builder = new StringBuilder("[Unit]\n");
        if (!_metadata.DisplayName.IsNullOrEmpty())
            builder.AppendLine($"Description={_metadata.DisplayName}");

        builder.Append("""
                       
                       [Service]
                       Type=exec
                       """);

        if (!environment.IsNullOrEmpty())
            builder.AppendLine(environment);

        builder.Append($"""
                        ExecStart={_metadata.FileName}
                        User={_metadata.User}
                        Group={_metadata.Group}
                        
                        [Install]
                        WantedBy=multi-user.target
                        """);

        string serviceFileName = $"/etc/systemd/system/{_metadata.Name}.service";
        await File.WriteAllTextAsync(serviceFileName, builder.ToString());
        
        _logger?.LogInformation("Saved service unit to {FileName}", serviceFileName);
        
        _logger?.LogInformation("Running 'systemctl daemon-reload'");
        var (output, exitCode) = await ProcessHelper.RunAndGetOutput("bash", $"-c \"systemctl daemon-reload\"");
        if (exitCode != 0)
        {
            _logger?.LogError("Failed to execute systemctl daemon-reload");
            foreach (var line in output)
            {
                _logger?.LogError(line);
            }
        }
        
        if (_metadata.Start is StartType.Auto or StartType.DelayedAuto)
        {
            _logger?.LogInformation($"Service set to auto start, running 'systemctl enable {_metadata.Name}.service'");
            (output, exitCode) = await ProcessHelper.RunAndGetOutput("bash", $"-c \"systemctl enable {_metadata.Name}.service\"");
            if (exitCode != 0)
            {
                _logger?.LogError("Failed to execute systemctl enable");
                foreach (var line in output)
                {
                    _logger?.LogError(line);
                }
            }
        }
    }

    /// <summary>
    /// Deletes a systemd service from Linux OS.
    /// </summary>
    /// <returns><see cref="ValueTask"/> completed when the service is deleted.</returns>
    public async ValueTask DeleteSystemDService()
    {
        if (await IsServiceRunning())
        {
            _logger?.LogInformation($"Service is running, executing 'systemctl stop {_metadata.Name}.service'");
            if (!await StopService())
                return;
        }

        _logger?.LogInformation($"Disabling service by executing 'systemctl disable {_metadata.Name}.service'");
        await ProcessHelper.RunAndGetOutput("bash", $"-c \"systemctl disable {_metadata.Name}.service\"");

        string serviceFileName = $"/etc/systemd/system/{_metadata.Name}.service";
        _logger?.LogInformation("Removing service unit {FileName}", serviceFileName);
        try
        {
            File.Delete(serviceFileName);
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Failed to delete service unit");
        }

        _logger?.LogInformation("Running 'systemctl daemon-reload'");
        var (output, exitCode) = await ProcessHelper.RunAndGetOutput("bash", $"-c \"systemctl daemon-reload\"");
        if (exitCode != 0)
        {
            _logger?.LogError("Failed to execute systemctl daemon-reload");
            foreach (var line in output)
            {
                _logger?.LogError(line);
            }
        }
    }
    
    public async ValueTask<bool> RestartService()
    {
        var (output, exitCode) =
            await ProcessHelper.RunAndGetOutput("bash", $"-c \"systemctl restart {_metadata.Name}.service\"");
            
        if (exitCode != 0)
        {
            _logger?.LogError($"Failed to execute systemctl restart {_metadata.Name}.service");
            foreach (var line in output)
            {
                _logger?.LogError(line);
            }

            return false;
        }

        return true;
    }

    public async ValueTask<bool> StopService()
    {
        var (output, exitCode) =
            await ProcessHelper.RunAndGetOutput("bash", $"-c \"systemctl stop {_metadata.Name}.service\"");
            
        if (exitCode != 0)
        {
            _logger?.LogError($"Failed to execute systemctl stop {_metadata.Name}.service");
            foreach (var line in output)
            {
                _logger?.LogError(line);
            }

            return false;
        }

        return true;
    }

    public async ValueTask<bool> IsServiceInstalled()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            throw new PlatformNotSupportedException();

        var (output, exitCode) = await 
            ProcessHelper.RunAndGetOutput(
                $"sudo bash -c \"systemctl list-unit-files {_metadata.Name}.service &>/dev/null && echo True || echo False\"");

        if (exitCode != 0 || output.Length != 1 || !bool.TryParse(output[0].Text, out bool result))
        {
            _logger?.LogError("Failed to check whether service is installed");
            foreach (var line in output)
            {
                _logger?.LogError(line);
            }

            return false;
        }

        return result;
    }

    public async ValueTask<bool> IsServiceRunning()
    {
        var (output, exitCode) =
            await ProcessHelper.RunAndGetOutput("bash", $"-c \"systemctl is-active --quiet {_metadata.Name}.service\"");

        return exitCode == 0;
    }
}