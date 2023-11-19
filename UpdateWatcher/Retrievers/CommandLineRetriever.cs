namespace UpdateWatcher.Retrievers;

public class CommandLineRetriever : IVersionRetriever
{
    public string Command { get; set; } = string.Empty;
    
    public Version GetVersion()
    {
        
    }
}