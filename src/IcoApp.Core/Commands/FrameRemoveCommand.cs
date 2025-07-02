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
    private readonly IIcoFileService icoFileService;

    private IImmutableList<Frame>? frames;

    public FrameRemoveCommand(IIcoFileService icoFileService)
    {
        ArgumentNullException.ThrowIfNull(icoFileService);

        this.icoFileService = icoFileService;
    }

    void IDisposable.Dispose()
    {
        if (CanUndo)
        {
            frames?.ForEach(x => x.Dispose());
        }
    }

    protected override Task<bool> ExecuteAsync(Parameters parameters)
    {
        if (parameters.Frames.Count == 0 && parameters.RemoveAll is false)
        {
            return Task.FromResult(false);
        }

        var removedFrames = parameters.RemoveAll
            ? frames = icoFileService.Frames.RemoveAll()
            : parameters.Frames.ForEach(x => icoFileService.Frames.Remove(x)).ToImmutableArray();

        if (removedFrames.Any() is false)
        {
            return Task.FromResult(false);
        }

        frames = removedFrames;

        return Task.FromResult(true);
    }

    protected override async Task Undo()
    {
        await icoFileService.Frames.AddAsync(frames ?? []);
    }

    protected override Task Redo()
    {
        frames?.ForEach(x => icoFileService.Frames.Remove(x));
        return Task.CompletedTask;
    }

    public sealed class Parameters
    {
        public IImmutableList<Frame> Frames { get; init; } = [];

        public bool RemoveAll { get; init; } = false;
    }
}
