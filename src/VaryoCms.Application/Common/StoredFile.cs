namespace VaryoCms.Application.Common;

// Result of persisting an uploaded file to storage.
public record StoredFile(string FileName, string RelativePath, long SizeBytes);

// Pixel dimensions of an image.
public record ImageDimensions(int Width, int Height);

// Result of a server-side crop: the new image bytes and its dimensions.
public record CroppedImage(byte[] Bytes, int Width, int Height);
