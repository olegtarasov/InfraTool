using InfraWatcher.Helpers;

namespace InfraWatcher.Comparers;

public class VersionComparer : IComparer
{
    public string Compare(string? actual, string? expected)
    {
        if (actual == null || expected == null)
            return VersionCompareResult.Error.ToSnakeCaseString();

        // Try to interpret results as strict versions
        if (Version.TryParse(actual, out var localVersion)
            && Version.TryParse(expected, out var remoteVersion))
        {
            return (remoteVersion > localVersion ? VersionCompareResult.Update : VersionCompareResult.Current).ToSnakeCaseString();
        }

        return (string.Equals(actual, expected) ? VersionCompareResult.Current : VersionCompareResult.Update).ToSnakeCaseString();
    }
}