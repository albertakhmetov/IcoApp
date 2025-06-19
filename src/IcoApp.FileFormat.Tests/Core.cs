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
namespace IcoApp.FileFormat.Tests;

using System.Buffers;
using System.Collections.Immutable;
using System.Drawing;
using Windows.Win32;
using Windows.Win32.Foundation;

internal static class Core
{
    private const int ICON_DIR_SIZE = 6;
    private const int ICON_DIR_ENTRY_SIZE = 16;

    public static string GetFullPath(string fileName)
    {
        return $"../../testdata/{fileName}";
    }

    public static Stream OpenFile(string fileName)
    {
        return File.OpenRead(GetFullPath(fileName));
    }

    public static void Compare(string expectedFileName, ImmutableArray<byte> actualImageData)
    {
        using var actualImageStream = new MemoryStream(actualImageData.ToArray());

        Compare(expectedFileName, actualImageStream);
    }

    public static void Compare(string expectedFileName, Stream actualImageStream)
    {
        using var expectedImage = Image.FromFile(GetFullPath(expectedFileName)) as Bitmap;
        using var actualImage = Image.FromStream(actualImageStream) as Bitmap;

        Assert.NotNull(expectedImage);
        Assert.NotNull(actualImage);

        Compare(expectedImage, actualImage);
    }

    public static void Compare(string expectedFileName, Bitmap actualImage)
    {
        using var expectedImage = Image.FromFile(GetFullPath(expectedFileName)) as Bitmap;

        Assert.NotNull(expectedImage);

        Compare(expectedImage, actualImage);
    }

    public static void Compare(Stream expectedImageStream, Bitmap actualImage)
    {
        using var expectedImage = Image.FromStream(expectedImageStream) as Bitmap;

        Assert.NotNull(expectedImage);

        Compare(expectedImage, actualImage);
    }

    public static void Compare(Bitmap expectedImage, Bitmap actualImage)
    {
        Assert.Equal(expectedImage.Width, actualImage.Width);
        Assert.Equal(expectedImage.Height, actualImage.Height);
        Assert.Equal(expectedImage.PixelFormat, actualImage.PixelFormat);

        for (var y = 0; y < expectedImage.Height; y++)
        {
            for (var x = 0; x < expectedImage.Width; x++)
            {
                Color expectedColor = expectedImage.GetPixel(x, y);
                Color actualColor = actualImage.GetPixel(x, y);

                Assert.Equal(expectedColor, actualColor);
            }
        }
    }

    public static void Compare(string expectedFileName, IReadOnlyList<uint> pixels)
    {
        using var expectedImage = Image.FromFile(GetFullPath(expectedFileName)) as Bitmap;

        Assert.NotNull(expectedImage);

        Compare(expectedImage, pixels);
    }

    public static void Compare(Bitmap expectedImage, IReadOnlyList<uint> pixels)
    {
        Assert.Equal(expectedImage.Width * expectedImage.Height, pixels.Count);

        for (var y = 0; y < expectedImage.Height; y++)
        {
            for (var x = 0; x < expectedImage.Width; x++)
            {
                Color expectedColor = expectedImage.GetPixel(x, y);
                Color actualColor = Color.FromArgb(unchecked((int)pixels[y * expectedImage.Width + x]));

                Assert.Equal(expectedColor, actualColor);
            }
        }
    }

    public static void Compare(Span<byte> expected, Span<byte> actual)
    {
        Assert.Equal(expected.Length, actual.Length);

        for (var i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], actual[i]);
        }
    }

    public static void CompareFrames(string expectedFileName, Span<byte> actual)
    {
        using var expectedImage = Image.FromFile(GetFullPath(expectedFileName)) as Bitmap;
        using var actualImage = GetFrame(actual);

        Assert.NotNull(expectedImage);

        Compare(expectedImage, actualImage);
    }

    public static int GetFramesCount(string fileName)
    {
        using var stream = File.OpenRead(GetFullPath(fileName));

        return GetFramesCount(stream);
    }

    public static int GetFramesCount(Stream stream)
    {
        var buffer = new Span<byte>(new byte[ICON_DIR_SIZE]);
        stream.ReadExactly(buffer);

        return GetFramesCount(buffer);
    }

    public static Bitmap GetFrame(string fileName, int frameId)
    {
        using var stream = File.OpenRead(GetFullPath(fileName));

        return GetFrame(stream, frameId);
    }

    public static Bitmap GetFrame(Stream stream, int frameId)
    {
        Assert.InRange(stream.Length, 0, int.MaxValue);
        var length = (int)stream.Length;

        var buffer = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            stream.ReadExactly(buffer.AsSpan(0, length));

            return GetFrame(buffer, frameId);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static int GetFramesCount(Span<byte> buffer)
    {
        Assert.Equal(0, BitConverter.ToUInt16(buffer.Slice(0, 2)));
        Assert.Equal(1, BitConverter.ToUInt16(buffer.Slice(2, 2)));

        return BitConverter.ToUInt16(buffer.Slice(4, 2));
    }

    private static Bitmap GetFrame(Span<byte> buffer, int frameId)
    {
        Assert.InRange(frameId, 0, GetFramesCount(buffer.Slice(0, ICON_DIR_SIZE)));

        var entryBuffer = buffer
            .Slice(ICON_DIR_SIZE)
            .Slice(ICON_DIR_ENTRY_SIZE * frameId, ICON_DIR_ENTRY_SIZE);

        int pos = BitConverter.ToInt32(entryBuffer.Slice(12, 4));
        int size = BitConverter.ToInt32(entryBuffer.Slice(8, 4));

        return GetFrame(buffer.Slice(pos, size));
    }

    private static Bitmap GetFrame(Span<byte> buffer)
    {
        using var handle = PInvoke.CreateIconFromResourceEx(buffer, new BOOL(true), 0x00030000, 0, 0, 0);

        Assert.False(handle.IsInvalid, "Error while loading an icon");

        using var icon = Icon.FromHandle(handle.DangerousGetHandle());

        return icon.ToBitmap();
    }
}
