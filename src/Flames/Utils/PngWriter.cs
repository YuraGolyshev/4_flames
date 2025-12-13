using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;

namespace Flames.Utils;

public static class PngWriter
{
    /// <summary>
    /// Сохраняет RGB-изображение (интерлейсированное), размером width*height, по пути path с помощью SixLabors.ImageSharp
    /// </summary>
    public static void SaveRgbImage(string path, int width, int height, byte[] rgbBuffer)
    {
        using var img = new Image<Rgb24>(width, height);
        int stride = width * 3;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int idx = (y * width + x) * 3;
                img[x, y] = new Rgb24(rgbBuffer[idx], rgbBuffer[idx + 1], rgbBuffer[idx + 2]);
            }
        }
        img.Save(path, new PngEncoder { ColorType = PngColorType.Rgb });
    }
}
