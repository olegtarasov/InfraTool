namespace InfraWatcher.Comparers;

public class VersionComparer : IComparer
{
    private const string Current = "current";
    private const string Update = "update";
    
    public string Compare(string? actual, string? expected)
    {
        if (actual == null || expected == null)
            return "error";

        // Try to interpret results as strict versions
        if (Version.TryParse(actual, out var localVersion)
            && Version.TryParse(expected, out var remoteVersion))
        {
            return remoteVersion > localVersion ? Update : Current;
        }

        return string.Equals(actual, expected) ? Current : Update;
    }
}