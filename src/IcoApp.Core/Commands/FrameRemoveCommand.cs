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

public class FrameRemoveCommand : UndoableAppCommand<FrameRemoveCommand.Parameters>, IDisposable
{
    private readonly ItemCollection<Frame> icoFileFrames;

    private IImmutableList<Frame>? affectedFrames;

    public FrameRemoveCommand(ItemCollection<Frame> icoFileFrames)
    {
        ArgumentNullException.ThrowIfNull(icoFileFrames);

        this.icoFileFrames = icoFileFrames;
    }

    void IDisposable.Dispose()
    {
        if (CanUndo)
        {
            affectedFrames?.ForEach(x => x.Dispose());
        }
    }

    protected override Task<bool> ExecuteAsync(Parameters parameters)
    {
        if (parameters.Frames.Count == 0 && parameters.RemoveAll is false)
        {
            return Task.FromResult(false);
        }

        var removedFrames = parameters.RemoveAll
            ? affectedFrames = icoFileFrames.RemoveAll()
            : parameters.Frames.ForEach(x => icoFileFrames.Remove(x)).ToImmutableArray();

        if (removedFrames.Any() is false)
        {
            return Task.FromResult(false);
        }

        affectedFrames = removedFrames;

        return Task.FromResult(true);
    }

    protected override async Task Undo()
    {
        await icoFileFrames.AddAsync(affectedFrames ?? []);
    }

    protected override Task Redo()
    {
        affectedFrames?.ForEach(x => icoFileFrames.Remove(x));
        return Task.CompletedTask;
    }

    public sealed class Parameters
    {
        public IImmutableList<Frame> Frames { get; init; } = [];

        public bool RemoveAll { get; init; } = false;
    }
}
