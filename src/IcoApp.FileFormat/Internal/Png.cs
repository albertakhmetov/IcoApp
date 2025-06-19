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

using System.Buffers;

internal static class Png
{
    private const int PNG_IMAGE_HEADER_SIZE = 0x0d;

    // REVERSED
    private const int IHDR_WIDTH = 0x04;
    private const int IHDR_HEIGHT = 0x00;

    private static long PngSignature = BitConverter.ToInt64(new byte[] {
        (byte)0x89,
        (byte)0x50,
        (byte)0x4e,
        (byte)0x47,
        (byte)0x0d,
        (byte)0x0a,
        (byte)0x1a,
        (byte)0x0a
    });

    private static long PngImageHeader = BitConverter.ToInt64(new byte[] {
        (byte)0x00,
        (byte)0x00,
        (byte)0x00,
        (byte)PNG_IMAGE_HEADER_SIZE,
        (byte)0x49,
        (byte)0x48,
        (byte)0x44,
        (byte)0x52
    });

    public static bool IsSupported(Span<byte> buffer)
    {
        return buffer.Length >= sizeof(long) * 2
            && BitConverter.ToInt64(buffer.Slice(0, sizeof(long))) == PngSignature
            && BitConverter.ToInt64(buffer.Slice(sizeof(long), sizeof(long))) == PngImageHeader;
    }

    public static void ParseSize(Span<byte> buffer, out int width, out int height)
    {
        var sizeBuffer = buffer
            .Slice(sizeof(long) * 2, sizeof(int) * 2)
            .ToArray()
            .AsSpan();

        sizeBuffer.Reverse();

        width = BitConverter.ToInt32(sizeBuffer.Slice(IHDR_WIDTH, sizeof(int)));
        height = BitConverter.ToInt32(sizeBuffer.Slice(IHDR_HEIGHT, sizeof(int)));
    }
}
