using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using InfraWatcher.Helpers;


namespace InfraWatcher.Processors;

public class RegexProcessor : IProcessor
{
    public required string Regex { get; set; }
    public string Replace { get; set; } = string.Empty;
    
    public bool TryParseVersion(string[] lines, [NotNullWhen(true)] out string? value)
    {
        value = null;
        
        var regex = new Regex(Regex);
        foreach (string line in lines)
        {
            var match = regex.Match(line);
            if (!match.Success || match.Groups.Count < 2)
                continue;

            value = Replace.IsNullOrEmpty() 
                ? match.Groups[1].Value 
                : regex.Replace(line, Replace);

            return true;
        }

        return false;
    }
}