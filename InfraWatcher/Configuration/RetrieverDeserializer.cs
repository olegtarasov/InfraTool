using InfraWatcher.Retrievers;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace InfraWatcher.Configuration;

public class RetrieverDeserializer : INodeDeserializer
{
    public bool Deserialize(
        IParser reader,
        Type expectedType,
        Func<IParser, Type, object?> nestedObjectDeserializer,
        out object? value,
        ObjectDeserializer rootDeserializer)
    {
        if (expectedType != typeof(ILinesRetriever))
        {
            value = null;
            return false;
        }

        if (!reader.TryConsume<Scalar>(out var scalar))
        {
            value = null;
            return false;
        }

        value = scalar.Value switch
                {
                    "cur_time" => new CurTimeRetriever(),
                    _ => null
                };

        return value != null;
    }
}