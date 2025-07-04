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
namespace IcoApp.Core.ViewModels;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using IcoApp.Core.Helpers;
using IcoApp.Core.Services;

public class IcoFileViewModel : ViewModel, IDisposable
{
    private readonly CompositeDisposable disposable = [];

    private readonly IIcoFileService icoFileService;
    private readonly IFileService fileService;
    private readonly IAppService appService;

    private string? fileName;
    private bool isModified;

    public IcoFileViewModel(IIcoFileService icoFileService, IFileService fileService, IAppService appService)
    {
        ArgumentNullException.ThrowIfNull(icoFileService);
        ArgumentNullException.ThrowIfNull(fileService);
        ArgumentNullException.ThrowIfNull(appService);

        this.icoFileService = icoFileService;
        this.fileService = fileService;
        this.appService = appService;

        fileName = null;
        isModified = false;

        NewFileCommand = new RelayCommand(_ => NewFile());
        OpenFileCommand = new RelayCommand(_ => OpenFile());
        SaveFileCommand = new RelayCommand(_ => SaveFile());

        UndoCommand = new RelayCommand(_ => this.icoFileService.Undo());
        RedoCommand = new RelayCommand(_ => this.icoFileService.Redo());

        InitSubscriptions();
    }

    public string Name =>
        (string.IsNullOrEmpty(FileName) ? "Noname" : Path.GetFileNameWithoutExtension(FileName)) +
        (IsModified ? "*" : "");

    public string? FileName
    {
        get => fileName;
        private set => Set(ref fileName, value);
    }

    public bool IsModified
    {
        get => isModified;
        private set => Set(ref isModified, value);
    }

    public RelayCommand NewFileCommand { get; }

    public RelayCommand OpenFileCommand { get; }

    public RelayCommand SaveFileCommand { get; }

    public RelayCommand UndoCommand { get; }

    public RelayCommand RedoCommand { get; }

    public void Dispose()
    {
        if (!disposable.IsDisposed)
        {
            disposable.Dispose();
        }
    }

    public async Task<bool> ConfirmChanges()
    {
        if (await icoFileService.Modified.FirstAsync() is true)
        {
            var isConfirmed = await appService.Show(new ConfirmationDialogViewModel
            {
                Text = "The icon has been modified. All unsaved changes will be discarded. Proceed?",
                IconGlyph = "\uE9CE",
                ConfirmationText = "Proceed"
            });

            if (isConfirmed is false)
            {
                return false;
            }
        }

        return true;
    }

    private void InitSubscriptions()
    {
        if (SynchronizationContext.Current == null)
        {
            throw new InvalidOperationException("SynchronizationContext.Current can't be null");
        }

        icoFileService
            .Modified
            .CombineLatest(icoFileService.FileName)
            .Throttle(TimeSpan.FromMilliseconds(200))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(_ => Invalidate(nameof(Name)))
            .DisposeWith(disposable);

        icoFileService
            .Modified
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => IsModified = x)
            .DisposeWith(disposable);

        icoFileService
            .FileName
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => FileName = x)
            .DisposeWith(disposable);

        icoFileService
            .CanUndo
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(canUndo => UndoCommand.IsEnabled = canUndo)
            .DisposeWith(disposable);

        icoFileService
            .CanRedo
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(canRedo => RedoCommand.IsEnabled = canRedo)
            .DisposeWith(disposable);
    }

    private async void NewFile()
    {
        if (await ConfirmChanges() is false)
        {
            return;
        }

        await icoFileService.CreateNew();
    }

    private async void OpenFile()
    {
        if (await ConfirmChanges() is false)
        {
            return;
        }

        var fileName = await fileService.PickFileForOpenAsync(appService.SupportedFileTypes);

        if (string.IsNullOrEmpty(fileName) is false)
        {
            await icoFileService.Load(fileName);
        }
    }

    private async void SaveFile()
    {
        if (string.IsNullOrEmpty(FileName) is false)
        {
            await icoFileService.Save();
        }
        else
        {
            var fileName = await fileService.PickFileForSaveAsync(appService.SupportedFileTypes);

            if (string.IsNullOrEmpty(fileName) is false)
            {
                await icoFileService.SaveAs(fileName);
            }
        }
    }
}
