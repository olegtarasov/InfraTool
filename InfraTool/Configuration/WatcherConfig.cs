using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using InfraTool.Comparers;
using InfraTool.Processors;
using InfraTool.Retrievers;
using InfraTool.Helpers;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace InfraTool.Configuration;

public class WatcherConfig
{
    private const string ConfigFileName = "config.yaml";
    private const string SecretsFileName = "secrets.yaml";
    
    private static readonly Regex SecretRegex = new(@"!secret\((.*?)\)");

    public ServerConfig Server { get; set; } = new();
    public ComparisonConfig[] Comparisons { get; set; } = [];
    public ScriptConfig[] Scripts { get; set; } = [];

    public static WatcherConfig Load()
    {
        if (!File.Exists(ConfigFileName))
            throw new FileNotFoundException("Configuration file not found! Create config.yaml in config directory");

        var secrets = LoadSecrets(SecretsFileName);
        var configText = new StringBuilder(File.ReadAllText(ConfigFileName));
        
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
            .IgnoreUnmatchedProperties()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .WithNodeDeserializer(new RetrieverDeserializer()) // Try to deserialize comparer in simple form (retriever: cur_time)
            .WithNodeDeserializer(new ComparerDeserializer()) // Try to deserialize comparer in simple form (comparer: version)
            .WithNodeDeserializer(new TimeSpanDeserializer())
            .WithTypeDiscriminatingNodeDeserializer(x =>
            {
                x.AddKeyValueTypeDiscriminator<ILinesRetriever>("type",
                    ("cmd", typeof(CommandRetriever)),
                    ("webdav_list", typeof(WebDavFileListRetriever)),
                    ("cur_time", typeof(CurTimeRetriever)),
                    ("gh_release", typeof(GithubReleaseRetriever)),
                    ("gh_tag", typeof(GithubTagRetriever)),
                    ("var", typeof(VariableRetriever)));
                x.AddKeyValueTypeDiscriminator<IProcessor>("type",
                    ("regex", typeof(RegexProcessor)),
                    ("date", typeof(DateProcessor)));
                x.AddKeyValueTypeDiscriminator<IComparer>("type",
                    ("version", typeof(VersionComparer)),
                    ("max_delta", typeof(MaxDeltaComparer)));
            })
            .Build();

        var config = deserializer.Deserialize<WatcherConfig>(configText.ToString());
        ValidateConfig(config);
        return config;
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

    private static void ValidateConfig(WatcherConfig config)
    {
        var existing = new HashSet<string>();
        foreach (var name in config.Comparisons.Select(x => x.Name).Concat(config.Scripts.Select(x => x.Name)))
        {
            if (!existing.Add(name))
                throw new InvalidDataException($"Comparison and script names must be unique. Offending name: {name}");
        }
    }
}