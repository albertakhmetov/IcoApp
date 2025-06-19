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
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.

using System;

public class IcoFileTests
{
    private const int DIR_SIZE = 0x06;
    private const int DIR_ENTRY_SIZE = 0x10;

    [Fact]
    public void Load_StreamIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new IcoFile().Load(null));
    }

    [Fact]
    public void Load()
    {
        using var stream = Core.OpenFile($"rectangular.ico");

        var frames = new IcoFile().Load(stream);

        foreach (var frame in frames)
        {
            if (frame is IcoBitmapFrame bitmapFrame)
            {
                Core.Compare($"rectangular/image-32.bmp", bitmapFrame.ImageData);
                Core.Compare($"rectangular/image-{bitmapFrame.BitCount}.bmp", bitmapFrame.OriginalImageData);
                Core.Compare($"rectangular/mask.bmp", bitmapFrame.MaskImageData);
            }
            else
            {
                Core.Compare($"rectangular/image.png", frame.ImageData);
            }
        }
    }

    [Fact]
    public void Save_StreamIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new IcoFile().Save(null, new IcoFrame[0]));
    }

    [Fact]
    public void Save_FramesIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new IcoFile().Save(new MemoryStream(), null));
    }

    [Fact]
    public void Save_FramesIsEmpty()
    {
        Assert.Throws<ArgumentException>(() => new IcoFile().Save(new MemoryStream(), new IcoFrame[0]));
    }

    [Theory]
    [InlineData("bmp4")]
    [InlineData("bmp8")]
    [InlineData("bmp24")]
    [InlineData("bmp32")]
    public void Save_Bitmap_Directory(string directoryName)
    {
        var frames = GetBitmapFrames(directoryName);

        var icoStream = new MemoryStream();
        new IcoFile().Save(icoStream, frames);

        CheckIcoDir(icoStream.GetBuffer(), frames.Count);
        for (var i = 0; i < frames.Count; i++)
        {
            CheckBitmapIcoDirEntry(icoStream.GetBuffer().AsSpan(DIR_SIZE).Slice(i * DIR_ENTRY_SIZE), frames[i] as IcoBitmapFrame);
        }
    }

    [Theory]
    [InlineData("bmp4")]
    [InlineData("bmp8")]
    [InlineData("bmp24")]
    [InlineData("bmp32")]
    public void Save_Bitmap(string directoryName)
    {
        var frames = GetBitmapFrames(directoryName);

        var icoStream = new MemoryStream();
        new IcoFile().Save(icoStream, frames);

        var i = 0;
        foreach (var fileName in Directory.GetFiles(Core.GetFullPath("bmp32")))
        {
            icoStream.Position = 0;
            using var stream = File.OpenRead(fileName);
            Core.Compare(stream, Core.GetFrame(icoStream, i++));
        }
    }

    [Fact]
    public void Save_Png_Directory()
    {
        var frames = GetPngFrames();

        var icoStream = new MemoryStream();
        new IcoFile().Save(icoStream, frames);

        CheckIcoDir(icoStream.GetBuffer(), frames.Count);
        for (var i = 0; i < frames.Count; i++)
        {
            CheckPngIcoDirEntry(icoStream.GetBuffer().AsSpan(DIR_SIZE).Slice(i * DIR_ENTRY_SIZE), frames[i]);
        }
    }

    [Fact]
    public void Save_Png()
    {
        var frames = GetPngFrames();

        var icoStream = new MemoryStream();
        new IcoFile().Save(icoStream, frames);

        var i = 0;
        foreach (var file in Directory.GetFiles(Core.GetFullPath("png")))
        {
            icoStream.Position = 0;
            using var stream = File.OpenRead(file);
            Core.Compare(stream, Core.GetFrame(icoStream, i++));
        }
    }

    private static List<IcoFrame> GetBitmapFrames(string directoryName)
    {
        var frames = new List<IcoFrame>();

        foreach (var fileName in Directory.GetFiles(Core.GetFullPath(directoryName)))
        {
            using var stream = File.OpenRead(fileName);
            using var maskStream = Core.OpenFile($"mask/{Path.GetFileName(fileName)}");

            frames.Add(IcoBitmapFrame.CreateFromImages(stream, maskStream));
        }

        return frames;
    }

    private static List<IcoFrame> GetPngFrames()
    {
        var frames = new List<IcoFrame>();

        foreach (var fileName in Directory.GetFiles(Core.GetFullPath("png")))
        {
            using var stream = File.OpenRead(fileName);
            frames.Add(IcoPngFrame.CreateFromImage(stream));
        }

        return frames;
    }

    private void CheckIcoDir(Span<byte> buffer, int count)
    {
        Assert.Equal(0, BitConverter.ToInt16(buffer[..]));
        Assert.Equal(1, BitConverter.ToInt16(buffer[2..]));
        Assert.Equal(count, BitConverter.ToInt16(buffer[4..]));
    }

    private void CheckBitmapIcoDirEntry(Span<byte> buffer, IcoBitmapFrame frame)
    {
        Assert.Equal(frame.Width == 256 ? 0 : frame.Width, buffer[0]);
        Assert.Equal(frame.Height == 256 ? 0 : frame.Height, buffer[1]);
        Assert.Equal(frame.BitCount >= 8 ? 0 : 1u << frame.BitCount, buffer[2]);
        Assert.Equal(0, buffer[3]);
        Assert.Equal(1, BitConverter.ToInt16(buffer[4..]));
        Assert.Equal(frame.BitCount, BitConverter.ToInt16(buffer[6..]));
    }

    private void CheckPngIcoDirEntry(Span<byte> buffer, IcoFrame frame)
    {
        Assert.Equal(frame.Width == 256 ? 0 : frame.Width, buffer[0]);
        Assert.Equal(frame.Height == 256 ? 0 : frame.Height, buffer[1]);
        Assert.Equal(0, buffer[2]);
        Assert.Equal(0, buffer[3]);
        Assert.Equal(1, BitConverter.ToInt16(buffer[4..]));
        Assert.Equal(32, BitConverter.ToInt16(buffer[6..]));
    }
}
