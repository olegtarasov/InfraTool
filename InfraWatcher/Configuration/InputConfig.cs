using InfraWatcher.Processors;
using InfraWatcher.Retrievers;

namespace InfraWatcher.Configuration;

public class InputConfig
{
    public required ILinesRetriever? Retriever { get; set; }
    public IProcessor[] Processors { get; set; } = Array.Empty<IProcessor>();
}