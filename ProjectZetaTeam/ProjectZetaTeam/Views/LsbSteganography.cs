using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ProjectZetaTeam.Views
{
    public static class LsbSteganography
    {
        public static void HideText(string inputPath, string outputPath, string secretText)
        {
            if (string.IsNullOrEmpty(inputPath))
                throw new ArgumentNullException(nameof(inputPath));
            if (string.IsNullOrEmpty(outputPath))
                throw new ArgumentNullException(nameof(outputPath));
            if (secretText == null)
                throw new ArgumentNullException(nameof(secretText));


            byte[] textBytes = Encoding.UTF8.GetBytes(secretText + "\0");

            BitArray bits = new BitArray(textBytes);
            int bitIndex = 0;

            using Image<Rgba32> image = Image.Load<Rgba32>(inputPath);

            long totalPixels = (long)image.Width * image.Height;
            if (bits.Length > totalPixels * 3)
            {
                throw new InvalidOperationException("Текст слишком большой для этого изображения!");
            }

            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    Span<Rgba32> row = accessor.GetRowSpan(y);

                    for (int x = 0; x < row.Length; x++)
                    {
                        if (bitIndex >= bits.Length) return;

                        ref Rgba32 pixel = ref row[x];

                        pixel.R = (byte)((pixel.R & 0xFE) | (bits[bitIndex++] ? 1 : 0));

                        if (bitIndex < bits.Length)
                            pixel.G = (byte)((pixel.G & 0xFE) | (bits[bitIndex++] ? 1 : 0));

                        if (bitIndex < bits.Length)
                            pixel.B = (byte)((pixel.B & 0xFE) | (bits[bitIndex++] ? 1 : 0));
                    }
                }
            });

            var pngEncoder = new PngEncoder();
            image.Save(outputPath, pngEncoder);
        }

        public static string ExtractText(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
                throw new ArgumentNullException(nameof(imagePath));

            using Image<Rgba32> image = Image.Load<Rgba32>(imagePath);

            List<byte> extractedBytes = new List<byte>();
            byte currentByte = 0;
            int bitPosition = 0;
            bool messageCompleted = false;

            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height && !messageCompleted; y++)
                {
                    Span<Rgba32> row = accessor.GetRowSpan(y);
                    for (int x = 0; x < row.Length && !messageCompleted; x++)
                    {
                        Rgba32 pixel = row[x];

                        for (int channel = 0; channel < 3 && !messageCompleted; channel++)
                        {
                            int bitValue = 0;
                            switch (channel)
                            {
                                case 0: bitValue = pixel.R & 1; break;
                                case 1: bitValue = pixel.G & 1; break;
                                case 2: bitValue = pixel.B & 1; break;
                            }

                            if (bitValue == 1)
                                currentByte |= (byte)(1 << bitPosition);

                            bitPosition++;

                            if (bitPosition == 8)
                            {
                                extractedBytes.Add(currentByte);

                                if (currentByte == 0)
                                {
                                    messageCompleted = true;
                                    break;
                                }

                                currentByte = 0;
                                bitPosition = 0;
                            }
                        }
                    }
                }
            });

            if (!messageCompleted && extractedBytes.Count > 0 && extractedBytes[^1] != 0)
            {
                extractedBytes.RemoveAt(extractedBytes.Count - 1);
            }

            string result = Encoding.UTF8.GetString(extractedBytes.ToArray());
            return result.TrimEnd('\0');
        }
    }
}
