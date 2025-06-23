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

public class IcoFilePngFrame : IIcoFileFrame
{
    public static IcoFilePngFrame CreateFromImage(Stream imageStream)
    {
        ArgumentNullException.ThrowIfNull(imageStream);

        var lenght = imageStream.Length.ToInt32();
        var buffer = ArrayPool<byte>.Shared.Rent(lenght);

        try
        {
            var imageData = buffer.AsSpan(0, lenght);
            imageStream.ReadExactly(imageData);

            return new IcoFilePngFrame(imageData);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public static IcoFilePngFrame LoadFromIcoEntry(Span<byte> entryBuffer)
    {
        return new IcoFilePngFrame(entryBuffer);
    }

    private IcoFilePngFrame(Span<byte> imageData)
    {
        if (!Png.IsSupported(imageData))
        {
            throw new ArgumentException("Invalid PNG format.");
        }

        Png.ParseSize(imageData, out var width, out var height);

        Width = width;
        Height = height;
        Length = imageData.Length;

        ImageData = ImmutableArray.Create(imageData);
    }

    public int Width { get; }

    public int Height { get; }

    public int Length { get; }

    public ImmutableArray<byte> ImageData { get; }

    public void Save(Span<byte> buffer)
    {
        if (buffer.Length != Length)
        {
            throw new ArgumentException();
        }

        buffer.Clear();
        ImageData.CopyTo(buffer);
    }
}
