using InfraTool.Helpers;

namespace InfraTool.Retrievers;

public class VariableRetriever : ILinesRetriever
{
    public required string Name { get; set; }
    
    public Task<string[]> GetLines(IDictionary<string, string>? variables)
    {
        if (Name.IsNullOrEmpty())
            throw new InvalidOperationException("Variable name is required");

        if (variables == null || !variables.ContainsKey(Name))
            throw new InvalidOperationException($"Variable collection doesn't contain variable {Name}");

        return Task.FromResult(variables[Name].Split('\n'));
    }
}