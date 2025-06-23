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
namespace IcoApp.Views;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using IcoApp.Core.Helpers;
using IcoApp.Core.Models;
using IcoApp.Core.Services;
using IcoApp.Core.ViewModels;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;

public sealed partial class SettingsWindow : Window
{
    private readonly CompositeDisposable disposable = [];
    private readonly ISettingsService settingsService;

    public SettingsWindow(ISettingsService settingsService, SettingsViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(settingsService);
        ArgumentNullException.ThrowIfNull(viewModel);

        this.settingsService = settingsService;

        ViewModel = viewModel;

        InitializeComponent();

        var presenter = OverlappedPresenter.Create();
        presenter.PreferredMinimumWidth = 600;
        presenter.PreferredMaximumWidth = 800;
        presenter.PreferredMinimumHeight = 600;
        presenter.IsMinimizable = false;
        presenter.IsMaximizable = false;
        presenter.SetBorderAndTitleBar(true, true);
        AppWindow.SetPresenter(presenter);

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        Closed += OnClosed;

        InitSubscriptions();
    }

    public SettingsViewModel ViewModel { get; }

    private void InitSubscriptions()
    {
        if (SynchronizationContext.Current == null)
        {
            throw new InvalidOperationException("SynchronizationContext.Current can't be null");
        }

        settingsService
            .WindowTheme
            .DistinctUntilChanged()
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => UpdateWindowTheme(x))
            .DisposeWith(disposable);
    }

    private void UpdateWindowTheme(WindowTheme x)
    {
        if (Content is Grid grid)
        {
            grid.RequestedTheme = x switch
            {
                WindowTheme.Dark => ElementTheme.Dark,
                WindowTheme.Light => ElementTheme.Light,
                _ => ElementTheme.Default
            };
        }
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        if (!disposable.IsDisposed)
        {
            disposable.Dispose();
        }
    }
}
