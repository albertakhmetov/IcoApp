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
namespace IcoApp.FileFormat.Tests.Internal;

#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.

using System;
using System.Buffers;
using IcoApp.FileFormat.Internal;

public class BitmapTests
{
    private const int FILE_HEADER_SIZE = 0x0e;
    private const int FILE_HEADER_PIXELS = 0x0a;

    private const int BITMAP_HEADER_BIT_COUNT = 0x1c;
    private const int BITMAP_HEADER_COLORS = 0x2e;

    private const int BITMAP_INFO_HEADER_SIZE = 40;
    private const int BITMAP_V5_HEADER_SIZE = 124;

    [Theory]
    [InlineData("rectangular/image-1.bmp")]
    [InlineData("rectangular/image-4.bmp")]
    [InlineData("rectangular/image-8.bmp")]
    [InlineData("rectangular/image-24.bmp")]
    [InlineData("rectangular/image-32.bmp")]
    public void IsSupported_Positive(string fileName)
    {
        using var stream = Core.OpenFile(fileName);

        var buffer = new byte[FILE_HEADER_SIZE + BITMAP_V5_HEADER_SIZE];
        stream.ReadExactly(buffer);

        Assert.True(Bitmap.IsSupported(buffer, stream.Length.ToInt32()));
    }

    [Fact]
    public void IsSuported_Negative()
    {
        using var stream = Core.OpenFile("rectangular/image.png");

        var buffer = new byte[FILE_HEADER_SIZE + BITMAP_V5_HEADER_SIZE];
        stream.ReadExactly(buffer);
        
        Assert.False(Bitmap.IsSupported(buffer, stream.Length.ToInt32()));
    }

    [Theory]
    [InlineData("rectangular/image-1.bmp", 1)]
    [InlineData("rectangular/image-4.bmp", 4)]
    [InlineData("rectangular/image-8.bmp", 8)]
    [InlineData("rectangular/image-24.bmp", 24)]
    [InlineData("rectangular/image-32.bmp", 32)]
    public void ParseSize_WithFileHeader(string fileName, int expectedBitCount)
    {
        using var stream = Core.OpenFile(fileName);

        var buffer = new byte[FILE_HEADER_SIZE + BITMAP_INFO_HEADER_SIZE];
        stream.ReadExactly(buffer);

        Bitmap.ParseSize(buffer, out var width, out var height, out var bitCount);

        Assert.Equal(96, width);
        Assert.Equal(64, height);
        Assert.Equal(expectedBitCount, bitCount);
    }

    [Theory]
    [InlineData("rectangular/image-1.bmp", 1)]
    [InlineData("rectangular/image-4.bmp", 4)]
    [InlineData("rectangular/image-8.bmp", 8)]
    [InlineData("rectangular/image-24.bmp", 24)]
    [InlineData("rectangular/image-32.bmp", 32)]
    public void ParseSize_WithoutFileHeader(string fileName, int expectedBitCount)
    {
        using var stream = Core.OpenFile(fileName);

        var buffer = new byte[FILE_HEADER_SIZE + BITMAP_INFO_HEADER_SIZE];
        stream.ReadExactly(buffer);

        Bitmap.ParseSize(buffer.AsSpan(FILE_HEADER_SIZE), out var width, out var height, out var bitCount);

        Assert.Equal(96, width);
        Assert.Equal(64, height);
        Assert.Equal(expectedBitCount, bitCount);
    }

    [Fact]
    public void FromFile_NullStream()
    {
        Assert.Throws<ArgumentNullException>(() => { Bitmap.FromFile(null as Stream); });
    }

    [Fact]
    public void FromFile_InvalidData()
    {
        Assert.Throws<InvalidDataException>(() => { Bitmap.FromFile(Core.OpenFile("png/image64.png")); });
    }

    [Theory]
    [InlineData("rectangular/image-1.bmp", 1)]
    [InlineData("rectangular/image-4.bmp", 4)]
    [InlineData("rectangular/image-8.bmp", 8)]
    [InlineData("rectangular/image-24.bmp", 24)]
    [InlineData("rectangular/image-32.bmp", 32)]
    public void FromFile_Metadata(string fileName, int bitCount)
    {
        using var stream = Core.OpenFile(fileName);

        var bitmap = Bitmap.FromFile(stream);

        Assert.Equal(96, bitmap.Width);
        Assert.Equal(64, bitmap.Height);
        Assert.Equal(bitCount, bitmap.BitCount);
    }

