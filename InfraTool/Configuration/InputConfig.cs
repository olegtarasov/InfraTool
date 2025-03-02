using InfraTool.Processors;
using InfraTool.Retrievers;

namespace InfraTool.Configuration;

public class InputConfig
{
    public required ILinesRetriever? Retriever { get; set; }
    public IProcessor[] Processors { get; set; } = Array.Empty<IProcessor>();
}