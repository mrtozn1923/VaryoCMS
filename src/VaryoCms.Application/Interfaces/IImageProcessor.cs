using VaryoCms.Application.Common;

namespace VaryoCms.Application.Interfaces;

// Inspects/transforms images. Implemented by Infrastructure (SixLabors.ImageSharp).
public interface IImageProcessor
{
    // Returns the image's dimensions, or null if the stream is not a recognised image.
    // Does not dispose the stream; resets position to 0 before returning.
    ImageDimensions? TryReadDimensions(Stream content);

    // Crops the image to the given rectangle (clamped to bounds), preserving format. Throws on a non-image stream.
    CroppedImage Crop(Stream input, int x, int y, int width, int height);
}
