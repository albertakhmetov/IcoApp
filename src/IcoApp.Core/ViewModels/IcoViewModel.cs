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
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using IcoApp.Core.Helpers;
using IcoApp.Core.Services;

public class IcoViewModel : ViewModel, IDisposable
{
    private readonly CompositeDisposable disposable = [];

    private readonly IIcoService icoService;

    private string? fileName;
    private bool isModified;

    public IcoViewModel(IIcoService icoService)
    {
        ArgumentNullException.ThrowIfNull(icoService);

        this.icoService = icoService;

        fileName = null;
        isModified = false;

        NewFileCommand = new RelayCommand(_ => NewFile());
        OpenFileCommand = new RelayCommand(_ => OpenFile());
        SaveFileCommand = new RelayCommand(_ => SaveFile());

        InitSubscriptions();
    }

    public string Name => (string.IsNullOrEmpty(FileName) ? "Noname" : FileName) + (IsModified ? "*" : "");

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

    private void NewFile()
    {
        throw new NotImplementedException();
    }

    private void OpenFile()
    {
        throw new NotImplementedException();
    }

    private void SaveFile()
    {
        throw new NotImplementedException();
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
            .Modified
            .CombineLatest(icoService.FileName)
            .Throttle(TimeSpan.FromMilliseconds(200))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(_ => Invalidate(nameof(Name)))
            .DisposeWith(disposable);

        icoService
            .Modified
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => IsModified = x)
            .DisposeWith(disposable);

        icoService
            .FileName
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => FileName = x)
            .DisposeWith(disposable);
    }
}
