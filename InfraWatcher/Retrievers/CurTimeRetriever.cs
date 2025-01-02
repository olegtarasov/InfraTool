using InfraWatcher.Helpers;

namespace InfraWatcher.Retrievers;

public class CurTimeRetriever : ILinesRetriever
{
    public Task<string[]> GetLines(IDictionary<string, string?>? variables)
    {
        return Task.FromResult(new[] { DateTime.Now.ToString("O") });
    }
}