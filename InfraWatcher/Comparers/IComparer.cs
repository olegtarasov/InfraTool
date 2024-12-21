namespace InfraWatcher.Comparers;

public interface IComparer
{
    string Compare(string? actual, string? expected);
}