namespace InfraWatcher.Shell;

/// <summary>
/// Output line type
/// </summary>
public enum OutputType
{
    /// <summary>
    /// stdout
    /// </summary>
    Output,
        
    /// <summary>
    /// stderr
    /// </summary>
    Error
}

/// <summary>
/// Output line with type.
/// </summary>
/// <param name="Type">Stream type.</param>
/// <param name="Text">Text.</param>
public record OutputLine(OutputType Type, string Text)
{
    /// <summary>
    /// Returns line text
    /// </summary>
    public static implicit operator string(OutputLine line) => line.Text;
}