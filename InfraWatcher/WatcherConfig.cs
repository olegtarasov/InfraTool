using System.Text;
using System.Text.RegularExpressions;
using InfraWatcher.Comparers;
using InfraWatcher.Helpers;
using InfraWatcher.Processors;
using InfraWatcher.Retrievers;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace InfraWatcher;

public class ComparerDeserializer : INodeDeserializer
{
    public bool Deserialize(
        IParser reader,
        Type expectedType,
        Func<IParser, Type, object?> nestedObjectDeserializer,
        out object? value,
        ObjectDeserializer rootDeserializer)
    {
        if (expectedType != typeof(IComparer))
        {
            value = null;
            return false;
        }
        
        if (!reader.TryConsume<Scalar>(out var scalar))
        {
            value = null;
            return false;
        }

        value = scalar.Value switch
                {
                    "version" => new VersionComparer(),
                    _ => null
                };

        return value != null;
    }
}

public class WatcherConfig
{
    private static readonly Regex SecretRegex = new(@"!secret\((.*?)\)");

    public ServerConfig Server { get; set; } = new();
    public GroupConfig[] Groups { get; set; } = [];

    public static WatcherConfig Load()
    {
        if (AppContext.BaseDirectory.IsNullOrEmpty())
            throw new InvalidOperationException("Failed to get current directory");

        string configPath = Path.Combine(AppContext.BaseDirectory, "config", "config.yaml");
        if (!File.Exists(configPath))
            throw new FileNotFoundException("Configuration file not found! Create config.yaml in config directory");

        var secrets = LoadSecrets(Path.Combine(AppContext.BaseDirectory, "config", "secrets.yaml"));
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
            .WithNodeDeserializer(new ComparerDeserializer())
            .Build();

        return deserializer.Deserialize<WatcherConfig>(configText.ToString());
    }

    public static Dictionary<string, string> LoadSecrets()
    {
        if (AppContext.BaseDirectory.IsNullOrEmpty())
            throw new InvalidOperationException("Failed to get current directory");

        return LoadSecrets(Path.Combine(AppContext.BaseDirectory, "config", "secrets.yaml"));
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

public class GroupConfig
{
    public required string Name { get; set; }
    public ItemConfig[] Items { get; set; } = [];
    public IComparer? Comparer { get; set; }
}

public class ItemConfig
{
    public required string Name { get; set; }
    public Dictionary<string, string>? Variables { get; set; }
    public required VersionConfig Actual { get; set; }
    public required VersionConfig Expected { get; set; }
    public required IComparer? Comparer { get; set; }
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