using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Common.Contracts.Helpers;
using UpdateWatcher.Processors;
using UpdateWatcher.Retrievers;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace UpdateWatcher;

public class WatcherConfig
{
    private static readonly Regex _secretRegex = new(@"!secret\((.*?)\)");
    
    public ItemConfig[] Items { get; set; }

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
        while ((match = _secretRegex.Match(configText.ToString())).Success)
        {
            if (match.Groups.Count < 2)
                throw new InvalidDataException("Error in secret reference!");

            string secretName = match.Groups[1].Value;
            if (!secrets.TryGetValue(secretName, out string secretValue))
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
                x.AddUniqueKeyTypeDiscriminator<IVersionRetriever>(
                    ("cmd", typeof(CmdRetriever)));
            })
            .Build();

        return deserializer.Deserialize<WatcherConfig>(configText.ToString());
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
    public string Name { get; set; }
    public IVersionRetriever Local { get; set; }
    public IVersionRetriever Remote { get; set; }
}