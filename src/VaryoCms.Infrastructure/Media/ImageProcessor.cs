using VaryoCms.Application.Common;
using VaryoCms.Application.Interfaces;
using SkiaSharp;

namespace VaryoCms.Infrastructure.Media;

public class ImageProcessor : IImageProcessor
{
    public ImageDimensions? TryReadDimensions(Stream content)
    {
        try
        {
            // Copy to a MemoryStream that SkiaSharp can own and dispose freely.
            // Reset the original stream BEFORE handing the copy to SkiaSharp so
            // subsequent callers (e.g. file storage) can still read the original.
            using var copy = new MemoryStream();
            content.CopyTo(copy);
            copy.Position = 0;
            if (content.CanSeek) content.Position = 0;

            using SKCodec? codec = SKCodec.Create(copy);
            if (codec is null) return null;
            SKImageInfo info = codec.Info;
            return new ImageDimensions(info.Width, info.Height);
        }
        catch
        {
            return null;
        }
    }

    public CroppedImage Crop(Stream input, int x, int y, int width, int height)
    {
        // Buffer once: SkiaSharp needs the stream for format detection and then again for decoding.
        using var buffer = new MemoryStream();
        input.CopyTo(buffer);
        byte[] bytes = buffer.ToArray();

        SKEncodedImageFormat format = DetectFormat(bytes);

        using SKBitmap? source = SKBitmap.Decode(bytes);
        if (source is null) throw new InvalidOperationException("Could not decode image.");

        // Clamp crop rectangle to bitmap bounds.
        x = Math.Clamp(x, 0, source.Width - 1);
        y = Math.Clamp(y, 0, source.Height - 1);
        width = Math.Clamp(width, 1, source.Width - x);
        height = Math.Clamp(height, 1, source.Height - y);

        using var dest = new SKBitmap(source.Info.WithSize(width, height));
        using (var canvas = new SKCanvas(dest))
            canvas.DrawBitmap(source,
                SKRect.Create(x, y, width, height),
                SKRect.Create(0, 0, width, height));

        using SKImage image = SKImage.FromBitmap(dest);
        int quality = format == SKEncodedImageFormat.Jpeg ? 90 : 100;
        using SKData encoded = image.Encode(format, quality);
        return new CroppedImage(encoded.ToArray(), width, height);
    }

    private static SKEncodedImageFormat DetectFormat(byte[] bytes)
    {
        using var ms = new MemoryStream(bytes, writable: false);
        using SKCodec? codec = SKCodec.Create(ms);
        return codec?.EncodedFormat ?? SKEncodedImageFormat.Jpeg;
    }
}
