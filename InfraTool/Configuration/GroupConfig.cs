using InfraTool.Comparers;

namespace InfraTool.Configuration;

public class GroupConfig
{
    public required string Name { get; set; }
    public ItemConfig[] Items { get; set; } = [];
    public IComparer? Comparer { get; set; }
    public Dictionary<string, InputConfig>? Variables { get; set; }
}