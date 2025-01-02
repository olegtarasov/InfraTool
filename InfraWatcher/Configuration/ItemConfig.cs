using InfraWatcher.Comparers;

namespace InfraWatcher.Configuration;

public class ItemConfig
{
    public required string Name { get; set; }
    public Dictionary<string, string>? Variables { get; set; }
    public required VersionConfig? Actual { get; set; }
    public required VersionConfig? Expected { get; set; }
    public required IComparer? Comparer { get; set; }
}