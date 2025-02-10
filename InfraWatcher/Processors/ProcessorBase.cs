namespace InfraWatcher.Processors;

public abstract class ProcessorBase : IProcessor
{
    public OrderType Order { get; set; } = OrderType.None;
    public int? Take { get; set; } = null;
    public int? Skip { get; set; } = null;
    
    public abstract string[] Process(string[] lines);

    protected IEnumerable<T> ApplyFilters<T>(IEnumerable<T> source)
    {
        var result = source;
        if (Order != OrderType.None)
            result = Order == OrderType.Ascending ? result.Order() : result.OrderDescending();

        if (Skip > 0)
            result = result.Skip(Skip.Value);

        if (Take > 0)
            result = result.Take(Take.Value);

        return result;
    }
}