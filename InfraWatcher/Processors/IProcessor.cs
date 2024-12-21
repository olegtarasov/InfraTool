using System.Diagnostics.CodeAnalysis;

namespace InfraWatcher.Processors;

public interface IProcessor
{
    bool TryParseVersion(string[] lines, [NotNullWhen(true)] out string? value);
}