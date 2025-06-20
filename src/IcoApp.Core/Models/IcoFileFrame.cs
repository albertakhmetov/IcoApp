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
using System.Collections.Immutable;

namespace IcoApp.Core.Models;

public sealed class IcoFileFrame : IComparable<IcoFileFrame>, IDisposable
{
    public IcoFileFrame(int width, int height, Stream? sourceStream)
    {
        Width = width;
        Height = height;
        BitCount = 32;

        Data = new ImageData(sourceStream);
        Type = IcoFileFrameType.Png;
    }

    public IcoFileFrame(int width, int height, int bitCount, Stream? sourceStream)
    {
        Width = width;
        Height = height;
        BitCount = bitCount;

        Data = new ImageData(sourceStream);
        Type = IcoFileFrameType.Bitmap;
    }

    public bool IsDisposed { get; private set; } = false;

    public int Width { get; }

    public int Height { get; }

    public int BitCount { get; }

    public IcoFileFrameType Type { get; }

    public ImageData Data { get; }

    public int CompareTo(IcoFileFrame? other)
    {
        if (other == null)
        {
            return 1;
        }

        var result = Width.CompareTo(other.Width);
        return result == 0 ? BitCount.CompareTo(other.BitCount) : result;
    }

    public void Dispose()
    {
        if (!IsDisposed)
        {
            Data.Dispose();
        }
    }
}
