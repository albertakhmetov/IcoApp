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
namespace IcoApp.FileFormat.Internal;

using System;
using System.Buffers;
using System.Collections.ObjectModel;
using System.Numerics;

internal sealed class Bitmap
{
    public static Bitmap FromFile(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var length = stream.Length.ToInt32();

        var buffer = ArrayPool<byte>.Shared.Rent(length);
        buffer.AsSpan().Clear();

        try
        {
            stream.ReadExactly(buffer.AsSpan(0, length));

            return FromFile(buffer.AsSpan(0, length));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public static Bitmap FromFile(Span<byte> buffer)
    {
        if (!IsSupported(buffer, buffer.Length))
        {
            throw new InvalidDataException("This file isn't supported");
        }

        var pixelBufferPosition = BitConverter.ToInt32(buffer.Slice(FILE_HEADER_PIXELS, sizeof(int)));

        return new Bitmap(buffer.Slice(FILE_HEADER_SIZE), pixelBufferPosition - FILE_HEADER_SIZE);
    }

    public static void ParseSize(Span<byte> buffer, out int width, out int height, out int bitCount)
    {
        var headerBuffer = BitConverter.ToInt16(buffer.Slice(0, sizeof(short))) == BitmapSignature
            ? buffer.Slice(FILE_HEADER_SIZE)
            : buffer;

        width = BitConverter.ToInt32(headerBuffer.Slice(BITMAP_HEADER_WIDTH, sizeof(int)));
        height = BitConverter.ToInt32(headerBuffer.Slice(BITMAP_HEADER_HEIGHT, sizeof(int)));
        bitCount = BitConverter.ToInt16(headerBuffer.Slice(BITMAP_HEADER_BIT_COUNT, sizeof(short)));
    }

    public static bool IsSupported(Span<byte> buffer, int fileLength)
    {
        var fileSize = BitConverter.ToInt32(buffer.Slice(sizeof(short), sizeof(int)));
        var headerSize = BitConverter.ToInt32(buffer.Slice(FILE_HEADER_SIZE, sizeof(int)));

        return fileSize == fileLength
            && Array.IndexOf(SupportedHeaders, headerSize) >= 0
            && BitConverter.ToInt16(buffer.Slice(0, sizeof(short))) == BitmapSignature;
    }

    public static int GetStride(int width, int bitCount)
    {
        return (((width * bitCount) + 31) & ~31) >> 3;
    }

    public static int GetImageLength(int width, int height, int bitCount, bool withFileHeader = true)
    {
        return (withFileHeader ? FILE_HEADER_SIZE : 0) +
            (bitCount >= 24 && withFileHeader ? BITMAP_V5_HEADER_SIZE : BITMAP_INFO_HEADER_SIZE) +
            (IsIndexedBitmap(bitCount) ? (1 << bitCount) * sizeof(uint) : 0) +
            GetStride(width, bitCount) * height;
    }

    public static ReadOnlyCollection<uint> ParseColorTable(Span<byte> buffer)
    {
        var colorTableLength = buffer.Length / sizeof(uint);
        var pos = 0;

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

    private const int FILE_HEADER_SIZE = 0x0e;
    private const int FILE_HEADER_FILE_SIZE = 0x02;
    private const int FILE_HEADER_PIXELS = 0x0a;

    private const int BITMAP_INFO_HEADER_SIZE = 40;
    private const int BITMAP_V4_HEADER_SIZE = 108;
    private const int BITMAP_V5_HEADER_SIZE = 124;

    private const int BITMAP_HEADER_WIDTH = 0x04;
    private const int BITMAP_HEADER_HEIGHT = 0x08;
    private const int BITMAP_HEADER_COLOR_PANES = 0x0c;
    private const int BITMAP_HEADER_BIT_COUNT = 0x0e;
    private const int BITMAP_HEADER_COMPRESSION = 0x10;
    private const int BITMAP_HEADER_IMAGE_SIZE = 0x14;
    private const int BITMAP_HEADER_COLORS = 0x20;
    private const int BITMAP_HEADER_RED_MASK = 0x28;
    private const int BITMAP_HEADER_GREEN_MASK = 0x2c;
    private const int BITMAP_HEADER_BLUE_MASK = 0x30;
    private const int BITMAP_HEADER_ALPHA_MASK = 0x34;

    private const int BI_BITFIELDS = 0x0003;

    private static readonly int[] SupportedHeaders = [BITMAP_INFO_HEADER_SIZE, BITMAP_V4_HEADER_SIZE, BITMAP_V5_HEADER_SIZE];

    private static readonly int[] SupportedBitCount = [1, 4, 8, 24, 32];

    private static readonly short BitmapSignature = BitConverter.ToInt16(new byte[] { (byte)0x42, (byte)0x4d });

    private readonly uint redMask = 0x00FF0000, greenMask = 0x0000FF00, blueMask = 0x000000FF, alphaMask = 0xFF000000;

    public Bitmap(Span<byte> buffer, int pixelBufferPosition)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pixelBufferPosition, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(pixelBufferPosition, buffer.Length);

        var headerSize = BitConverter.ToInt32(buffer.Slice(0, sizeof(int)));
        var colorUsed = BitConverter.ToInt32(buffer.Slice(BITMAP_HEADER_COLORS, sizeof(int)));

        Width = BitConverter.ToInt32(buffer.Slice(BITMAP_HEADER_WIDTH, sizeof(int)));
        Height = BitConverter.ToInt32(buffer.Slice(BITMAP_HEADER_HEIGHT, sizeof(int)));
        BitCount = BitConverter.ToInt16(buffer.Slice(BITMAP_HEADER_BIT_COUNT, sizeof(short)));
        ColorTableLength = IsIndexedBitmap(BitCount) ? (1 << BitCount) * sizeof(uint) : 0;

        if (Array.IndexOf(SupportedBitCount, BitCount) < 0)
        {
            throw new NotSupportedException($"Bit count isn't supported ({BitCount})");
        }

        if (IsIndexedBitmap(BitCount))
        {
            var colorCount = BitConverter.ToInt32(buffer.Slice(BITMAP_HEADER_COLORS, sizeof(int)));
            if (colorCount == 0)
            {
                colorCount = 1 << BitCount;
            }

            ColorTable = ParseColorTable(buffer.Slice(headerSize, colorCount * sizeof(uint)));
            Pixels = ParseIndexedPixels(buffer.Slice(pixelBufferPosition));
        }
        else
        {
            if (headerSize >= BITMAP_V4_HEADER_SIZE)
            {
                redMask = BitConverter.ToUInt32(buffer[BITMAP_HEADER_RED_MASK..]);
                greenMask = BitConverter.ToUInt32(buffer[BITMAP_HEADER_GREEN_MASK..]);
                blueMask = BitConverter.ToUInt32(buffer[BITMAP_HEADER_BLUE_MASK..]);
                alphaMask = BitConverter.ToUInt32(buffer[BITMAP_HEADER_ALPHA_MASK..]);
            }

            ColorTable = Array.Empty<uint>().AsReadOnly();
            Pixels = ParsePixels(buffer.Slice(pixelBufferPosition));
        }
    }

    public Bitmap(int width, int height, int bitCount, Span<byte> pixelBuffer, IEnumerable<uint>? colorTable = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(width, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(height, 0);

        if (Array.IndexOf(SupportedBitCount, bitCount) < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bitCount), $"{bitCount} isn't supported");
        }

        if (IsIndexedBitmap(bitCount) && colorTable == null)
        {
            throw new ArgumentNullException(nameof(colorTable), "Color table is required for indexed bitmaps");
        }

        Width = width;
        Height = height;
        BitCount = bitCount;
        ColorTableLength = IsIndexedBitmap(bitCount) ? (1 << bitCount) * sizeof(uint) : 0;

        if (IsIndexedBitmap(bitCount))
        {
            ColorTable = new List<uint>(colorTable!).AsReadOnly();
            Pixels = ParseIndexedPixels(pixelBuffer);
        }
        else
        {
            ColorTable = Array.Empty<uint>().AsReadOnly();
            Pixels = ParsePixels(pixelBuffer);
        }
    }

    public Bitmap(int width, int height, int bitCount, IList<uint> pixels)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(width, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(height, 0);

        if (Array.IndexOf(SupportedBitCount, bitCount) < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bitCount), $"{bitCount} isn't supported");
        }

