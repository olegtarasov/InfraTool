using InfraWatcher.Const;

namespace InfraWatcher.Comparers;

public class MaxDeltaComparer : IComparer
{
    public TimeSpan MaxDelta { get; set; } = TimeSpan.FromDays(1);
    public string CurrentResult { get; set; } = CommonStatus.Current;
    public string ExpiredResult { get; set; } = CommonStatus.Expired;
    
    public string Compare(string? actual, string? expected)
    {
        if (actual == null || expected == null)
            return CommonStatus.Error;

        var actualTime = DateTime.Parse(actual);
        var expectedTime = DateTime.Parse(expected);

        return (expectedTime - actualTime).Duration() > MaxDelta ? ExpiredResult : CurrentResult;
    }
}