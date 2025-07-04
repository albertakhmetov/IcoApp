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
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IcoApp.Core.Models;

public class FrameConvertCommand : UndoableAppCommand<FrameConvertCommand.Parameters>, IDisposable
{
    private readonly ItemCollection<Frame> icoFileFrames;

    private Frame? originalFrame, generatedFrame;

    public FrameConvertCommand(ItemCollection<Frame> icoFileFrames)
    {
        ArgumentNullException.ThrowIfNull(icoFileFrames);

        this.icoFileFrames = icoFileFrames;
    }

    void IDisposable.Dispose()
    {
        originalFrame?.Dispose();
        originalFrame = null;
    }

    protected override async Task<bool> ExecuteAsync(Parameters parameters)
    {
        originalFrame = parameters.Frame;

        if (icoFileFrames.Contains(originalFrame) is false)
        {
            return false;
        }

        using var imageStream = originalFrame.Image.GetStream();
        using var image = Image.FromStream(imageStream);
        using var convertedImageStream = new MemoryStream();

        if (originalFrame is FrameWithMask)
        {
            image.Save(convertedImageStream, ImageFormat.Png);

            convertedImageStream.Position = 0;

            generatedFrame = new Frame()
            {
                Width = originalFrame.Width,
                Height = originalFrame.Height,
                Image = new ImageData(convertedImageStream)
            };
        }
        else
        {
            image.Save(convertedImageStream, ImageFormat.Bmp);

            imageStream.Position = 0;
            convertedImageStream.Position = 0;

            generatedFrame = new FrameWithMask()
            {
                Width = originalFrame.Width,
                Height = originalFrame.Height,
                Image = new ImageData(imageStream),
                OriginalImage = new ImageData(convertedImageStream)
            };
        }

        icoFileFrames.Remove(originalFrame);
        await icoFileFrames.AddAsync([generatedFrame]);

        return true;
    }

    protected override async Task Undo()
    {
        if (generatedFrame == null || originalFrame == null)
        {
            throw new InvalidOperationException("Can undo when generated or original frame is null");
        }

        icoFileFrames.Remove(generatedFrame);
        await icoFileFrames.AddAsync([originalFrame]);
    }

    protected override async Task Redo()
    {
        if (generatedFrame == null || originalFrame == null)
        {
            throw new InvalidOperationException("Can redo when generated or original frame is null");
        }

        icoFileFrames.Remove(originalFrame);
        await icoFileFrames.AddAsync([generatedFrame]);
    }

    public sealed class Parameters
    {
        public required Frame Frame { get; init; }
    }
}
