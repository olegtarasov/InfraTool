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
    public GroupConfig[] Groups { get; set; } = [];

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
                    ("var", typeof(VariableRetriever)));
                x.AddKeyValueTypeDiscriminator<IProcessor>("type",
                    ("regex", typeof(RegexProcessor)),
                    ("date", typeof(DateProcessor)));
                x.AddKeyValueTypeDiscriminator<IComparer>("type",
                    ("version", typeof(VersionComparer)),
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