using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using InfraWatcher.Helpers;
using InfraWatcher.ServiceInstaller;
using Spectre.Console.Cli;

namespace InfraWatcher.Cli;

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

        if (githubVersion.Version > localVersion)
        {
            string platform = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "linux" : "osx";
            string arch = RuntimeInformation.OSArchitecture == Architecture.X64 ? "x64" : "arm64";
            var asset = githubVersion.Assets.FirstOrDefault(x => x.Name == $"infrawatcher-{platform}-{arch}.zip");
            if (asset == null)
                throw new InvalidOperationException($"Can't find an asset for platform {platform} and architecture {arch}");
            
            string tempFile = Path.GetTempFileName();
            _logger.LogInformation("Downloading new version from {Url} to {FileName}", asset.BrowserDownloadUrl, tempFile);
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://api.github.com/repos/olegtarasov/InfraWatcher/releases/assets/{asset.Id}");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",
                "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");
            request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
            request.Headers.Add("User-Agent", "olegtarasov");
            var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"HTTP status code {response.StatusCode}");
            
            using (var stream = new FileStream(tempFile, FileMode.Create))
            {
                await response.Content.CopyToAsync(stream);
            }
            _logger.LogInformation("Downloaded");
            string? fileName = Process.GetCurrentProcess().MainModule?.FileName;
            if (fileName.IsNullOrEmpty())
                throw new InvalidOperationException("Error: could not get path to the executable");
            string dir = Path.GetDirectoryName(fileName) ?? "";
            try
            {
                _logger.LogInformation("Deleting the old binary: {FileName}", fileName);
                if (File.Exists(fileName))
                    File.Delete(fileName);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Failed to delete the old binary", e);
            }
            try
            {
                _logger.LogInformation("Extracting zip file to directory: {Directory}", dir);
                ZipFile.ExtractToDirectory(tempFile, dir);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Failed to extract zip file", e);
            }
            try
            {
                _logger.LogInformation("Cleaning up");
                File.Delete(tempFile);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Failed to delete downloaded archive", e);
            }

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return 0;
            
            var installer = new SystemDServiceInstaller(GetServiceMetadata(), _loggerFactory.CreateLogger<SystemDServiceInstaller>());
            if (await installer.IsServiceInstalled() && await installer.IsServiceRunning())
            {
                _logger.LogInformation("Systemd service is installed and running, restarting");
                return await installer.RestartService() ? 0 : 1;
            }
        }
        
        return 0;
    }

    private static async Task<(Version Version, GithubAsset[] Assets)> GetLatestVersion()
    {
        try
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get,
                "https://api.github.com/repos/olegtarasov/InfraWatcher/releases/latest");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",
                "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");
            request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
            request.Headers.Add("User-Agent", "olegtarasov");
            var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"HTTP status code {response.StatusCode}");
            string text = await response.Content.ReadAsStringAsync();
            var deserialized = JsonSerializer.Deserialize<GithubVersion>(text, new JsonSerializerOptions(JsonSerializerDefaults.Web)
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });
            var githubVersion = Version.Parse(deserialized.Name[1..]);
            return (githubVersion, deserialized.Assets);
        }
        catch (Exception e)
        {
            throw new InvalidOperationException("Failed to get latest version from Github", e);
        }
    }

    private class GithubVersion
    {
        public string Name { get; set; }
        public GithubAsset[] Assets { get; set; }
    }
    
    private class GithubAsset
    {
        public string Name { get; set; }
        public string BrowserDownloadUrl { get; set; }
        public long Id { get; set; }
    }
}