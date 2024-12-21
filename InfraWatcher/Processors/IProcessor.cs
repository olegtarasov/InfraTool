using System.Diagnostics.CodeAnalysis;

namespace InfraWatcher.Processors;

public interface IProcessor
{
    bool TryParseVersion(string[] inputs, [NotNullWhen(true)] out string? version);
}