        ArgumentNullException.ThrowIfNull(pixels);
        ArgumentOutOfRangeException.ThrowIfNotEqual(pixels.Count, width * height, nameof(pixels));

        Width = width;
        Height = height;
        BitCount = bitCount;
        ColorTableLength = bitCount <= 8 ? (1 << bitCount) * sizeof(uint) : 0;

        Pixels = new List<uint>(pixels).AsReadOnly();

        ColorTable = IsIndexedBitmap(bitCount)
            ? new HashSet<uint>(pixels).ToArray().AsReadOnly()
            : Array.Empty<uint>().AsReadOnly();
    }

    public int Width { get; private set; }

    public int Height { get; private set; }

    public int BitCount { get; private set; }

    public int ColorTableLength { get; }

    public ReadOnlyCollection<uint> ColorTable { get; }

    public ReadOnlyCollection<uint> Pixels { get; }

    public int GetLength(bool withFileHeader = true)
    {
        return GetImageLength(Width, Height, BitCount, withFileHeader);
    }

    public void Save(Span<byte> buffer, bool writeFileHeader = true)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(buffer.Length, GetLength(writeFileHeader));

        buffer.Clear();

        var fileHeaderSize = writeFileHeader ? FILE_HEADER_SIZE : 0;
        var bitmapHeaderSize = !IsIndexedBitmap(BitCount) && writeFileHeader ? BITMAP_V5_HEADER_SIZE : BITMAP_INFO_HEADER_SIZE;

        var pixelBufferPosition =
            fileHeaderSize +
            bitmapHeaderSize +
            ColorTableLength;

        if (writeFileHeader)
        {
            // File Header
            BitConverter.TryWriteBytes(buffer.Slice(0, sizeof(short)), BitmapSignature).IsTrue();
            BitConverter.TryWriteBytes(buffer.Slice(FILE_HEADER_FILE_SIZE, sizeof(int)), buffer.Length).IsTrue();
            BitConverter.TryWriteBytes(buffer.Slice(FILE_HEADER_PIXELS, sizeof(int)), pixelBufferPosition).IsTrue();
        }

        // Bitmap Header

        var headerBuffer = buffer.Slice(fileHeaderSize, bitmapHeaderSize);

        BitConverter.TryWriteBytes(headerBuffer.Slice(0, sizeof(int)), bitmapHeaderSize).IsTrue();
        BitConverter.TryWriteBytes(headerBuffer.Slice(BITMAP_HEADER_WIDTH, sizeof(int)), Width).IsTrue();
        BitConverter.TryWriteBytes(headerBuffer.Slice(BITMAP_HEADER_HEIGHT, sizeof(int)), Height).IsTrue();
        BitConverter.TryWriteBytes(headerBuffer.Slice(BITMAP_HEADER_COLOR_PANES, sizeof(int)), 1).IsTrue();
        BitConverter.TryWriteBytes(headerBuffer.Slice(BITMAP_HEADER_BIT_COUNT, sizeof(int)), BitCount).IsTrue();

        if (!IsIndexedBitmap(BitCount) && writeFileHeader)
        {
            BitConverter.TryWriteBytes(headerBuffer.Slice(BITMAP_HEADER_COMPRESSION, sizeof(int)), BI_BITFIELDS).IsTrue();
            BitConverter.TryWriteBytes(headerBuffer.Slice(BITMAP_HEADER_RED_MASK, sizeof(uint)), (uint)0x00ff0000).IsTrue();
            BitConverter.TryWriteBytes(headerBuffer.Slice(BITMAP_HEADER_GREEN_MASK, sizeof(uint)), (uint)0x00ff00).IsTrue();
            BitConverter.TryWriteBytes(headerBuffer.Slice(BITMAP_HEADER_BLUE_MASK, sizeof(uint)), (uint)0x000000ff).IsTrue();
            BitConverter.TryWriteBytes(headerBuffer.Slice(BITMAP_HEADER_ALPHA_MASK, sizeof(uint)), (uint)0xff000000).IsTrue();
        }

        if (IsIndexedBitmap(BitCount))
        {
            BitConverter.TryWriteBytes(headerBuffer.Slice(BITMAP_HEADER_COLORS, sizeof(int)), 0).IsTrue();

            var colorTablePosition = fileHeaderSize + bitmapHeaderSize;
            WriteColorTable(buffer.Slice(colorTablePosition, ColorTableLength));

            WriteIndexedPixels(buffer.Slice(pixelBufferPosition));
        }
        else
        {
            WritePixels(buffer.Slice(pixelBufferPosition));
        }
    }

    public void SavePixels(Span<byte> pixelBuffer)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(pixelBuffer.Length, GetStride(Width, BitCount) * Height);

        if (IsIndexedBitmap(BitCount))
        {
            WriteIndexedPixels(pixelBuffer);
        }
        else
        {
            WritePixels(pixelBuffer);
        }
    }

    public void SaveBitmask(Span<byte> bitmaskBuffer, Func<uint, bool> predicate)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(bitmaskBuffer.Length, GetStride(Width, 1) * Height);

        bitmaskBuffer.Clear();

        var paddingBytes = GetPaddingBytes(Width, 1);

        var pos = 0;

        for (var y = Height; y > 0; y--)
        {
            byte b = 0;
            int bPos = 0;

            for (var x = 0; x < Width; x++)
            {
                var color = Pixels[Width * (y - 1) + x];
                var colorIndex = predicate(color) ? 0 : 1;

                bPos += 1;
                b |= (byte)((colorIndex << (8 - bPos)) & 0xFFFF);

                if (bPos == 8 || x == Width - 1)
                {
                    bitmaskBuffer[pos++] = b;

                    bPos = 0;
                    b = 0;
                }
            }

            pos += paddingBytes;
        }
    }

    private static bool IsIndexedBitmap(int bitCount) => bitCount <= 8;

    private static int GetPaddingBytes(int width, int bitCount)
    {
        return GetStride(width, bitCount) - Convert.ToInt32(Math.Ceiling(width * bitCount / 8.0));
    }

    private static void ReadBits(Span<byte> buffer, int width, int height, int bitCount, Span<byte> data)
    {
        data.Clear();

        var pos = 0;

        var x = 0;
        var y = height;

        var paddingBytes = GetPaddingBytes(width, bitCount);

        var b = buffer[pos++];
        var bPos = 0;

        do
        {
            var mask = (1 << bitCount) - 1;
            var result = (b >> (8 - bPos - bitCount)) & mask;

            data[width * (y - 1) + x] = (byte)result;

            if (x < width - 1)
            {
                x++;
            }
            else
            {
                x = 0;
                y--;
                pos += paddingBytes;
                bPos = 8;
            }

            if (bPos + bitCount >= 8 && y > 0)
            {
                b = buffer[pos++];
                bPos = 0;
            }
            else
            {
                bPos += bitCount;
            }
        }
        while (y > 0);
    }

    private ReadOnlyCollection<uint> ParsePixels(Span<byte> pixelBuffer)
    {
        var red = BitOperations.TrailingZeroCount(redMask) / 8;
        var green = BitOperations.TrailingZeroCount(greenMask) / 8;
        var blue = BitOperations.TrailingZeroCount(blueMask) / 8;
        var alpha = BitOperations.TrailingZeroCount(alphaMask) / 8;

        var hasAlpha = BitCount == 32;

        var pixels = new uint[Width * Height];
        var pos = 0;

        for (var y = Height; y > 0; y--)
        {
            for (var x = 0; x < Width; x++)
            {
                var b = pixelBuffer[pos + blue];
                var g = pixelBuffer[pos + green];
                var r = pixelBuffer[pos + red];

                if (hasAlpha)
                {
                    var a = pixelBuffer[pos + alpha];

                    pixels[Width * (y - 1) + x] = (uint)
                        (0xFFFFFFFF & (a << 24 | r << 16 | g << 8 | b));

                    pos += 4; // BGRA
                }
                else
                {
                    pixels[Width * (y - 1) + x] = (uint)
                        (0xFFFFFFFF & ((r + b + g == 0 ? 0 : 255) << 24 | r << 16 | g << 8 | b));

                    pos += 3; // BGR
                }
            }
        }

        return pixels.AsReadOnly();
    }

    private ReadOnlyCollection<uint> ParseIndexedPixels(Span<byte> buffer)
    {
        var rawDataSize = Width * Height;
        var rawData = ArrayPool<byte>.Shared.Rent(rawDataSize);

        var pixels = new uint[rawDataSize];
        try
        {
            ReadBits(buffer, Width, Height, BitCount, rawData.AsSpan(0, rawDataSize));

            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = ColorTable[rawData[i]];
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rawData);
        }

        return pixels.AsReadOnly();
    }

    private void WritePixels(Span<byte> buffer)
    {
        var paddingBytes = GetPaddingBytes(Width, BitCount);
        var pos = 0;
        var mask = (1u << 8) - 1;

        for (var y = Height; y > 0; y--)
        {
            for (var x = 0; x < Width; x++)
            {
                var color = Pixels[Width * (y - 1) + x];

                buffer[pos++] = (byte)(color & mask);
                buffer[pos++] = (byte)((color >> 8) & mask);
                buffer[pos++] = (byte)((color >> 16) & mask);

                if (BitCount == 32)
                {
                    buffer[pos++] = (byte)((color >> 24) & mask);
                }
            }

            pos += paddingBytes;
        }
    }

    private void WriteIndexedPixels(Span<byte> buffer)
    {
        var paddingBytes = GetPaddingBytes(Width, BitCount);
        buffer.Clear();

        var pos = 0;

        for (var y = Height; y > 0; y--)
        {
            byte b = 0;
            int bPos = 0;

            for (var x = 0; x < Width; x++)
            {
                var color = Pixels[Width * (y - 1) + x];
                var colorIndex = ColorTable.IndexOf(color);

                bPos += BitCount;
                b |= (byte)((colorIndex << (8 - bPos)) & 0xFFFF);

                if (bPos == 8 || x == Width - 1)
                {
                    buffer[pos++] = b;

                    bPos = 0;
                    b = 0;
                }
            }

            pos += paddingBytes;
        }
    }

    private void WriteColorTable(Span<byte> buffer)
    {
        buffer.Clear();

        var pos = 0;
        var mask = (1u << 8) - 1;

        for (var i = 0; i < ColorTable.Count; i++)
        {
            var color = ColorTable[i];

            buffer[pos++] = (byte)(color & mask);
            buffer[pos++] = (byte)((color >> 8) & mask);
            buffer[pos++] = (byte)((color >> 16) & mask);
            buffer[pos++] = (byte)((color >> 24) & mask);
        }
    }
}
