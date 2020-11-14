using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections.Generic;

namespace AbraCADabra
{
    public static class ImageIO
    {
        public static (byte[] pixels, int width, int height) LoadImageBytes(string path)
        {
            Image<Rgba32> img = Image.Load<Rgba32>(path);
            img.TryGetSinglePixelSpan(out System.Span<Rgba32> span);
            var tempPixels = span.ToArray();

            var pixels = new List<byte>();
            foreach (Rgba32 p in tempPixels)
            {
                pixels.Add(p.R);
                pixels.Add(p.G);
                pixels.Add(p.B);
                pixels.Add(p.A);
            }
            return (pixels.ToArray(), img.Width, img.Height);
        }

        public static void SaveGrayscalePng(float[,] pixels, string path, float valScale = 1)
        {
            int width = pixels.GetLength(0), height = pixels.GetLength(1);
            var tmpimg = new Image<Rgba32>(width, height);
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    float val = pixels[i, j] * valScale;
                    tmpimg[i, j] = new Rgba32(val, val, val);
                }
            }
            tmpimg.SaveAsPng(path);
        }
    }
}
