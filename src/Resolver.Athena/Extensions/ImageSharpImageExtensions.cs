using Resolver.Athena.Grpc;
using SixLabors.ImageSharp;

namespace Resolver.Athena.Extensions;

public static class ImageSharpImageExtensions
{
    public static ImageFormat ToAthenaImageFormat(this Image image)
    {
        var format = image?.Metadata?.DecodedImageFormat?.Name.ToLowerInvariant();
        return format switch
        {
            "jpeg" or "jpg" => ImageFormat.Jpeg,
            "png" => ImageFormat.Png,
            "bmp" => ImageFormat.Bmp,
            "gif" => ImageFormat.Gif,
            "tiff" => ImageFormat.Tiff,
            _ => throw new NotSupportedException($"Image format '{format}' is not supported."),
        };
    }
}
