using InfraWatcher.Helpers;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace InfraWatcher.Configuration;

public class TimeSpanDeserializer : INodeDeserializer
{
    public bool Deserialize(
        IParser reader,
        Type expectedType,
        Func<IParser, Type, object?> nestedObjectDeserializer,
        out object? value,
        ObjectDeserializer rootDeserializer)
    {
        if (expectedType != typeof(TimeSpan))
        {
            value = null;
            return false;
        }


        if (reader.TryConsume<Scalar>(out var scalar))
        {
            try
            {
                value = DateTimeHelper.FromShortTimeSpanString(scalar.Value);
                return true;
            }
            catch
            {
            }
        }
        
        value = null;
        return false;
    }
}