    [Theory]
    [InlineData("rectangular/image-1.bmp")]
    [InlineData("rectangular/image-4.bmp")]
    [InlineData("rectangular/image-8.bmp")]
    [InlineData("rectangular/image-24.bmp")]
    [InlineData("rectangular/image-32.bmp")]
    public void FromFile_Pixels(string fileName)
    {
        using var stream = Core.OpenFile(fileName);

        var bitmap = Bitmap.FromFile(stream);

        Core.Compare(fileName, bitmap.Pixels);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10)]
    public void Buffer_OutOfRangeArgument(int pixelBufferPosition)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Bitmap(new byte[10], pixelBufferPosition));
    }

    [Theory]
    [InlineData("rectangular/image-24.bmp", 24)]
    [InlineData("rectangular/image-32.bmp", 32)]
    public void Buffer_NonIndexedBitmaps(string fileName, int bitCount)
    {
        using var sourceFile = Core.OpenFile(fileName);

        var buffer = ArrayPool<byte>.Shared.Rent((int)sourceFile.Length);
        sourceFile.ReadExactly(buffer.AsSpan(0, (int)sourceFile.Length));

        try
        {
            var pixelBufferPosition = BitConverter.ToInt32(buffer.AsSpan(FILE_HEADER_PIXELS, sizeof(int)));

            // Skip the file header
            var bitmap = new Bitmap(buffer.AsSpan(FILE_HEADER_SIZE), pixelBufferPosition - FILE_HEADER_SIZE);

            Assert.Equal(96, bitmap.Width);
            Assert.Equal(64, bitmap.Height);
            Assert.Equal(bitCount, bitmap.BitCount);

            Core.Compare(fileName, bitmap.Pixels);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    [Theory]
    [InlineData("rectangular/image-1.bmp", 1)]
    [InlineData("rectangular/image-4.bmp", 4)]
    [InlineData("rectangular/image-8.bmp", 8)]
    public void Buffer_IndexedBitmaps(string fileName, int bitCount)
    {
        using var sourceFile = Core.OpenFile(fileName);

        var buffer = ArrayPool<byte>.Shared.Rent((int)sourceFile.Length);
        sourceFile.ReadExactly(buffer.AsSpan(0, (int)sourceFile.Length));

        try
        {
            var pixelBufferPosition = BitConverter.ToInt32(buffer.AsSpan(FILE_HEADER_PIXELS, sizeof(int)));

            // Skip the file header
            var bitmap = new Bitmap(buffer.AsSpan(FILE_HEADER_SIZE), pixelBufferPosition - FILE_HEADER_SIZE);

            Assert.Equal(96, bitmap.Width);
            Assert.Equal(64, bitmap.Height);
            Assert.Equal(bitCount, bitmap.BitCount);

            Core.Compare(fileName, bitmap.Pixels);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    [Theory]
    [InlineData(-1, 1, 24)]
    [InlineData(1, -1, 24)]
    [InlineData(0, 1, 24)]
    [InlineData(1, 0, 24)]
    [InlineData(1, 1, 0)]
    [InlineData(1, 1, 25)]
    [InlineData(1, 1, 33)]
    public void PixelBuffer_OutOfRangeArgument(int width, int height, int bitCount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Bitmap(width, height, bitCount, new byte[1]));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(8)]
    public void PixelBuffer_ColorTableIsRequiredForIndexedBitmaps(int bitCount)
    {
        Assert.Throws<ArgumentNullException>(() => new Bitmap(16, 16, bitCount, new byte[1]));
    }

    [Theory]
    [InlineData("rectangular/image-24.bmp", 24)]
    [InlineData("rectangular/image-32.bmp", 32)]
    public void PixelBuffer_NonIndexedBitmaps(string fileName, int expectedBitCount)
    {
        using var sourceFile = Core.OpenFile(fileName);

        var buffer = ArrayPool<byte>.Shared.Rent((int)sourceFile.Length);
        sourceFile.ReadExactly(buffer.AsSpan(0, (int)sourceFile.Length));

        try
        {
            var pixelBufferPosition = BitConverter.ToInt32(buffer.AsSpan(FILE_HEADER_PIXELS, sizeof(int)));

            var bitmap = new Bitmap(96, 64, expectedBitCount, buffer.AsSpan(pixelBufferPosition));

            Assert.Equal(96, bitmap.Width);
            Assert.Equal(64, bitmap.Height);
            Assert.Equal(expectedBitCount, bitmap.BitCount);
            Assert.Empty(bitmap.ColorTable);
            Assert.Equal(0, bitmap.ColorTableLength);

            var expectedLength =
                FILE_HEADER_SIZE + // file header size
                BITMAP_V5_HEADER_SIZE + // bitmap header size
                bitmap.ColorTableLength + // color table size
                Bitmap.GetStride(96, expectedBitCount) * 64; // pixel area size

            var expectedShortLength =
                BITMAP_INFO_HEADER_SIZE + // bitmap header size
                bitmap.ColorTableLength + // color table size
                Bitmap.GetStride(96, expectedBitCount) * 64; // pixel area size

            Assert.Equal(expectedLength, bitmap.GetLength());
            Assert.Equal(expectedShortLength, bitmap.GetLength(withFileHeader: false));

            Core.Compare(fileName, bitmap.Pixels);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    [Theory]
    [InlineData("rectangular/image-1.bmp", 1)]
    [InlineData("rectangular/image-4.bmp", 4)]
    [InlineData("rectangular/image-8.bmp", 8)]
    public void PixelBuffer_IndexedBitmaps(string fileName, int expectedBitCount)
    {
        using var sourceFile = Core.OpenFile(fileName);

        var buffer = ArrayPool<byte>.Shared.Rent((int)sourceFile.Length);
        sourceFile.ReadExactly(buffer.AsSpan(0, (int)sourceFile.Length));

        try
        {
            var pixelBufferPosition = BitConverter.ToInt32(buffer.AsSpan(FILE_HEADER_PIXELS, sizeof(int)));

            var colorTable = LoadColorTable(buffer, out var bitCount);

            var bitmap = new Bitmap(96, 64, bitCount, buffer.AsSpan(pixelBufferPosition), colorTable);

            Assert.Equal(96, bitmap.Width);
            Assert.Equal(64, bitmap.Height);
            Assert.Equal(expectedBitCount, bitmap.BitCount);
            Assert.Equal(colorTable.Count, bitmap.ColorTable.Count);
            Assert.Equal((1 << expectedBitCount) * sizeof(uint), bitmap.ColorTableLength);

            var expectedLength =
                FILE_HEADER_SIZE + // file header size
                BITMAP_INFO_HEADER_SIZE + // bitmap header size
                bitmap.ColorTableLength + // color table size
                Bitmap.GetStride(96, expectedBitCount) * 64; // pixel area size
            Assert.Equal(expectedLength, bitmap.GetLength());
            Assert.Equal(expectedLength - FILE_HEADER_SIZE, bitmap.GetLength(withFileHeader: false));

            Core.Compare(fileName, bitmap.Pixels);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private IList<uint> LoadColorTable(Span<byte> buffer, out int bitCount)
    {
        bitCount = BitConverter.ToInt16(buffer.Slice(BITMAP_HEADER_BIT_COUNT, sizeof(short)));

        var headerSize = BitConverter.ToInt32(buffer.Slice(FILE_HEADER_SIZE, sizeof(int)));
        var colorTableLength = BitConverter.ToInt32(buffer.Slice(BITMAP_HEADER_COLORS, sizeof(int)));
        if (colorTableLength == 0)
        {
            colorTableLength = (int)(1u << bitCount);
        }

        var pos = FILE_HEADER_SIZE + headerSize;

        var colorTable = new uint[colorTableLength];
        for (var i = 0; i < colorTable.Length; i++)
        {
            var b = buffer[pos++];
            var g = buffer[pos++];
            var r = buffer[pos++];
            var a = buffer[pos++] | 0xFF;

            colorTable[i] = (uint)
                (0xFFFFFFFF & (a << 24 | r << 16 | g << 8 | b));
        }

        return colorTable.AsReadOnly();
    }

    [Theory]
    [InlineData(-1, 1, 24)]
    [InlineData(1, -1, 24)]
    [InlineData(0, 1, 24)]
    [InlineData(1, 0, 24)]
    [InlineData(1, 1, 0)]
    [InlineData(1, 1, 25)]
    [InlineData(1, 1, 33)]
    public void Pixels_OutOfRangeArgument(int width, int height, int bitCount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Bitmap(width, height, bitCount, null!));
    }

    [Fact]
    public void Pixels_NullPixels()
    {
        Assert.Throws<ArgumentNullException>(() => new Bitmap(96, 64, 8, null!));
    }

    [Fact]
    public void Pixels_PixelsIsLess()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Bitmap(96, 64, 8, new uint[96 * 64 - 1]));
    }

    [Fact]
    public void Pixels_PixelsIsGreater()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Bitmap(96, 64, 8, new uint[96 * 64 + 1]));
    }

    [Theory]
    [InlineData("rectangular/image-24.bmp", 24)]
    [InlineData("rectangular/image-32.bmp", 32)]
    public void Pixels_NonIndexedBitmap(string fileName, int expectedBitCount)
    {
        var expectedBitmap = Bitmap.FromFile(Core.OpenFile(fileName));
        var expectedPixels = expectedBitmap.Pixels;

        var bitmap = new Bitmap(96, 64, expectedBitCount, expectedPixels);

        Assert.Equal(96, bitmap.Width);
        Assert.Equal(64, bitmap.Height);
        Assert.Equal(expectedBitCount, bitmap.BitCount);

        Assert.Empty(bitmap.ColorTable);
        Assert.Equal(0, bitmap.ColorTableLength);

        Assert.Equal(expectedPixels.Count, bitmap.Pixels.Count);
        for (var i = 0; i < expectedPixels.Count; i++)
        {
            Assert.Equal(expectedPixels[i], bitmap.Pixels[i]);
        }
    }

    [Theory]
    [InlineData("rectangular/image-1.bmp", 1)]
    [InlineData("rectangular/image-4.bmp", 4)]
    [InlineData("rectangular/image-8.bmp", 8)]
    public void Pixels_IndexedBitmap(string fileName, int expectedBitCount)
    {
        var expectedBitmap = Bitmap.FromFile(Core.OpenFile(fileName));
        var expectedPixels = expectedBitmap.Pixels;

        var bitmap = new Bitmap(96, 64, expectedBitCount, expectedPixels);

        Assert.Equal(96, bitmap.Width);
        Assert.Equal(64, bitmap.Height);
        Assert.Equal(expectedBitCount, bitmap.BitCount);
        Assert.Equal((1 << expectedBitCount) * sizeof(uint), bitmap.ColorTableLength);

        if (expectedBitCount == 1)
        {
            Assert.Equal(2, bitmap.ColorTable.Count);
            Assert.Contains(bitmap.ColorTable, x => (x & 0xFFFFFF) == 0xFFFFFF);
            Assert.Contains(bitmap.ColorTable, x => (x & 0xFFFFFF) == 0x2695D3);
        }
        else
        {
            Assert.Equal(3, bitmap.ColorTable.Count);
            Assert.Contains(bitmap.ColorTable, x => (x & 0xFFFFFF) == 0xFFFFFF);
            Assert.Contains(bitmap.ColorTable, x => (x & 0xFFFFFF) == 0x03A9F4);
            Assert.Contains(bitmap.ColorTable, x => (x & 0xFFFFFF) == 0xCE3636);
        }

        Assert.Equal(expectedPixels.Count, bitmap.Pixels.Count);
        for (var i = 0; i < expectedPixels.Count; i++)
        {
            Assert.Equal(expectedPixels[i], bitmap.Pixels[i]);
        }
    }

    [Theory]
    [InlineData("rectangular/image-1.bmp")]
    [InlineData("rectangular/image-4.bmp")]
    [InlineData("rectangular/image-8.bmp")]
    [InlineData("rectangular/image-24.bmp")]
    [InlineData("rectangular/image-32.bmp")]
    public void Save_SmallBuffer_WithFileHeader(string fileName)
    {
        var bitmap = Bitmap.FromFile(Core.OpenFile(fileName));

        var length = bitmap.GetLength();

        var buffer = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => bitmap.Save(buffer.AsSpan(0, length - 1)));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    [Theory]
    [InlineData("rectangular/image-1.bmp")]
    [InlineData("rectangular/image-4.bmp")]
    [InlineData("rectangular/image-8.bmp")]
    [InlineData("rectangular/image-24.bmp")]
    [InlineData("rectangular/image-32.bmp")]
    public void Save_SmallBuffer_WithoutFileHeader(string fileName)
    {
        var bitmap = Bitmap.FromFile(Core.OpenFile(fileName));

        var length = bitmap.GetLength(withFileHeader: false);

        var buffer = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => bitmap.Save(buffer.AsSpan(0, length - 1), writeFileHeader: false));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    [Theory]
    [InlineData("rectangular/image-1.bmp")]
    [InlineData("rectangular/image-4.bmp")]
    [InlineData("rectangular/image-8.bmp")]
    [InlineData("rectangular/image-24.bmp")]
    [InlineData("rectangular/image-32.bmp")]
    public void Save_WithFileHeader(string fileName)
    {
        var bitmap = Bitmap.FromFile(Core.OpenFile(fileName));

        var length = bitmap.GetLength();

        var buffer = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            buffer.AsSpan(0, length).Clear();
            bitmap.Save(buffer.AsSpan(0, length));

            using var stream = new MemoryStream(length);
            stream.Write(buffer.AsSpan(0, length));
            stream.Flush();
            stream.Position = 0;

            Core.Compare(fileName, stream);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    [Theory]
    [InlineData("rectangular/image-1.bmp")]
    [InlineData("rectangular/image-4.bmp")]
    [InlineData("rectangular/image-8.bmp")]
    [InlineData("rectangular/image-24.bmp")]
    [InlineData("rectangular/image-32.bmp")]
    public void Save_WithoutFileHeader(string fileName)
    {
        var bitmap = Bitmap.FromFile(Core.OpenFile(fileName));

        var lengthFull = bitmap.GetLength();
        var lengthShort = bitmap.GetLength(false);

        var bufferFull = ArrayPool<byte>.Shared.Rent(lengthFull);
        var bufferShort = ArrayPool<byte>.Shared.Rent(lengthShort);
        try
        {
            bufferFull.AsSpan(0, lengthFull).Clear();
            bitmap.Save(bufferFull.AsSpan(0, lengthFull));

            bufferShort.AsSpan(0, lengthShort).Clear();
            bitmap.Save(bufferShort.AsSpan(0, lengthShort), false);

            VerifyBitmapInfoHeader(
                bufferShort.AsSpan(0, 40),
                bitmap.Width,
                bitmap.Height,
                bitmap.BitCount,
                bitmap.ColorTable.Count);

            if (bitmap.BitCount < 24)
            {
                // compare color table
                var expectedColorTable = bufferFull.AsSpan(14 + 40, bitmap.ColorTableLength);
                var actualColorTable = bufferShort.AsSpan(40, bitmap.ColorTableLength);

                Core.Compare(expectedColorTable, actualColorTable);
            }

            var pixelBufferLength = Bitmap.GetStride(bitmap.Width, bitmap.BitCount) * bitmap.Height;

            var expectedPixelBuffer = bufferFull.AsSpan(lengthFull - pixelBufferLength, pixelBufferLength);
            var actualPixelBuffer = bufferShort.AsSpan(lengthShort - pixelBufferLength, pixelBufferLength);

            Core.Compare(expectedPixelBuffer, actualPixelBuffer);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(bufferFull);
            ArrayPool<byte>.Shared.Return(bufferShort);
        }
    }

    [Theory]
    [InlineData("rectangular/image-1.bmp")]
    [InlineData("rectangular/image-4.bmp")]
    [InlineData("rectangular/image-8.bmp")]
    [InlineData("rectangular/image-24.bmp")]
    [InlineData("rectangular/image-32.bmp")]
    public void SavePixels_BufferIsLess(string fileName)
    {
        var bitmap = Bitmap.FromFile(Core.OpenFile(fileName));

        var length = Bitmap.GetStride(bitmap.Width, bitmap.BitCount) * bitmap.Height - 1;

        var buffer = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => bitmap.SavePixels(buffer.AsSpan(0, length)));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    [Theory]
    [InlineData("rectangular/image-1.bmp")]
    [InlineData("rectangular/image-4.bmp")]
    [InlineData("rectangular/image-8.bmp")]
    [InlineData("rectangular/image-24.bmp")]
    [InlineData("rectangular/image-32.bmp")]
    public void SavePixels_BufferIsGreater(string fileName)
    {
        var bitmap = Bitmap.FromFile(Core.OpenFile(fileName));

        var length = Bitmap.GetStride(bitmap.Width, bitmap.BitCount) * bitmap.Height + 1;

        var buffer = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => bitmap.SavePixels(buffer.AsSpan(0, length)));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    [Theory]
    [InlineData("rectangular/image-1.bmp", 1)]
    [InlineData("rectangular/image-4.bmp", 4)]
    [InlineData("rectangular/image-8.bmp", 8)]
    [InlineData("rectangular/image-24.bmp", 24)]
    [InlineData("rectangular/image-32.bmp", 32)]
    public void SavePixels(string fileName, int bitCount)
    {
        var bitmap = Bitmap.FromFile(Core.OpenFile(fileName));

        var lengthFull = bitmap.GetLength();
        var lengthShort = Bitmap.GetStride(96, bitCount) * 64;

        var bufferFull = ArrayPool<byte>.Shared.Rent(lengthFull);
        var bufferShort = ArrayPool<byte>.Shared.Rent(lengthShort);
        try
        {
            bufferFull.AsSpan(0, lengthFull).Clear();
            bitmap.Save(bufferFull.AsSpan(0, lengthFull));

            bufferShort.AsSpan(0, lengthShort).Clear();
            bitmap.SavePixels(bufferShort.AsSpan(0, lengthShort));

            var pixelBufferPosition = BitConverter.ToInt32(bufferFull.AsSpan(FILE_HEADER_PIXELS, sizeof(int)));

            Core.Compare(bufferFull.AsSpan(pixelBufferPosition, lengthShort), bufferShort.AsSpan(0, lengthShort));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(bufferFull);
            ArrayPool<byte>.Shared.Return(bufferShort);
        }
    }

    [Theory]
    [InlineData("rectangular/image-1.bmp")]
    [InlineData("rectangular/image-4.bmp")]
    [InlineData("rectangular/image-8.bmp")]
    [InlineData("rectangular/image-24.bmp")]
    [InlineData("rectangular/image-32.bmp")]
    public void SaveBitmask_BufferIsLess(string fileName)
    {
        var bitmap = Bitmap.FromFile(Core.OpenFile(fileName));

        var length = Bitmap.GetStride(bitmap.Width, 1) * bitmap.Height - 1;

        var buffer = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => bitmap.SaveBitmask(buffer.AsSpan(0, length), _ => true));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    [Theory]
    [InlineData("rectangular/image-1.bmp")]
    [InlineData("rectangular/image-4.bmp")]
    [InlineData("rectangular/image-8.bmp")]
    [InlineData("rectangular/image-24.bmp")]
    [InlineData("rectangular/image-32.bmp")]
    public void SaveBitmask_BufferIsGreater(string fileName)
    {
        var bitmap = Bitmap.FromFile(Core.OpenFile(fileName));

        var length = Bitmap.GetStride(bitmap.Width, 1) * bitmap.Height + 1;

        var buffer = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => bitmap.SaveBitmask(buffer.AsSpan(0, length), _ => true));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    [Theory]
    [InlineData("rectangular/image-1.bmp")]
    [InlineData("rectangular/image-4.bmp")]
    [InlineData("rectangular/image-8.bmp")]
    [InlineData("rectangular/image-24.bmp")]
    [InlineData("rectangular/image-32.bmp")]
    public void SaveBitmask(string fileName)
    {
        var bitmap = Bitmap.FromFile(Core.OpenFile(fileName));

        var length = Bitmap.GetStride(bitmap.Width, 1) * bitmap.Height;

        var pixelBuffer = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            bitmap.SaveBitmask(pixelBuffer.AsSpan(0, length), x => (x & 0xFF000000) == 0x00 || (x & 0xFFFFFF) == 0xFFFFFF);

            var colorTable = new uint[] { 0xFFFFFF, 0x000000 };
            var bmp = new Bitmap(bitmap.Width, bitmap.Height, 1, pixelBuffer.AsSpan(0, length), colorTable);

            var buffer = ArrayPool<byte>.Shared.Rent(bmp.GetLength());
            try
            {
                bmp.Save(buffer.AsSpan(0, bmp.GetLength()));
                using var stream = new MemoryStream(bmp.GetLength());
                stream.Write(buffer);
                stream.Flush();
                stream.Position = 0;

                Core.Compare("rectangular/mask.bmp", stream);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(pixelBuffer);
        }
    }

    private static void VerifyBitmapInfoHeader(Span<byte> buffer, int width, int height, int bitCount, int colorCount)
    {
        Assert.Equal(40, buffer.Length);

        Assert.Equal(40, BitConverter.ToInt32(buffer[..]));
        Assert.Equal(width, BitConverter.ToInt32(buffer[4..]));
        Assert.Equal(height, BitConverter.ToInt32(buffer[8..]));
        Assert.Equal(1, BitConverter.ToInt16(buffer[12..]));
        Assert.Equal(bitCount, BitConverter.ToInt16(buffer[14..]));
    }
}
