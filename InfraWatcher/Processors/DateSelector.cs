using System.Globalization;

namespace InfraWatcher.Processors;

public enum DateSelectorMode
{
    Earliest, 
    Latest
}

public class DateSelector : IProcessor
{
    public required string DateFormat { get; set; }
    public required DateSelectorMode Mode { get; set; }
    
    public string[] Process(string[] lines)
    {
        var dates = new List<DateTime>();
        foreach (string line in lines)
        {
            if (DateTime.TryParseExact(line, DateFormat, DateTimeFormatInfo.CurrentInfo, DateTimeStyles.AssumeLocal, out var result))
            {
                dates.Add(result);
            }
        }

        if (dates.Count == 0)
            return [];

        return Mode == DateSelectorMode.Earliest ? [dates.Min().ToString("O")] : [dates.Max().ToString("O")];
    }
}