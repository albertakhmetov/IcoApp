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
namespace IcoApp.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using IcoApp.Core.Commands;
using IcoApp.Core.Helpers;
using IcoApp.Core.Models;
using IcoApp.Core.Services;

internal class IcoService : IIcoService, IDisposable
{
    private readonly CompositeDisposable disposable = [];
    private readonly IAppCommandManager appCommandManager;

    private readonly BehaviorSubject<bool> modifiedSubject;
    private readonly BehaviorSubject<string?> fileNameSubject;

    private int savedExecutedCount = 0;

    public IcoService(IAppCommandManager appCommandManager)
    {
        ArgumentNullException.ThrowIfNull(appCommandManager);

        this.appCommandManager = appCommandManager;

        modifiedSubject = new BehaviorSubject<bool>(false);
        fileNameSubject = new BehaviorSubject<string?>(null);

        Frames = new ItemCollection<IcoFrame>();

        Modified = modifiedSubject.DistinctUntilChanged().Throttle(TimeSpan.FromMilliseconds(50)).AsObservable();
        FileName = fileNameSubject.Throttle(TimeSpan.FromMilliseconds(50)).AsObservable();

        InitSubscriptions();
    }

    public IObservable<bool> Modified { get; }

    public IObservable<string?> FileName { get; }

    public ItemCollectionBase<IcoFrame> Frames { get; }

    public Task CreateNew()
    {
        foreach (var i in Frames.List)
        {
            i.Dispose();
        }

        Frames.RemoveAll();

        fileNameSubject.OnNext(null);
        modifiedSubject.OnNext(false);

        return Task.CompletedTask;
    }

    public async Task Load(string fileName)
    {
        using var stream = File.OpenRead(fileName);

        var framesFromStream = new FileFormat.IcoFile().Load(stream);

        var newFrames = framesFromStream.Select(x => CreateFrame(x)).ToArray();

        await Frames.SetAsync(newFrames);

        foreach (var x in framesFromStream)
        {
            x.Dispose();
        }
    }

    private IcoFrame CreateFrame(FileFormat.IcoFrame frame)
    {
        using var imageStream = new MemoryStream(frame.ImageData.ToArray());

        if (frame is FileFormat.IcoBitmapFrame bitmap)
        { 
            using var originalImageStream = new MemoryStream(bitmap.OriginalImageData.ToArray());
            using var maskStream = new MemoryStream(bitmap.ImageData.ToArray());

            return new IcoFrame(frame.Width, frame.Height, bitmap.BitCount, originalImageStream, imageStream)
            {
                MaskImage = new ImageData(maskStream)
            };
        }
        else
        {
            return new IcoFrame(frame.Width, frame.Height, imageStream);
        }
    }

    public Task Save()
    {
        var fileName = fileNameSubject.Value;

        if (string.IsNullOrEmpty(fileName))
        {
            throw new InvalidOperationException("Can't save a file without a name.");
        }

        var framesToStream = Frames.List.Select(x => CreateFrame(x)).ToArray();

        using var stream = File.OpenWrite(fileName);
        new FileFormat.IcoFile().Save(stream, framesToStream);

        foreach (var x in framesToStream)
        {
            x.Dispose();
        }

        return Task.CompletedTask;
    }

    private FileFormat.IcoFrame CreateFrame(IcoFrame frame)
    {
        if (frame.Type == IcoFrameType.Bitmap)
        {
            using var imageStream = frame.OriginalImage.GetStream();
            using var maskStream = frame.MaskImage?.GetStream();

            return FileFormat.IcoBitmapFrame.CreateFromImages(imageStream, maskStream);
        }
        else
        {
            return FileFormat.IcoPngFrame.CreateFromImage(frame.OriginalImage.GetStream());
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
        appCommandManager
            .ExecutedCount
            .Subscribe(x => modifiedSubject.OnNext(x != savedExecutedCount))
            .DisposeWith(disposable);
    }
}
