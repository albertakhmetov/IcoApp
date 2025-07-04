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
namespace IcoApp.Core.Commands;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IcoApp.Core.Helpers;
using IcoApp.Core.Models;
using IcoApp.Core.Services;

public class FrameAddCommand : UndoableAppCommand<FrameAddCommand.Parameters>, IDisposable
{
    private readonly ItemCollection<Frame> icoFileFrames;

    private IImmutableList<Frame>? affectedFrames;

    public FrameAddCommand(ItemCollection<Frame> icoFileFrames)
    {
        ArgumentNullException.ThrowIfNull(icoFileFrames);

        this.icoFileFrames = icoFileFrames;
    }

    void IDisposable.Dispose()
    {
        if (CanRedo)
        {
            affectedFrames?.ForEach(x => x.Dispose());
        }
    }

    protected override async Task<bool> ExecuteAsync(Parameters parameters)
    {
        if (parameters.FileNames.Count == 0)
        {
            return false;
        }

        var frames = new List<Frame>();
        foreach (var fileName in parameters.FileNames)
        {
            var dataStream = LoadImageData(fileName);

            frames.Add(CreateFrame(dataStream));
        }

        await this.icoFileFrames.AddAsync(frames);

        this.affectedFrames = frames.ToImmutableArray();

        return true;
    }

    private static MemoryStream LoadImageData(string fileName)
    {
        using var fileStream = File.OpenRead(fileName);

        var dataStream = new MemoryStream();
        fileStream.CopyTo(dataStream);
        dataStream.Position = 0;

        return dataStream;
    }

    private static Frame CreateFrame(MemoryStream dataStream)
    {
        using var image = Image.FromStream(dataStream);

        if (image.RawFormat.Equals(ImageFormat.Png))
        {
            dataStream.Position = 0;
            return new Frame()
            {
                Width = image.Width,
                Height = image.Height,
                Image = new ImageData(dataStream)
            };
        }
        else
        {
            using var imageStream = new MemoryStream();
            image.Save(imageStream, ImageFormat.Png);
            imageStream.Flush();
            imageStream.Position = 0;

            if (image.RawFormat.Equals(ImageFormat.Bmp) && TryGetBitCount(image.PixelFormat, out var bitCount))
            {
                dataStream.Position = 0;
                return new FrameWithMask()
                {
                    Width = image.Width,
                    Height = image.Height,
                    BitCount = bitCount,
                    Image = new ImageData(imageStream),
                    OriginalImage = new ImageData(dataStream)
                };
            }
            else
            {
                return new Frame()
                {
                    Width = image.Width,
                    Height = image.Height,
                    Image = new ImageData(dataStream)
                };
            }
        }
    }

    private static bool TryGetBitCount(PixelFormat pixelFormat, out int bitCount)
    {
        bitCount = pixelFormat switch
        {
            //PixelFormat.Format1bppIndexed => 1,
            //PixelFormat.Format4bppIndexed => 4,
            //PixelFormat.Format8bppIndexed => 8,
            //PixelFormat.Format24bppRgb => 24,
            PixelFormat.Format32bppArgb => 32,
            _ => 0,
        };

        return bitCount != 0;
    }

    protected override Task Undo()
    {
        affectedFrames?.ForEach(x => icoFileFrames.Remove(x));

        return Task.CompletedTask;
    }

    protected override async Task Redo()
    {
        await icoFileFrames.AddAsync(affectedFrames ?? []);
    }

    public sealed class Parameters
    {
        public required IImmutableList<string> FileNames { get; init; }
    }
}
