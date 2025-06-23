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
namespace IcoApp;

using System;
using System.Collections.Immutable;
using System.Text;
using IcoApp.Core.Commands;
using IcoApp.Core.Services;
using IcoApp.Core.ViewModels;
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
    private IHost? host;
    private MainWindow? mainWindow;

    public App(string[] args)
    {
        this.arguments = ImmutableArray.Create(args);

        InitializeComponent();
    }

    public nint? Handle => mainWindow == null ? null : WindowNative.GetWindowHandle(mainWindow);

    public XamlRoot? XamlRoot => mainWindow?.Content?.XamlRoot;

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        host = CreateHost();

        mainWindow = host.Services.GetRequiredService<MainWindow>();
        mainWindow.AppWindow.Show();

        _ = host.RunAsync();
    }

    private IHost CreateHost()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Services.AddSingleton<MainWindow>();
        builder.Services.AddSingleton<SettingsWindow>();

        builder.Services.AddSingleton<IAppService, AppService>();
        builder.Services.AddSingleton<IFileService, FileService>();
        builder.Services.AddSingleton<IIcoService, IcoService>();
        builder.Services.AddSingleton<ISettingsService, SettingsService>();

        builder.Services.AddSingleton<IAppCommandManager, AppCommandManager>();
        builder.Services
            .AddTransient<IAppCommand<IcoFrameAddCommand.Parameters>, IcoFrameAddCommand>();
        builder.Services
            .AddTransient<IAppCommand<IcoFrameRemoveCommand.Parameters>, IcoFrameRemoveCommand>();
        builder.Services
            .AddTransient<IAppCommand<ImageDataExportCommand.Parameters>, ImageDataExportCommand>();

        builder.Services.AddSingleton<IcoViewModel>();
        builder.Services.AddSingleton<IcoFramesViewModel>();
        builder.Services.AddSingleton<URViewModel>();
        builder.Services.AddSingleton<SettingsViewModel>();

        builder.Services.AddKeyedSingleton<UserControl, ConfirmationDialogView>(nameof(ConfirmationDialogViewModel));

        return builder.Build();
    }
}