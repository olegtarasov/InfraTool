using System.Globalization;

namespace InfraTool.Processors;

public class DateProcessor : ProcessorBase
{
    public required string InputFormat { get; set; }
    public string OutputFormat { get; set; } = "O";
    
    public override string[] Process(string[] lines)
    {
        var dates = new List<DateTime>();
        foreach (string line in lines)
        {
            if (DateTime.TryParseExact(line, InputFormat, DateTimeFormatInfo.CurrentInfo, DateTimeStyles.AssumeLocal, out var result))
            {
                dates.Add(result);
            }
        }

        if (dates.Count == 0)
            return [];

        return ApplyFilters(dates).Select(x => x.ToString(OutputFormat)).ToArray();
    }
}