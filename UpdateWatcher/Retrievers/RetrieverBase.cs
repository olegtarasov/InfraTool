using UpdateWatcher.Processors;

namespace UpdateWatcher.Retrievers;

public abstract class RetrieverBase : IVersionRetriever
{
    public IProcessor[] Processors { get; set; } = Array.Empty<IProcessor>();
    
    public abstract Task<Version?> GetVersion();
}