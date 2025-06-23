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

#pragma warning disable CS8604 // Possible null reference argument.

using System;
using System.Buffers;

public class IcoPngFrameTests
{
    [Fact]
    public void CreateFromImage_NullStream()
    {
        Assert.Throws<ArgumentNullException>(() => { IcoFilePngFrame.CreateFromImage(null as Stream); });
    }

    [Fact]
    public void CreateFromImage_BmpStream_Invalid()
    {
        using var stream = Core.OpenFile("rectangular/image-32.bmp");

        Assert.Throws<ArgumentException>(() => { IcoFilePngFrame.CreateFromImage(stream); });
    }

    [Fact]
    public void CreateFromImage_Stream()
    {
        using var stream = Core.OpenFile("rectangular/image.png");

        var frame = IcoFilePngFrame.CreateFromImage(stream);

        Assert.Equal(96, frame.Width);
        Assert.Equal(64, frame.Height);
        Assert.Equal(stream.Length, frame.Length);
    }

    [Fact]
    public void LoadFromIcoEntry()
    {
        using var stream = Core.OpenFile("rectangular/image.png");

        var buffer = ArrayPool<byte>.Shared.Rent((int)stream.Length);
        try
        {
            stream.ReadExactly(buffer.AsSpan(0, (int)stream.Length));

            var frame = IcoFilePngFrame.LoadFromIcoEntry(buffer.AsSpan(0, (int)stream.Length));

            Assert.Equal(96, frame.Width);
            Assert.Equal(64, frame.Height);
            Assert.Equal(stream.Length, frame.Length);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    [Fact]
    public void GetImage()
    {
        const string fileName = "rectangular/image.png";

        var frame = IcoFilePngFrame.CreateFromImage(Core.OpenFile(fileName));

        Core.Compare(fileName, frame.ImageData);
    }

    [Fact]
    public void Save_BufferIsLess()
    {
        var frame = IcoFilePngFrame.CreateFromImage(Core.OpenFile("rectangular/image.png"));

        var buffer = new byte[frame.Length - 1];
        Assert.Throws<ArgumentException>(() => frame.Save(buffer));
    }

    [Fact]
    public void Save_BufferIsGreater()
    {
        var frame = IcoFilePngFrame.CreateFromImage(Core.OpenFile("rectangular/image.png"));

        var buffer = new byte[frame.Length + 1];
        Assert.Throws<ArgumentException>(() => frame.Save(buffer));
    }

    [Fact]
    public void Save()
    {
        const string fileName = "rectangular/image.png";

        var frame = IcoFilePngFrame.CreateFromImage(Core.OpenFile(fileName));

        var buffer = new byte[frame.Length];
        frame.Save(buffer);

        using var memoryStream = new MemoryStream(buffer, false);
        Core.Compare(fileName, memoryStream);
    }
}
