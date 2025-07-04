/*  Copyright � 2025, Albert Akhmetov <akhmetov@live.com>   
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
namespace IcoApp;

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using IcoApp.Core.Commands;
using IcoApp.Core.Helpers;
using IcoApp.Core.Models;
using IcoApp.Core.Services;
using IcoApp.Core.ViewModels;
using IcoApp.Helpers;
using IcoApp.Services;
using IcoApp.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppLifecycle;
using WinRT.Interop;

public partial class App : Application
{
    [STAThread]
    public static void Main(string[] args)
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();

        Start(_ =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);

            instance = new App(args);
        });

        instance?.host?.StopAsync().Wait();
        instance?.host?.Dispose();
    }

    private static App? instance;

    private readonly ImmutableArray<string> arguments;
    private IHost host;
    private MainWindow? mainWindow;

    public App(string[] args)
    {
        host = CreateHost();

        this.arguments = ImmutableArray.Create(args);

        InitializeComponent();

        var theme = host.Services.GetRequiredService<ISettingsService>().WindowTheme.Value;

        switch (theme)
        {
            case WindowTheme.Dark:
                RequestedTheme = ApplicationTheme.Dark;
                break;

            case WindowTheme.Light:
                RequestedTheme = ApplicationTheme.Light;
                break;
        }

    }

    public nint? Handle => mainWindow == null ? null : WindowNative.GetWindowHandle(mainWindow);

    public XamlRoot? XamlRoot => mainWindow?.Content?.XamlRoot;

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        mainWindow = host.Services.GetRequiredService<MainWindow>();
        mainWindow.AppWindow.Show();

        _ = host.RunAsync();

        if (arguments.IsEmpty is false)
        {
            host.Services.GetRequiredService<IIcoFileService>().Load(arguments.First());
        }
    }

    private IHost CreateHost()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Services.AddSingleton<MainWindow>();
        builder.Services.AddSingleton<SettingsWindow>();

        builder.Services.AddSingleton<IAppService, AppService>();
        builder.Services.AddSingleton<IFileService, FileService>();
        builder.Services.AddSingleton<IIcoFileService, IcoFileService>();
        builder.Services.AddSingleton<ISettingsService, SettingsService>();
        builder.Services.AddSingleton<ISystemEventsService, SystemEventsService>();

        builder.Services.AddSingleton<IAppCommandManager, AppCommandManager>();
        builder.Services.AddTransient(x => x.GetRequiredService<IIcoFileService>().CreateCommand<FrameAddCommand.Parameters>());
        builder.Services.AddTransient(x => x.GetRequiredService<IIcoFileService>().CreateCommand<FrameRemoveCommand.Parameters>());
        builder.Services.AddTransient(x => x.GetRequiredService<IIcoFileService>().CreateCommand<FrameConvertCommand.Parameters>());

        builder.Services.AddTransient<IAppCommand<ImageDataExportCommand.Parameters>, ImageDataExportCommand>();

        builder.Services.AddSingleton<IcoFileViewModel>();
        builder.Services.AddSingleton<FramesViewModel>();
        builder.Services.AddSingleton<SettingsViewModel>();

        builder.Services.AddKeyedSingleton<UserControl, ConfirmationDialogView>(nameof(ConfirmationDialogViewModel));

        return builder.Build();
    }
}