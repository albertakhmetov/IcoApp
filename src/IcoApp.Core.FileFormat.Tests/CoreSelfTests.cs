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

public class CoreSelfTests
{
    [Theory]
    [InlineData("bmp4.ico")]
    [InlineData("bmp8.ico")]
    [InlineData("bmp24.ico")]
    [InlineData("bmp32.ico")]
    public void GetFramesCount_Test(string fileName)
    {
        Assert.Equal(4, Core.GetFramesCount(fileName));
    }

    [Theory]
    [InlineData("bmp4.ico", 0)]
    [InlineData("bmp4.ico", 1)]
    [InlineData("bmp4.ico", 2)]
    [InlineData("bmp4.ico", 3)]
    [InlineData("bmp8.ico", 0)]
    [InlineData("bmp8.ico", 1)]
    [InlineData("bmp8.ico", 2)]
    [InlineData("bmp8.ico", 3)]
    [InlineData("bmp24.ico", 0)]
    [InlineData("bmp24.ico", 1)]
    [InlineData("bmp24.ico", 2)]
    [InlineData("bmp24.ico", 3)]
    [InlineData("bmp32.ico", 0)]
    [InlineData("bmp32.ico", 1)]
    [InlineData("bmp32.ico", 2)]
    [InlineData("bmp32.ico", 3)]
    public void GetFrame_Test(string fileName, int frameId)
    {
        using var frame = Core.GetFrame(fileName, frameId);

        var expectedFileName = frameId switch
        {
            0 => "bmp32/image16.bmp",
            1 => "bmp32/image20.bmp",
            2 => "bmp32/image32.bmp",
            3 => "bmp32/image64.bmp",
            _ => throw new NotSupportedException()
        };

        Core.Compare(expectedFileName, frame);
    }
}
