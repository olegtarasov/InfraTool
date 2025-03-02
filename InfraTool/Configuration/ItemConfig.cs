using InfraTool.Comparers;

namespace InfraTool.Configuration;

public class ItemConfig
{
    public required string Name { get; set; }
    public Dictionary<string, InputConfig>? Variables { get; set; }
    public required InputConfig? Actual { get; set; }
    public required InputConfig? Expected { get; set; }
    public required IComparer? Comparer { get; set; }
}