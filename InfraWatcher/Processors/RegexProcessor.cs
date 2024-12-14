using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Common.Contracts.Helpers;

namespace InfraWatcher.Processors;

public class RegexProcessor : IProcessor
{
    public required string Regex { get; set; }
    public string Replace { get; set; } = string.Empty;
    
    public bool TryParseVersion(string[] inputs, [NotNullWhen(true)] out Version? version)
    {
        version = null;
        
        var regex = new Regex(Regex);
        foreach (string input in inputs)
        {
            var match = regex.Match(input);
            if (!match.Success || match.Groups.Count < 2)
                continue;

            string value = Replace.IsNullOrEmpty() 
                ? match.Groups[1].Value 
                : regex.Replace(input, Replace);
            
            if (Version.TryParse(value, out version))
                return true;
        }

        return false;
    }
}