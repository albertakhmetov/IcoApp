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
namespace IcoApp.Core.ViewModels;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using IcoApp.Core.Commands;
using IcoApp.Core.Helpers;
using IcoApp.Core.Models;
using IcoApp.Core.Services;
using Microsoft.Extensions.Hosting;

public class IcoFramesViewModel : ViewModel, IDisposable
{
    private readonly CompositeDisposable disposable = [];

    private readonly IIcoService icoService;
    private readonly IAppService appService;
    private readonly IFileService fileService;
    private readonly IAppCommandManager appCommandManager;

    private ItemObservableCollection<IcoFrameViewModel> baseItems;
    private IcoFrameViewModel? currentItem;

    public IcoFramesViewModel(
        IIcoService icoService,
        IAppService appService,
        IFileService fileService,
        IAppCommandManager appCommandManager)
    {
        ArgumentNullException.ThrowIfNull(icoService);
        ArgumentNullException.ThrowIfNull(appService);
        ArgumentNullException.ThrowIfNull(fileService);
        ArgumentNullException.ThrowIfNull(appCommandManager);

        this.icoService = icoService;
        this.appService = appService;
        this.fileService = fileService;
        this.appCommandManager = appCommandManager;

        baseItems = [];
        Items = new ReadOnlyObservableCollection<IcoFrameViewModel>(baseItems);

        AddFrameCommand = new RelayCommand(_ => AddFrame());
        RemoveFrameCommand = new RelayCommand(x => RemoveFrame((x as IcoFrameViewModel)?.Frame));
        RemoveAllFramesCommand = new RelayCommand(x => RemoveAllFrames());
        ExportFrameCommand = new RelayCommand(x => ExportFrame((x as IcoFrameViewModel)?.Frame));

        InitSubscriptions();
    }

    public ReadOnlyObservableCollection<IcoFrameViewModel> Items { get; }

    public bool IsEmpty => Items.Count == 0;

    public IcoFrameViewModel? CurrentItem
    {
        get => currentItem;
        set => Set(ref currentItem, value);
    }

    public RelayCommand AddFrameCommand { get; }

    public RelayCommand RemoveFrameCommand { get; }

    public RelayCommand RemoveAllFramesCommand { get; }

    public RelayCommand ExportFrameCommand { get; }

    private async void AddFrame()
    {
        var fileNames = await fileService.PickFilesForOpenAsync(appService.SupportedImageTypes);
        if (fileNames.Count > 0)
        {
            await appCommandManager.ExecuteAsync(new IcoFrameAddCommand.Parameters
            {
                FileNames = fileNames.ToImmutableArray()
            });
        }
    }

    private async void RemoveFrame(IcoFrame? frame)
    {
        if (frame is not null)
        {
            await appCommandManager.ExecuteAsync(new IcoFrameRemoveCommand.Parameters
            {
                Frames = ImmutableArray.Create([frame])
            });
        }
    }

    private async void RemoveAllFrames()
    {
        await appCommandManager.ExecuteAsync(new IcoFrameRemoveCommand.Parameters
        {
            RemoveAll = true
        });
    }

    private async void ExportFrame(IcoFrame? frame)
    {
        if (frame == null)
        {
            return;
        }

        var fileType = frame.Type == IcoFrameType.Bitmap ? FileType.Bmp : FileType.Png;

        var fileName = await fileService.PickFileForSaveAsync([fileType], $"frame{fileType.Extension}");

        if (string.IsNullOrEmpty(fileName) is false)
        {
            await appCommandManager.ExecuteAsync(new ImageDataExportCommand.Parameters
            {
                Image = frame.Image,
                FileName = fileName
            });
        }
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
        if (SynchronizationContext.Current == null)
        {
            throw new InvalidOperationException("SynchronizationContext.Current can't be null");
        }

        icoService
            .Frames
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(UpdateItems)
            .DisposeWith(disposable);
    }

    private void UpdateItems(ItemCollectionAction<IcoFrame> action)
    {
        switch (action.Type)
        {
            case ItemCollectionActionType.Reset:
                baseItems.Set(action.Items.Select(x => new IcoFrameViewModel(x, ExportFrameCommand, RemoveFrameCommand)));
                break;

            case ItemCollectionActionType.Add:
                action
                    .Items
                    .Select(x => new IcoFrameViewModel(x, ExportFrameCommand, RemoveFrameCommand))
                    .ForEach(x => baseItems.Add(x));
                break;

            case ItemCollectionActionType.Remove:
                var itemToRemove = baseItems.FirstOrDefault(x => x.Frame.Equals(action.Items[0]));

                if (itemToRemove != null)
                {
                    baseItems.Remove(itemToRemove);
                }

                break;
        }

        Invalidate(nameof(IsEmpty));
    }
}
