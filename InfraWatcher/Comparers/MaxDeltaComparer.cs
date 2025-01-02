namespace InfraWatcher.Comparers;

public class MaxDeltaComparer : IComparer
{
    public TimeSpan MaxDelta { get; set; } = TimeSpan.FromDays(1);
    
    public string Compare(string? actual, string? expected)
    {
        if (actual == null || expected == null)
            return "error";

        var actualTime = DateTime.Parse(actual);
        var expectedTime = DateTime.Parse(expected);

        return (expectedTime - actualTime).Duration() > MaxDelta ? "expired" : "current";
    }
}