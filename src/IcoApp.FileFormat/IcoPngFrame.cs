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
using System.Collections.Immutable;
using IcoApp.FileFormat.Internal;

public class IcoPngFrame : IcoFrame
{
    public static IcoPngFrame CreateFromImage(Stream imageStream)
    {
        ArgumentNullException.ThrowIfNull(imageStream);

        return new IcoPngFrame(imageStream, []);
    }

    public static IcoPngFrame LoadFromIcoEntry(Span<byte> entryBuffer)
    {
        return new IcoPngFrame(null, entryBuffer);
    }

    private IcoPngFrame(Stream? stream, Span<byte> entryBuffer)
    {
        FrameLength = stream != null ? (int)stream.Length : entryBuffer.Length;

        var imageData = new byte[FrameLength];

        if (stream != null)
        {
            stream.ReadExactly(imageData.AsSpan(0, FrameLength));
        }
        else
        {
            entryBuffer.CopyTo(imageData.AsSpan(0, FrameLength));
        }

        if (!Png.IsSupported(imageData))
        {
            throw new ArgumentException();
        }

        Png.ParseSize(imageData, out var width, out var height);

        Width = width;
        Height = height;

        ImageData = ImmutableArray.Create(imageData);
    }

    public override int Width { get; }

    public override int Height { get; }

    public override int FrameLength { get; }

    public override ImmutableArray<byte> ImageData { get; }

    public override void SaveFrame(Span<byte> buffer)
    {
        if (buffer.Length != FrameLength)
        {
            throw new ArgumentException();
        }

        buffer.Clear();
        ImageData.CopyTo(buffer);
    }
}
