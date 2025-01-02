using System.Text;
using System.Text.RegularExpressions;
using InfraWatcher.Comparers;
using InfraWatcher.Helpers;
using InfraWatcher.Processors;
using InfraWatcher.Retrievers;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace InfraWatcher.Configuration;

public class WatcherConfig
{
    private static readonly Regex SecretRegex = new(@"!secret\((.*?)\)");

    public ServerConfig Server { get; set; } = new();
    public GroupConfig[] Groups { get; set; } = [];

    public static WatcherConfig Load()
    {
        if (AppContext.BaseDirectory.IsNullOrEmpty())
            throw new InvalidOperationException("Failed to get current directory");

        string configPath = Path.Combine(AppContext.BaseDirectory, "config.yaml");
        if (!File.Exists(configPath))
            throw new FileNotFoundException("Configuration file not found! Create config.yaml in config directory");

        var secrets = LoadSecrets(Path.Combine(AppContext.BaseDirectory, "secrets.yaml"));
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
            .WithNodeDeserializer(new RetrieverDeserializer()) // Try to deserialize comparer in simple form (retriever: cur_time)
            .WithNodeDeserializer(new ComparerDeserializer()) // Try to deserialize comparer in simple form (comparer: version)
            .WithNodeDeserializer(new TimeSpanDeserializer())
            .WithTypeDiscriminatingNodeDeserializer(x =>
            {
                x.AddUniqueKeyTypeDiscriminator<IProcessor>(
                    ("regex", typeof(RegexProcessor)),
                    ("date_format", typeof(DateSelector)));
                x.AddUniqueKeyTypeDiscriminator<ILinesRetriever>(
                    ("command", typeof(CommandRetriever)),
                    ("webdav", typeof(WebDavFileListRetriever)));
                x.AddUniqueKeyTypeDiscriminator<IComparer>( // Try to deserealize comparer in complex form (as mapping)
                    ("max_delta", typeof(MaxDeltaComparer)));
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