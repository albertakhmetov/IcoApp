﻿/*  Copyright © 2025, Albert Akhmetov <akhmetov@live.com>   
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
namespace IcoApp.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using IcoApp.Core.Commands;
using IcoApp.Core.Helpers;
using IcoApp.Core.Models;
using IcoApp.Core.Services;
using IcoApp.FileFormat;
using IcoApp.FileFormat.Internal;
using Microsoft.Extensions.Logging;

internal class IcoFileService : IIcoFileService, IDisposable
{
    private readonly CompositeDisposable disposable = [];

    private readonly ItemCollection<Frame> baseFrames;

    private readonly BehaviorSubject<bool> modifiedSubject;
    private readonly BehaviorSubject<string?> fileNameSubject;

    private readonly UndoableHistoryManager undoableHistoryManager = new UndoableHistoryManager();
    private int savedExecutedCount = 0;

    public IcoFileService()
    {
        modifiedSubject = new BehaviorSubject<bool>(false);
        fileNameSubject = new BehaviorSubject<string?>(null);

        baseFrames = new ItemCollection<Frame>();

        Frames = baseFrames.AsObservable();

        Modified = modifiedSubject.DistinctUntilChanged().Throttle(TimeSpan.FromMilliseconds(50)).AsObservable();
        FileName = fileNameSubject.Throttle(TimeSpan.FromMilliseconds(50)).AsObservable();
        CanUndo = undoableHistoryManager.CanUndo;
        CanRedo = undoableHistoryManager.CanRedo;

        InitSubscriptions();
    }

    public IObservable<bool> Modified { get; }

    public IObservable<string?> FileName { get; }

    public IObservable<ItemCollectionAction<Frame>> Frames { get; }

    public IObservable<bool> CanUndo { get; }

    public IObservable<bool> CanRedo { get; }

    public void Undo() => undoableHistoryManager.Undo();

    public void Redo() => undoableHistoryManager.Redo();

    public IAppCommand<T> CreateCommand<T>()
    {
        if (typeof(T) == typeof(FrameAddCommand.Parameters))
        {
            return (IAppCommand<T>)new FrameAddCommand(baseFrames)
            {
                UndoableHistoryManager = undoableHistoryManager
            };
        }
        else if (typeof(T) == typeof(FrameRemoveCommand.Parameters))
        {
            return (IAppCommand<T>)new FrameRemoveCommand(baseFrames)
            {
                UndoableHistoryManager = undoableHistoryManager
            };
        }
        else if (typeof(T) == typeof(FrameConvertCommand.Parameters))
        {
            return (IAppCommand<T>)new FrameConvertCommand(baseFrames)
            {
                UndoableHistoryManager = undoableHistoryManager
            };
        }
        else
        {
            throw new NotSupportedException($"{typeof(T).Name} is not supported");
        }
    }

    public Task CreateNew()
    {
        foreach (var i in baseFrames.List)
        {
            i.Dispose();
        }

        baseFrames.RemoveAll();

        undoableHistoryManager.Clear();
        fileNameSubject.OnNext(null);
        modifiedSubject.OnNext(false);

        return Task.CompletedTask;
    }

    public async Task Load(string fileName)
    {
        using var stream = File.OpenRead(fileName);

        var framesFromStream = IcoFile.Load(stream);

        var newFrames = framesFromStream.Select(x => CreateFrame(x)).ToArray();

        await baseFrames.SetAsync(newFrames);

        undoableHistoryManager.Clear();
        fileNameSubject.OnNext(fileName);
        modifiedSubject.OnNext(false);
    }

    private Frame CreateFrame(IIcoFileFrame frame)
    {
        using var imageStream = new MemoryStream(frame.ImageData.ToArray());

        if (frame is IcoFileBitmapFrame bitmap)
        {
            using var originalImageStream = new MemoryStream(bitmap.OriginalImageData.ToArray());
            using var maskStream = new MemoryStream(bitmap.ImageData.ToArray());

            return new FrameWithMask()
            {
                Width = frame.Width,
                Height = frame.Height,
                BitCount = bitmap.BitCount,
                Image = new ImageData(imageStream),
                OriginalImage = new ImageData(originalImageStream),
                MaskImage = new ImageData(maskStream)
            };
        }
        else
        {
            return new Frame()
            {
                Width = frame.Width,
                Height = frame.Height,
                Image = new ImageData(imageStream)
            };
        }
    }

    public async Task Save()
    {
        var fileName = fileNameSubject.Value;

        if (string.IsNullOrEmpty(fileName))
        {
            throw new InvalidOperationException("Can't save a file without a name.");
        }

        var framesToStream = baseFrames.List.Select(x => CreateFrame(x)).ToArray();

        using var stream = File.OpenWrite(fileName);
        IcoFile.Save(stream, framesToStream);

        savedExecutedCount = await undoableHistoryManager.ExecutedCount.FirstAsync();
        modifiedSubject.OnNext(false);
    }

    private IIcoFileFrame CreateFrame(Frame frame)
    {
        if (frame is FrameWithMask frameWithMask)
        {
            using var imageStream = frameWithMask.OriginalImage.GetStream();
            using var maskStream = frameWithMask.MaskImage?.GetStream();

            return IcoFileBitmapFrame.CreateFromImages(imageStream, maskStream);
        }
        else
        {
            return IcoFilePngFrame.CreateFromImage(frame.Image.GetStream());
        }
    }

    public async Task SaveAs(string fileName)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileName);

        fileNameSubject.OnNext(fileName);
        await Save();
    }

    public void Dispose()
    {
        if (!disposable.IsDisposed)
        {
            disposable.Dispose();
        }
    }

    private void InitSubscriptions()
    {
        undoableHistoryManager
            .ExecutedCount
            .Subscribe(x => modifiedSubject.OnNext(x != savedExecutedCount))
            .DisposeWith(disposable);
    }
}
