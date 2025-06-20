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
using IcoApp.Core.Commands;
using IcoApp.Core.Helpers;

public class URViewModel : ViewModel
{
    private readonly CompositeDisposable disposable = [];
    private readonly IAppCommandManager appCommandManager;

    private bool canUndo, canRedo;

    public URViewModel(IAppCommandManager appCommandManager)
    {
        ArgumentNullException.ThrowIfNull(appCommandManager);

        this.appCommandManager = appCommandManager;

        UndoCommand = new RelayCommand(_ => Undo());
        RedoCommand = new RelayCommand(_ => Redo());

        InitSubscriptions();
    }

    public bool CanUndo
    {
        get => canUndo;
        private set => Set(ref canUndo, value);
    }

    public bool CanRedo
    {
        get => canRedo;
        private set => Set(ref canRedo, value);
    }

    public RelayCommand UndoCommand { get; }

    public RelayCommand RedoCommand { get; }

    private void Undo()
    {
        throw new NotImplementedException();
    }

    private void Redo()
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

        appCommandManager
            .CanUndo
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => CanUndo = x)
            .DisposeWith(disposable);

        appCommandManager
            .CanRedo
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => CanRedo = x)
            .DisposeWith(disposable);
    }
}
