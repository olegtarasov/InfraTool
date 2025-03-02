namespace InfraTool.Retrievers;

public class CurTimeRetriever : ILinesRetriever
{
    public TimezoneMode Tz { get; set; } = TimezoneMode.Local;
    public string Format { get; set; } = "O";
    
    public Task<string[]> GetLines(IDictionary<string, string>? variables)
    {
        return Task.FromResult(new[]
                               {
                                   Tz == TimezoneMode.Local
                                    ? DateTime.Now.ToString(Format)
                                    : DateTime.UtcNow.ToString(Format)
                               });
    }
}