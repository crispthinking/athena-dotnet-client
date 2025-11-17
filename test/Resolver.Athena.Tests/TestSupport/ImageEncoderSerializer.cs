using Resolver.Athena.Tests.TestSupport;
using SixLabors.ImageSharp.Formats;
using Xunit.Sdk;

[assembly: RegisterXunitSerializer(typeof(ImageEncoderSerializer), typeof(IImageEncoder))]

namespace Resolver.Athena.Tests.TestSupport;

public class ImageEncoderSerializer : IXunitSerializer
{
    public object Deserialize(Type type, string serializedValue)
    {
        var t = type.Assembly.GetType(serializedValue) ?? throw new InvalidOperationException($"Type '{serializedValue}' not found in assembly '{type.Assembly.FullName}'");
        return Activator.CreateInstance(t) ?? throw new InvalidOperationException($"Could not create instance of type '{serializedValue}'");
    }

    public bool IsSerializable(Type type, object? value, out string failureReason)
    {
        if (typeof(IImageEncoder).IsAssignableFrom(type))
        {
            failureReason = string.Empty;
            return true;
        }

        failureReason = $"Type '{type.FullName}' does not implement IImageEncoder.";
        return false;
    }

    public string Serialize(object value)
    {
        return value.GetType().FullName ?? throw new InvalidOperationException("Type must have a name");
    }
}
