using InfraTool.Comparers;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace InfraTool.Configuration;

public class ComparerDeserializer : INodeDeserializer
{
    public bool Deserialize(
        IParser reader,
        Type expectedType,
        Func<IParser, Type, object?> nestedObjectDeserializer,
        out object? value,
        ObjectDeserializer rootDeserializer)
    {
        if (expectedType != typeof(IComparer))
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
                    "version" => new VersionComparer(),
                    _ => null
                };

        return value != null;
    }
}