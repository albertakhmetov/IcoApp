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

public class IcoFrameRemoveCommand : UndoableAppCommand<IcoFrameRemoveCommand.Parameters>, IDisposable
{
    private readonly IIcoService icoService;

    private IImmutableList<IcoFrame>? frames;

    public IcoFrameRemoveCommand(IIcoService icoService)
    {
        ArgumentNullException.ThrowIfNull(icoService);

        this.icoService = icoService;
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

        if (parameters.RemoveAll)
        {
            frames = icoService.Frames.RemoveAll();
        }
        else
        {
            parameters.Frames.ForEach(x => icoService.Frames.Remove(x));
            frames = parameters.Frames;
        }

        return Task.FromResult(true);
    }

    protected override async Task Undo()
    {
        await icoService.Frames.AddAsync(frames ?? []);
    }

    protected override Task Redo()
    {
        frames?.ForEach(x => icoService.Frames.Remove(x));
        return Task.CompletedTask;
    }

    public sealed class Parameters
    {
        public IImmutableList<IcoFrame> Frames { get; init; } = [];

        public bool RemoveAll { get; init; } = false;
    }
}
