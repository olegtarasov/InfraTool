namespace UpdateWatcher.Retrievers;

public interface IVersionRetriever
{
    Task<Version?> GetVersion(IDictionary<string, string?>? variables);
}