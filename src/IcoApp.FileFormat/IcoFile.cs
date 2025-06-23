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
namespace IcoApp.FileFormat;

using System;
using System.Buffers;
using IcoApp.FileFormat.Internal;

public static class IcoFile
{
    private const int DIR_SIZE = 0x06;
    private const int DIR_ENTRY_SIZE = 0x10;

    public static IList<IIcoFileFrame> Load(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var frames = new List<IIcoFileFrame>();

        var length = (int)stream.Length;

        var buffer = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            stream.ReadExactly(buffer.AsSpan(0, length));
            var headerBuffer = buffer.AsSpan(0, DIR_SIZE);
            if (BitConverter.ToInt16(headerBuffer[0..]) != 0 || BitConverter.ToInt16(headerBuffer[2..]) != 1)
            {
                throw new InvalidDataException();
            }

            var frameCount = BitConverter.ToInt16(headerBuffer[4..]);

            for (var frameId = 0; frameId < frameCount; frameId++)
            {
                frames.Add(ParseFrame(buffer, frameId));
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        return frames;
    }

    private static IIcoFileFrame ParseFrame(Span<byte> buffer, int frameId)
    {
        var entryBuffer = buffer
            .Slice(DIR_SIZE)
            .Slice(DIR_ENTRY_SIZE * frameId, DIR_ENTRY_SIZE);

        var width = entryBuffer[0];
        var height = entryBuffer[1];
        var pos = BitConverter.ToInt32(entryBuffer.Slice(12, 4));
        var size = BitConverter.ToInt32(entryBuffer.Slice(8, 4));

        var frameBuffer = buffer.Slice(pos, size);

        if (Png.IsSupported(frameBuffer))
        {
            return IcoFilePngFrame.LoadFromIcoEntry(frameBuffer);
        }
        else
        {
            return IcoFileBitmapFrame.LoadFromIcoEntry(frameBuffer);
        }
    }

    public static void Save(Stream stream, IList<IIcoFileFrame> frames)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(frames);

        if (frames.Count == 0)
        {
            throw new ArgumentException("frames can't be empty", nameof(frames));
        }

        WriteDirectory(stream, frames);
        foreach (var frame in frames)
        {
            WriteFrame(stream, frame);
        }

        stream.Flush();
    }

    private static void WriteDirectory(Stream stream, IList<IIcoFileFrame> frames)
    {
        var dirLength = DIR_SIZE + frames.Count * DIR_ENTRY_SIZE;

        var buffer = ArrayPool<byte>.Shared.Rent(dirLength);
        try
        {
            buffer.AsSpan().Clear();

            BitConverter.TryWriteBytes(buffer.AsSpan(2), (short)1).IsTrue();
            BitConverter.TryWriteBytes(buffer.AsSpan(4), (short)frames.Count).IsTrue();

            var pos = dirLength;

            for (var i = 0; i < frames.Count; i++)
            {
                var entryBuffer = buffer.AsSpan(DIR_SIZE).Slice(i * DIR_ENTRY_SIZE, DIR_ENTRY_SIZE);
                var frame = frames[i];
                var bitCount = frame is IcoFileBitmapFrame bitmapFrame ? bitmapFrame.BitCount : 32;

                entryBuffer[0] = (byte)(frame.Width == 256 ? 0 : frame.Width);
                entryBuffer[1] = (byte)(frame.Height == 256 ? 0 : frame.Height);
                entryBuffer[2] = (byte)(bitCount >= 8 ? 0 : (1u << bitCount));
                entryBuffer[3] = 0;
                BitConverter.TryWriteBytes(entryBuffer[4..], (short)1).IsTrue();
                BitConverter.TryWriteBytes(entryBuffer[6..], (short)bitCount).IsTrue();

                BitConverter.TryWriteBytes(entryBuffer[8..], frame.Length).IsTrue();
                BitConverter.TryWriteBytes(entryBuffer[12..], pos).IsTrue();

                pos += frame.Length;
            }

            stream.Write(buffer.AsSpan(0, dirLength));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static void WriteFrame(Stream stream, IIcoFileFrame frame)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(frame.Length);
        try
        {
            buffer.AsSpan().Clear();
            frame.Save(buffer.AsSpan(0, frame.Length));
            stream.Write(buffer.AsSpan(0, frame.Length));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
