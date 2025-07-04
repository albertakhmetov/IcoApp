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
namespace IcoApp.Core.Models;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public sealed class ImageData : IDisposable
{
    public readonly static ImageData Empty = new(null);

    private readonly byte[] buffer;
    private bool isDisposed = false;

    public ImageData(Stream? sourceStream)
    {
        if (sourceStream == null)
        {
            Size = 0;
            buffer = [];
        }
        else
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(sourceStream.Length, int.MaxValue);

            Size = (int)sourceStream.Length;

            buffer = ArrayPool<byte>.Shared.Rent(Size);
            sourceStream.ReadExactly(buffer, 0, Size);
        }
    }

    ~ImageData()
    {
        Dispose(false);
    }

    public bool IsEmpty => Size == 0;

    public int Size { get; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public Stream GetStream()
    {
        return new MemoryStream(buffer, 0, Size, false, false);
    }

    private void Dispose(bool _)
    {
        if (!IsEmpty && !isDisposed)
        {
            ArrayPool<byte>.Shared.Return(buffer);
            isDisposed = true;
        }
    }
}
