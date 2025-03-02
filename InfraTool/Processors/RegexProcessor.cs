using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using InfraTool.Helpers;


namespace InfraTool.Processors;

public class RegexProcessor : ProcessorBase
{
    public required string Expr { get; set; }
    public string Replace { get; set; } = string.Empty;
    
    public override string[] Process(string[] lines)
    {
        var result = new List<string>();
        
        var regex = new Regex(Expr);
        foreach (string line in lines)
        {
            var match = regex.Match(line);
            if (!match.Success)
                continue;

            if (!Replace.IsNullOrEmpty())
            {
                result.Add(regex.Replace(line, Replace));
                continue;
            }

            if (match.Groups.Count > 1)
            {
                for (int i = 1; i < match.Groups.Count; i++)
                    result.Add(match.Groups[i].Value);
            }
            else
            {
                result.Add(match.Value);
            }
        }

        return ApplyFilters(result).ToArray();
    }
}