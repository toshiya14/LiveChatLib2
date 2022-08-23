using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace LiveChatLib2.Utils;

internal static class ImageHelper
{
    /// <summary>
    /// Resize the image to the specified width and height.
    /// </summary>
    /// <param name="image">The image to resize.</param>
    /// <param name="width">The width to resize to.</param>
    /// <param name="height">The height to resize to.</param>
    /// <returns>The resized image.</returns>
    public static Image ResizeImage(Image image, int width, int height)
    {
        var output = image.Clone(x=>x.Resize(new ResizeOptions
        {
            Size = new SixLabors.ImageSharp.Size(width, height),
            Mode = ResizeMode.Stretch
        }));
        return output;
    }

    /// <summary>
    /// Convert byte image to base64 string.
    /// </summary>
    /// <param name="facedata"></param>
    /// <returns></returns>
    public static string ConvertToJpegBase64(byte[] facedata)
    {
        var bitmap = ResizeImage(Image.Load(facedata), 64, 64);
        var result = bitmap.ToBase64String(SixLabors.ImageSharp.Formats.Jpeg.JpegFormat.Instance);
        return result;
    }
}
