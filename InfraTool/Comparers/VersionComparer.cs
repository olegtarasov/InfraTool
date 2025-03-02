using InfraTool.Const;

namespace InfraTool.Comparers;

public class VersionComparer : IComparer
{
    public string CurrentResult { get; set; } = CommonStatus.Current;
    public string UpdateResult { get; set; } = CommonStatus.Update;
    
    public string Compare(string? actual, string? expected)
    {
        if (actual == null || expected == null)
            return CommonStatus.Error;

        // Try to interpret results as strict versions
        if (Version.TryParse(actual, out var localVersion)
            && Version.TryParse(expected, out var remoteVersion))
        {
            return remoteVersion > localVersion ? UpdateResult : CurrentResult;
        }

        return string.Equals(actual, expected) ? CurrentResult : UpdateResult;
    }
}