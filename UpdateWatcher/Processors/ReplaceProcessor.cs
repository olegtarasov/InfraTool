using System.Text.RegularExpressions;

namespace UpdateWatcher.Processors;

public class ReplaceProcessor : IProcessor
{
    public string Regex { get; set; } = string.Empty;
    public string Replace { get; set; } = string.Empty;
    
    public bool TryParseVersion(string[] inputs, out Version? version)
    {
        version = null;

        var regex = new Regex(Regex);
        foreach (string input in inputs)
        {
            var match = regex.Match(input);
            if (!match.Success)
                continue;

            if (Version.TryParse(regex.Replace(input, Replace), out version))
                return true;
        }

        return false;
    }
}