/*  Copyright © 2025, Albert Akhmetov <akhmetov@live.com>   
 *
 *  This file is part of IcoApp.
 *
 *  IcoApp is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  IcoApp is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with IcoApp. If not, see <https://www.gnu.org/licenses/>.   
 *
 */
namespace IcoApp.Core.FileFormat;

using System.Buffers;
using System.Collections.Immutable;
using IcoApp.Core.Helpers;
using IcoApp.Core.FileFormat.Internal;

public class IcoBitmapFrame : IcoFrame
{
    public static IcoBitmapFrame CreateFromImages(Stream imageStream, Stream? maskImageStream = null)
    {
        ArgumentNullException.ThrowIfNull(imageStream);

        var length = imageStream.Length.ToInt32();

        var imageBuffer = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            imageBuffer.AsSpan().Clear();

            imageStream.ReadExactly(imageBuffer.AsSpan(0, length));

            if (!Bitmap.IsSupported(imageBuffer.AsSpan(), length))
            {
                throw new ArgumentException();
            }

            Bitmap.ParseSize(imageBuffer, out _, out _, out var bitCount);
            if (bitCount < 32 && maskImageStream == null)
            {
                throw new ArgumentNullException();
            }

            var originalImage = Bitmap.FromFile(imageBuffer.AsSpan(0, length));
            var maskImage = maskImageStream == null ? GenerateMask(originalImage) : Bitmap.FromFile(maskImageStream);

            return new IcoBitmapFrame(originalImage, maskImage);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(imageBuffer);
        }
    }

    public static IcoBitmapFrame LoadFromIcoEntry(Span<byte> entryBuffer)
    {
        const int BITMAP_INFO_HEADER_SIZE = 40;
        const int BITMAP_HEADER_WIDTH = 0x04;
        const int BITMAP_HEADER_HEIGHT = 0x08;
        const int BITMAP_HEADER_BIT_COUNT = 0x0e;
        const int BITMAP_HEADER_COLORS = 0x20;

        var width = BitConverter.ToInt32(entryBuffer[BITMAP_HEADER_WIDTH..]);
        var height = BitConverter.ToInt32(entryBuffer[BITMAP_HEADER_HEIGHT..]) / 2;
        var bitCount = BitConverter.ToInt32(entryBuffer[BITMAP_HEADER_BIT_COUNT..]);

        var colorCount = BitConverter.ToInt32(entryBuffer.Slice(BITMAP_HEADER_COLORS, sizeof(int)));
        if (colorCount == 0)
        {
            colorCount = bitCount <= 8 ? (1 << bitCount) : 0;
        }

        var colorTableLength = colorCount * sizeof(uint);
        var pixelBufferLength = Bitmap.GetStride(width, bitCount) * height;
        var maskPixelBufferLength = Bitmap.GetStride(width, 1) * height;

        var colorTable = Bitmap.ParseColorTable(entryBuffer.Slice(BITMAP_INFO_HEADER_SIZE, colorTableLength));
        var pixelBuffer = entryBuffer.Slice(BITMAP_INFO_HEADER_SIZE + colorTableLength);
        var bitmaskBuffer = entryBuffer.Slice(BITMAP_INFO_HEADER_SIZE + colorTableLength + pixelBufferLength);

        var originalImage = new Bitmap(
            width,
            height,
            bitCount,
            entryBuffer.Slice(BITMAP_INFO_HEADER_SIZE + colorTableLength),
            colorTable);
        var maskImage = new Bitmap(
            width,
            height,
            1,
            entryBuffer.Slice(BITMAP_INFO_HEADER_SIZE + colorTableLength + pixelBufferLength),
            [0x000000, 0xFFFFFF]);

        return new IcoBitmapFrame(originalImage, maskImage);
    }

    private Bitmap image, originalImage, maskImage;

    private IcoBitmapFrame(Bitmap originalImage, Bitmap maskImage)
    {
        ArgumentNullException.ThrowIfNull(originalImage);
        ArgumentNullException.ThrowIfNull(maskImage);

        this.originalImage = originalImage;
        this.maskImage = maskImage;

        ImageData = GenerateImage(originalImage, maskImage);
        OriginalImageData = GetImageData(originalImage);
        MaskImageData = GetImageData(maskImage);
    }

    public override int Width => originalImage.Width;

    public override int Height => originalImage.Height;

    public int BitCount => originalImage.BitCount;

    public override int FrameLength => originalImage.GetLength(false) + Bitmap.GetStride(Width, 1) * Height;

    public override ImmutableArray<byte> ImageData { get; }

    public ImmutableArray<byte> OriginalImageData { get; }

    public ImmutableArray<byte> MaskImageData { get; }

    public override void SaveFrame(Span<byte> buffer)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(buffer.Length, FrameLength);

        originalImage.Save(buffer, false);
        maskImage.SaveBitmask(buffer.Slice(originalImage.GetLength(false)), x => (x & 0xFFFFFF) != 0xFFFFFF);

        const int BITMAP_HEADER_HEIGHT = 0x08;
        const int BITMAP_HEADER_IMAGE_SIZE = 0x14;

        var pixelBufferLength =
            originalImage.ColorTableLength +
            Bitmap.GetStride(originalImage.Width, originalImage.BitCount) * Height +
            Bitmap.GetStride(originalImage.Width, 1) * Height;

        BitConverter.TryWriteBytes(buffer.Slice(BITMAP_HEADER_HEIGHT, sizeof(int)), originalImage.Height * 2).IsTrue();
        BitConverter.TryWriteBytes(buffer.Slice(BITMAP_HEADER_IMAGE_SIZE, sizeof(int)), pixelBufferLength).IsTrue();
    }

    private static ImmutableArray<byte> GetImageData(Bitmap image)
    {
        var imageData = new byte[image.GetLength()];
        image.Save(imageData);

        return ImmutableArray.Create<byte>(imageData);
    }

    private static ImmutableArray<byte> GenerateImage(Bitmap image, Bitmap maskImage)
    {
        Bitmap? bitmap = null;

        if (image.BitCount == 32)
        {
            bitmap = image;
        }
        else
        {
            var pixels = new uint[image.Pixels.Count];

            var originalPixels = image.Pixels;
            var maskPixels = maskImage.Pixels;

            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = (maskPixels[i] & 0xFFFFFF) == 0xFFFFFF ? 0 : originalPixels[i];
            }

            bitmap = new Bitmap(image.Width, image.Height, 32, pixels);
        }

        return GetImageData(bitmap);
    }

    private static Bitmap GenerateMask(Bitmap image)
    {
        var pixels = new uint[image.Width * image.Height];

        var pos = 0;
        foreach (var p in image.Pixels)
        {
            pixels[pos++] = ((p & 0xFF000000) >> 24) < 0xFF ? 0xFFFFFFFF : 0xFF000000;
        }

        return new Bitmap(image.Width, image.Height, 1, pixels);
    }
}
