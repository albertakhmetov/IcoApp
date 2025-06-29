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

using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using IcoApp.Core;
using IcoApp.Core.Helpers;
using IcoApp.Core.Models;
using IcoApp.Core.Services;
using IcoApp.Core.ViewModels;
using IcoApp.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinRT.Interop;

public partial class MainWindow : Window
{
    private readonly CompositeDisposable disposable = [];
    private readonly ISettingsService settingsService;
    private readonly ISystemEventsService systemEventsService;

    public MainWindow(
        IAppService appService,
        ISettingsService settingsService,
        ISystemEventsService systemEventsService,
        IcoViewModel icoViewModel,
        IcoFramesViewModel icoFramesViewModel,
        URViewModel urViewModel)
    {
        ArgumentNullException.ThrowIfNull(appService);
        ArgumentNullException.ThrowIfNull(settingsService);
        ArgumentNullException.ThrowIfNull(systemEventsService);

        this.settingsService = settingsService;
        this.systemEventsService = systemEventsService;

        IcoViewModel = icoViewModel;
        IcoFramesViewModel = icoFramesViewModel;
        URViewModel = urViewModel;
        SettingsCommand = new RelayCommand(_ => appService.ShowSettings());

        this.InitializeComponent();

        ExtendsContentIntoTitleBar = true;

        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        AppWindow.SetIcon("Assets/IcoApp.ico");
        AppWindow.Title = appService.AppInfo.ProductName;

        SetTitleBar(AppTitleBar);

        AppWindow.Closing += AppWindow_Closing;
        Closed += OnClosed;

        InitSubscriptions();
    }

    public IcoViewModel IcoViewModel { get; }

    public IcoFramesViewModel IcoFramesViewModel { get; }

    public URViewModel URViewModel { get; }

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

        if (await IcoViewModel.ConfirmChanges() is true)
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
}
