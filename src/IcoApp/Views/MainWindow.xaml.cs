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

using System.Collections.Immutable;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using IcoApp.Core;
using IcoApp.Core.Commands;
using IcoApp.Core.Helpers;
using IcoApp.Core.Models;
using IcoApp.Core.Services;
using IcoApp.Core.ViewModels;
using IcoApp.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using WinRT.Interop;

public partial class MainWindow : Window
{
    private readonly CompositeDisposable disposable = [];
    private readonly IAppService appService;
    private readonly ISettingsService settingsService;
    private readonly ISystemEventsService systemEventsService;
    private readonly IAppCommandManager appCommandManager;

    public MainWindow(
        IAppService appService,
        ISettingsService settingsService,
        ISystemEventsService systemEventsService,
        IAppCommandManager appCommandManager,
        IcoFileViewModel icoFileViewModel,
        FramesViewModel framesViewModel)
    {
        ArgumentNullException.ThrowIfNull(appService);
        ArgumentNullException.ThrowIfNull(settingsService);
        ArgumentNullException.ThrowIfNull(systemEventsService);
        ArgumentNullException.ThrowIfNull(appCommandManager);

        this.appService = appService;
        this.settingsService = settingsService;
        this.systemEventsService = systemEventsService;
        this.appCommandManager = appCommandManager;

        IcoFileViewModel = icoFileViewModel;
        FramesViewModel = framesViewModel;
        SettingsCommand = new RelayCommand(_ => appService.ShowSettings());

        this.InitializeComponent();

        ExtendsContentIntoTitleBar = true;

        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        AppWindow.Title = appService.AppInfo.ProductName;

        var icon = System.Drawing.Icon
            .ExtractAssociatedIcon(appService.ApplicationPath)!
            .DisposeWith(disposable);
        AppWindow.SetIcon(Win32Interop.GetIconIdFromIcon(icon.Handle));

        SetTitleBar(AppTitleBar);

        AppWindow.Closing += AppWindow_Closing;
        Closed += OnClosed;

        InitSubscriptions();
    }

    public IcoFileViewModel IcoFileViewModel { get; }

    public FramesViewModel FramesViewModel { get; }

    public RelayCommand SettingsCommand { get; }

    private void InitSubscriptions()
    {
        if (SynchronizationContext.Current == null)
        {
            throw new InvalidOperationException("SynchronizationContext.Current can't be null");
        }

        Observable
            .CombineLatest(
                settingsService.WindowTheme,
                systemEventsService.AppDarkTheme,
                (theme, isSystemDark) => theme == WindowTheme.Dark || theme == WindowTheme.System && isSystemDark)
            .DistinctUntilChanged()
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(isDarkTheme => this.UpdateTheme(isDarkTheme))
            .DisposeWith(disposable);
    }

    private async void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        args.Cancel = true;

        if (await IcoFileViewModel.ConfirmChanges() is true)
        {
            App.Current.Exit();
        }
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        if (!disposable.IsDisposed)
        {
            disposable.Dispose();
        }
    }

    private async void OnDragOver(object sender, DragEventArgs e)
    {
        var def = e.GetDeferral();

        try
        {
            var items = await e.DataView.GetStorageItemsAsync();
            var acceptedCount = items
                .Select(x => (x as StorageFile)?.Path)
                .Count(x => appService?.IsImageFileSupported(x) == true);

            if (acceptedCount > 0)
            {
                e.AcceptedOperation = DataPackageOperation.Link;
                e.DragUIOverride.Caption = acceptedCount == 1 ? "Add Frame" : "Add Frames";
                e.DragUIOverride.IsCaptionVisible = true;
                e.DragUIOverride.IsGlyphVisible = false;
            }
            else
            {
                e.AcceptedOperation = DataPackageOperation.None;
            }
        }
        finally
        {
            def.Complete();
        }
    }

    private async void OnDrop(object sender, DragEventArgs e)
    {
        var parameters = new FrameAddCommand.Parameters
        {
            FileNames = await GetDroppedFiles(e.DataView)
        };

        await appCommandManager.ExecuteAsync(parameters);
    }

    private async Task<IImmutableList<string>> GetDroppedFiles(DataPackageView dataView)
    {
        if (dataView.Contains(StandardDataFormats.StorageItems))
        {
            var items = await dataView.GetStorageItemsAsync();

            return items
                .Select(x => (x as StorageFile)?.Path)
                .Where(x => appService?.IsImageFileSupported(x) == true)
                .ToImmutableArray();
        }

        return ImmutableArray<string>.Empty;
    }
}
