using System.Diagnostics.CodeAnalysis;

namespace InfraTool.Processors;

public interface IProcessor
{
    string[] Process(string[] lines);
}