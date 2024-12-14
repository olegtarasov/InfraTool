using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using InfraWatcher.Helpers;
using InfraWatcher.Processors;
using InfraWatcher.Retrievers;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace InfraWatcher;

public class WatcherConfig
{
    private static readonly Regex SecretRegex = new(@"!secret\((.*?)\)");

    public ServerConfig Server { get; set; } = new();
    public ItemConfig[] Items { get; set; } = Array.Empty<ItemConfig>();

    public static WatcherConfig Load()
    {
        string? dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (dir.IsNullOrEmpty())
            throw new InvalidOperationException("Failed to get current directory");

        string configPath = Path.Combine(dir, "config", "config.yaml");
        if (!File.Exists(configPath))
            throw new FileNotFoundException("Configuration file not found! Create config.yaml in config directory");

        var secrets = LoadSecrets(Path.Combine(dir, "config", "secrets.yaml"));
        var configText = new StringBuilder(File.ReadAllText(configPath));
        
        Match match;
        while ((match = SecretRegex.Match(configText.ToString())).Success)
        {
            if (match.Groups.Count < 2)
                throw new InvalidDataException("Error in secret reference!");

            string secretName = match.Groups[1].Value;
            if (!secrets.TryGetValue(secretName, out string? secretValue))
                throw new InvalidDataException($"Secret with name '{secretName}' not found in config/secrets.yaml");

            configText.Remove(match.Index, match.Length);
            configText.Insert(match.Index, secretValue);
        }
        
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .WithTypeDiscriminatingNodeDeserializer(x =>
            {
                x.AddUniqueKeyTypeDiscriminator<IProcessor>(
                    ("regex", typeof(RegexProcessor)));
                x.AddUniqueKeyTypeDiscriminator<ILinesRetriever>(
                    ("cmd", typeof(CmdRetriever)));
            })
            .Build();

        return deserializer.Deserialize<WatcherConfig>(configText.ToString());
    }

    public static Dictionary<string, string> LoadSecrets()
    {
        string? dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (dir.IsNullOrEmpty())
            throw new InvalidOperationException("Failed to get current directory");

        return LoadSecrets(Path.Combine(dir, "config", "secrets.yaml"));
    }
    
    private static Dictionary<string, string> LoadSecrets(string fileName)
    {
        if (!File.Exists(fileName))
            return new();

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        return deserializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(fileName));
    }
}

public class ItemConfig
{
    public required string Name { get; set; }
    public Dictionary<string, string>? Variables { get; set; }
    public required VersionConfig Local { get; set; }
    public required VersionConfig Remote { get; set; }
}

public class VersionConfig
{
    public required ILinesRetriever Retriever { get; set; }
    public IProcessor[] Processors { get; set; } = Array.Empty<IProcessor>();
}

public class ServerConfig
{
    public int Port { get; set; } = 5015;
    public LokiConfig? Loki { get; set; }
}

public class LokiConfig
{
    public required string Login { get; set; }
    public required string Password { get; set; }
}