using System.Diagnostics.CodeAnalysis;

namespace InfraWatcher.Processors;

public interface IProcessor
{
    string[] Process(string[] lines);
}