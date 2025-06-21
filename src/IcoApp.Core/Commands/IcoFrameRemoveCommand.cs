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
