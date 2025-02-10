namespace InfraWatcher.Retrievers;

public interface ILinesRetriever
{
    Task<string[]> GetLines(IDictionary<string, string>? variables);
}