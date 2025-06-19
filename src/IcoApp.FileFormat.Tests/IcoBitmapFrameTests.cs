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
using IcoApp.FileFormat.Internal;

public class IcoBitmapFrameTests
{
    private const int FILE_HEADER_SIZE = 0x0e;

    [Fact]
    public void Ctor_NullStream()
    {
        Assert.Throws<ArgumentNullException>(() => { IcoBitmapFrame.CreateFromImages(null as Stream); });
    }

    [Fact]
    public void Ctor_PngStream_Invalid()
    {
        using var stream = Core.OpenFile("rectangular/image.png");

        Assert.Throws<ArgumentException>(() => { IcoBitmapFrame.CreateFromImages(stream); });
    }

    [Fact]
    public void Ctor_32bit()
    {
        using var stream = Core.OpenFile("rectangular/image-32.bmp");

        var frame = IcoBitmapFrame.CreateFromImages(stream);

        Assert.Equal(96, frame.Width);
        Assert.Equal(64, frame.Height);
    }

    [Theory]
    [InlineData("rectangular/image-1.bmp")]
    [InlineData("rectangular/image-4.bmp")]
    [InlineData("rectangular/image-8.bmp")]
    [InlineData("rectangular/image-24.bmp")]
    public void Ctor_NonTransparentWithoutMask_Invalid(string fileName)
    {
        Assert.Throws<ArgumentNullException>(() => IcoBitmapFrame.CreateFromImages(Core.OpenFile(fileName), maskImageStream: null));
    }

    [Fact]
    public void Ctor_Buffer()
    {
        const int DIR_SIZE = 0x06;
        const int DIR_ENTRY_SIZE = 0x10;

        using var stream = Core.OpenFile("rectangular.ico");
        var length = (int)stream.Length;

        var buffer = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            buffer.AsSpan().Clear();
            stream.ReadExactly(buffer.AsSpan(0, length));

            var frameCount = BitConverter.ToInt16(buffer[4..]);

            Assert.Equal(5, frameCount);

            var processed = 0;

            for (var frameId = 0; frameId < frameCount; frameId++)
            {
                var entryBuffer = buffer
                    .AsSpan(DIR_SIZE)
                    .Slice(DIR_ENTRY_SIZE * frameId, DIR_ENTRY_SIZE);

                Assert.Equal(96, entryBuffer[0]);
                Assert.Equal(64, entryBuffer[1]);

                var width = entryBuffer[0];
                var height = entryBuffer[1];
                var pos = BitConverter.ToInt32(entryBuffer.Slice(12, 4));
                var size = BitConverter.ToInt32(entryBuffer.Slice(8, 4));

                if (!Png.IsSupported(buffer.AsSpan(pos)))
                {
                    var frame = IcoBitmapFrame.LoadFromIcoEntry(buffer.AsSpan(pos, size));

                    Assert.Equal(96, frame.Width);
                    Assert.Equal(64, frame.Height);

                    Core.Compare($"rectangular/image-{frame.BitCount}.bmp", frame.OriginalImageData);
                    Core.Compare("rectangular/mask.bmp", frame.MaskImageData);

                    processed++;
                }
            }

            Assert.Equal(4, processed);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    [Theory]
    [InlineData("rectangular/image-4.bmp")]
    [InlineData("rectangular/image-8.bmp")]
    [InlineData("rectangular/image-24.bmp")]
    [InlineData("rectangular/image-32.bmp")]
    public void GetImage(string fileName)
    {
        var frame = IcoBitmapFrame.CreateFromImages(Core.OpenFile(fileName), Core.OpenFile("rectangular/mask.bmp"));

        Core.Compare("rectangular/image-32.bmp", frame.ImageData);
    }

    [Fact]
    public void GetOriginalImage()
    {
        const string fileName = "rectangular/image-32.bmp";

        var frame = IcoBitmapFrame.CreateFromImages(Core.OpenFile(fileName));

        Core.Compare(fileName, frame.OriginalImageData);
    }

    [Theory]
    [InlineData("rectangular/image-1.bmp")]
    [InlineData("rectangular/image-4.bmp")]
    [InlineData("rectangular/image-8.bmp")]
    [InlineData("rectangular/image-24.bmp")]
    public void GetMaskImage(string fileName)
    {
        var frame = IcoBitmapFrame.CreateFromImages(Core.OpenFile(fileName), Core.OpenFile("rectangular/mask.bmp"));

        Core.Compare("rectangular/mask.bmp", frame.MaskImageData);
    }

    [Fact]
    public void GetMaskImage_Generated()
    {
        var frame = IcoBitmapFrame.CreateFromImages(Core.OpenFile("rectangular/image-32.bmp"));

        Core.Compare("rectangular/mask.bmp", frame.MaskImageData);
    }

    [Fact]
    public void SaveFrame_BufferIsLess()
    {
        var frame = IcoBitmapFrame.CreateFromImages(Core.OpenFile("rectangular/image-32.bmp"));

        var buffer = new byte[frame.FrameLength - 1];
        Assert.Throws<ArgumentOutOfRangeException>(() => frame.SaveFrame(buffer));
    }

    [Fact]
    public void SaveFrame_BufferIsGreater()
    {
        var frame = IcoBitmapFrame.CreateFromImages(Core.OpenFile("rectangular/image-32.bmp"));

        var buffer = new byte[frame.FrameLength + 1];
        Assert.Throws<ArgumentOutOfRangeException>(() => frame.SaveFrame(buffer));
    }

    [Theory]
    [InlineData("rectangular/image-4.bmp")]
    [InlineData("rectangular/image-8.bmp")]
    [InlineData("rectangular/image-24.bmp")]
    [InlineData("rectangular/image-32.bmp")]
    public void SaveFrame(string fileName)
    {
        const string expectedFileName = "rectangular/image-32.bmp";

        var frame = IcoBitmapFrame.CreateFromImages(Core.OpenFile(fileName), Core.OpenFile("rectangular/mask.bmp"));

        var buffer = new byte[frame.FrameLength];
        frame.SaveFrame(buffer);

        Core.CompareFrames(expectedFileName, buffer);
    }
}
