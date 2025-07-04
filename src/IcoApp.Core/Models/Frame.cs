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

public class Frame : IComparable<Frame>, IDisposable
{
    public bool IsDisposed { get; private set; } = false;

    public required int Width { get; init; }

    public required int Height { get; init; }

    public int BitCount { get; init; } = 32;

    public required ImageData Image { get; init; }

    public int CompareTo(Frame? other)
    {
        if (other == null)
        {
            return 1;
        }

        var result = Width.CompareTo(other.Width);
        return result == 0 ? BitCount.CompareTo(other.BitCount) : result;
    }

    public virtual void Dispose()
    {
        if (!IsDisposed)
        {
            Image.Dispose();
            IsDisposed = true;
        }
    }
}

public class FrameWithMask : Frame
{
    public required ImageData OriginalImage { get; init; }

    public ImageData? MaskImage { get; init; }

    public override void Dispose()
    {
        if (!IsDisposed)
        {
            OriginalImage?.Dispose();
            MaskImage?.Dispose();
        }

        base.Dispose();
    }
}