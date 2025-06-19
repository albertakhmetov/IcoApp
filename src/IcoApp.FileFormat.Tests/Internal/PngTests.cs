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

using IcoApp.FileFormat.Internal;

public class PngTests
{
    [Fact]
    public void IsSupported_Positive()
    {
        using var stream = Core.OpenFile("rectangular/image.png");

        var buffer = new byte[sizeof(long)*2];
        stream.ReadExactly(buffer);

        Assert.True(Png.IsSupported(buffer));
    }

    [Fact]
    public void IsSupported_Negative()
    {
        using var stream = Core.OpenFile("rectangular/image-32.bmp");
        
        var buffer = new byte[sizeof(long) * 2];
        stream.ReadExactly(buffer);

        Assert.False(Png.IsSupported(buffer));
    }

    [Fact]
    public void ParseSize()
    {
        using var stream = Core.OpenFile("rectangular/image.png");
        var buffer = new byte[sizeof(long) * 2 + 0x0d];
        stream.ReadExactly(buffer);

        Png.ParseSize(buffer.AsSpan(), out var width, out var height);

        Assert.Equal(96, width);
        Assert.Equal(64, height);
    }
}
