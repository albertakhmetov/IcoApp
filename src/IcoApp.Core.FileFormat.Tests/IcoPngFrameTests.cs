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
namespace IcoApp.Core.FileFormat.Tests;

#pragma warning disable CS8604 // Possible null reference argument.

using System;
using System.Buffers;

public class IcoPngFrameTests
{
    [Fact]
    public void CreateFromImage_NullStream()
    {
        Assert.Throws<ArgumentNullException>(() => { IcoPngFrame.CreateFromImage(null as Stream); });
    }

    [Fact]
    public void CreateFromImage_BmpStream_Invalid()
    {
        using var stream = Core.OpenFile("rectangular/image-32.bmp");

        Assert.Throws<ArgumentException>(() => { IcoPngFrame.CreateFromImage(stream); });
    }

    [Fact]
    public void CreateFromImage_Stream()
    {
        using var stream = Core.OpenFile("rectangular/image.png");

        var frame = IcoPngFrame.CreateFromImage(stream);

        Assert.Equal(96, frame.Width);
        Assert.Equal(64, frame.Height);
        Assert.Equal(stream.Length, frame.FrameLength);
    }

    [Fact]
    public void LoadFromIcoEntry()
    {
        using var stream = Core.OpenFile("rectangular/image.png");

        var buffer = ArrayPool<byte>.Shared.Rent((int)stream.Length);
        try
        {
            stream.ReadExactly(buffer.AsSpan(0, (int)stream.Length));

            var frame = IcoPngFrame.LoadFromIcoEntry(buffer.AsSpan(0, (int)stream.Length));

            Assert.Equal(96, frame.Width);
            Assert.Equal(64, frame.Height);
            Assert.Equal(stream.Length, frame.FrameLength);
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

        var frame = IcoPngFrame.CreateFromImage(Core.OpenFile(fileName));

        Core.Compare(fileName, frame.ImageData);
    }

    [Fact]
    public void SaveFrame_BufferIsLess()
    {
        var frame = IcoPngFrame.CreateFromImage(Core.OpenFile("rectangular/image.png"));

        var buffer = new byte[frame.FrameLength - 1];
        Assert.Throws<ArgumentException>(() => frame.SaveFrame(buffer));
    }

    [Fact]
    public void SaveFrame_BufferIsGreater()
    {
        var frame = IcoPngFrame.CreateFromImage(Core.OpenFile("rectangular/image.png"));

        var buffer = new byte[frame.FrameLength + 1];
        Assert.Throws<ArgumentException>(() => frame.SaveFrame(buffer));
    }

    [Fact]
    public void SaveFrame()
    {
        const string fileName = "rectangular/image.png";

        var frame = IcoPngFrame.CreateFromImage(Core.OpenFile(fileName));

        var buffer = new byte[frame.FrameLength];
        frame.SaveFrame(buffer);

        using var memoryStream = new MemoryStream(buffer, false);
        Core.Compare(fileName, memoryStream);
    }
}
