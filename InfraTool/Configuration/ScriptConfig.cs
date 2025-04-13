using InfraTool.Retrievers;

namespace InfraTool.Configuration;

public class ScriptConfig
{
    public required string Name { get; set; }
    public required CommandRetriever Execute { get; set; }
}