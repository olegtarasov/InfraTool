using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace UpdateWatcher.Processors;

public class RegexProcessor : IProcessor
{
    public string Regex { get; set; } = string.Empty;
    
    public bool TryParseVersion(string[] inputs, [NotNullWhen(true)] out Version? version)
    {
        version = null;
        
        var regex = new Regex(Regex);
        foreach (string input in inputs)
        {
            var match = regex.Match(input);
            if (match.Groups.Count < 2)
                continue;

            if (Version.TryParse(match.Groups[1].Value, out version))
                return true;
        }

        return false;
    }
}