using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using InfraTool.Helpers;
using InfraTool.ServiceInstaller;
using Spectre.Console.Cli;

namespace InfraTool.Cli;

public class UpdateCommand : SystemdCommandBase
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<UpdateCommand> _logger;

    public UpdateCommand(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
        _logger = _loggerFactory.CreateLogger<UpdateCommand>();
    }

    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && !RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            throw new PlatformNotSupportedException("Self-update currently works only on Linux and macOs");

        if (RuntimeInformation.OSArchitecture != Architecture.X64 && RuntimeInformation.OSArchitecture != Architecture.Arm64)
            throw new PlatformNotSupportedException("Self-update currently works only on x64 and arm64 architectures");
        
        var githubVersion = await GetLatestVersion();
        var localVersion = Assembly.GetExecutingAssembly().GetName().Version;

        _logger.LogInformation("Github latest version: {Version}", githubVersion.Version);
        _logger.LogInformation("Local version: {Version}", localVersion);

        if (githubVersion.Version <= localVersion)
        {
            _logger.LogInformation("No need to update");
            return 0;
        }

        // We are creating the installer beforehand because there is a problem with bundled deps after we replace the binary
        SystemDServiceInstaller? installer = null;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            installer = new SystemDServiceInstaller(GetServiceMetadata(), _loggerFactory.CreateLogger<SystemDServiceInstaller>());

        string platform = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "linux" : "osx";
        string arch = RuntimeInformation.OSArchitecture == Architecture.X64 ? "x64" : "arm64";
        var asset = githubVersion.Assets.FirstOrDefault(x => x.Name == $"infratool-{platform}-{arch}.zip");
        if (asset == null)
            throw new InvalidOperationException($"Can't find an asset for platform {platform} and architecture {arch}");
            
        string zipFile = await DownloadAsset(asset);
        string? binaryFileName = Process.GetCurrentProcess().MainModule?.FileName;
        if (binaryFileName.IsNullOrEmpty())
            throw new InvalidOperationException("Error: could not get path to the executable");
        string binaryDir = Path.GetDirectoryName(binaryFileName) ?? "";
        try
        {
            _logger.LogInformation("Deleting the old binary: {FileName}", binaryFileName);
            if (File.Exists(binaryFileName))
                File.Delete(binaryFileName);
        }
        catch (Exception e)
        {
            throw new InvalidOperationException("Failed to delete the old binary", e);
        }
        try
        {
            _logger.LogInformation("Extracting zip file to directory: {Directory}", binaryDir);
            ZipFile.ExtractToDirectory(zipFile, binaryDir);
        }
        catch (Exception e)
        {
            throw new InvalidOperationException("Failed to extract zip file", e);
        }
        try
        {
            _logger.LogInformation("Cleaning up");
            File.Delete(zipFile);
        }
        catch (Exception e)
        {
            throw new InvalidOperationException("Failed to delete downloaded archive", e);
        }

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return 0;

        if (installer == null)
            throw new InvalidOperationException("Didn't create sytemd installer beforehand!");
        
        if (await installer.IsServiceInstalled() && await installer.IsServiceRunning())
        {
            _logger.LogInformation("Systemd service is installed and running, restarting");
            if (!await installer.RestartService())
                return 1;

            _logger.LogInformation("Fixing file ownership");
            var userName = await SystemDServiceInstaller.GetCurrentUser();
            var (output, exitCode) =
                await ProcessHelper.RunAndGetOutput("bash", $"-c \"chown {userName}:{userName} {binaryFileName}\"");
            
            if (exitCode != 0)
            {
                _logger?.LogError($"Failed to execute chown");
                foreach (var line in output)
                {
                    _logger?.LogError(line);
                }

                return 1;
            }
        }
        
        return 0;
    }

    private async Task<string> DownloadAsset(GithubAsset asset)
    {
        string tempFile = Path.GetTempFileName();
        _logger.LogInformation("Downloading new version from {Url} to {FileName}", asset.BrowserDownloadUrl, tempFile);
        var response = await MakeGHApiRequest(
            $"https://api.github.com/repos/olegtarasov/InfraTool/releases/assets/{asset.Id}",
            "application/octet-stream");
        
        using (var stream = new FileStream(tempFile, FileMode.Create))
        {
            await response.Content.CopyToAsync(stream);
        }
        _logger.LogInformation("Downloaded");
        return tempFile;
    }

    private static async Task<(Version Version, GithubAsset[] Assets)> GetLatestVersion()
    {
        try
        {
            var response = await MakeGHApiRequest(
                "https://api.github.com/repos/olegtarasov/InfraTool/releases/latest",
                "application/vnd.github+json");
            string text = await response.Content.ReadAsStringAsync();
            var deserialized = JsonSerializer.Deserialize<GithubVersion>(text, new JsonSerializerOptions(JsonSerializerDefaults.Web)
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                }) ?? throw new InvalidOperationException("Failed to deserialize API response");
            var githubVersion = Version.Parse(deserialized.Name[1..]);
            return (githubVersion, deserialized.Assets);
        }
        catch (Exception e)
        {
            throw new InvalidOperationException("Failed to get latest version from Github", e);
        }
    }

    private static async Task<HttpResponseMessage> MakeGHApiRequest(string url, string mimeType)
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(mimeType));
        request.Headers.Add("User-Agent", "olegtarasov");
        var response = await client.SendAsync(request);
        if (response is not { IsSuccessStatusCode: true })
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"HTTP status code {response.StatusCode}. Response:\n{errorContent}");
        }

        return response;
    }

    private class GithubVersion
    {
        public required string Name { get; set; }
        public required GithubAsset[] Assets { get; set; }
    }
    
    private class GithubAsset
    {
        public required string Name { get; set; }
        public required string BrowserDownloadUrl { get; set; }
        public required long Id { get; set; }
    }
}