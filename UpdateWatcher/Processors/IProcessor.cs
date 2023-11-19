using System.Diagnostics.CodeAnalysis;

namespace UpdateWatcher.Processors;

public interface IProcessor
{
    bool TryParseVersion(string[] inputs, [NotNullWhen(true)] out Version? version);
}