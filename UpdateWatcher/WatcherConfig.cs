using System.Diagnostics;
using System.Reflection;
using Common.Contracts.Helpers;
using UpdateWatcher.Processors;
using UpdateWatcher.Retrievers;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace UpdateWatcher;

public class WatcherConfig
{
    public ItemConfig[] Items { get; set; }

    public static WatcherConfig Load()
    {
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

        string? dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (dir.IsNullOrEmpty())
            throw new InvalidOperationException("Failed to get current directory");
        
        string configPath = Path.Combine(dir, "config", "config.yaml");
        if (!File.Exists(configPath))
            throw new FileNotFoundException("Configuration file not found! Create config.yaml in config directory");

        return deserializer.Deserialize<WatcherConfig>(File.ReadAllText(configPath));
    }
}

public class ItemConfig
{
    public string Name { get; set; }
    public IVersionRetriever Local { get; set; }
    public IVersionRetriever Remote { get; set; }
}