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
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using IcoApp.Core.Models;
using IcoApp.Core.Services;
using IcoApp.Core.ViewModels;
using IcoApp.Helpers;
using IcoApp.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

internal class AppService : IAppService
{
    private readonly IServiceProvider serviceProvider;

    private SettingsWindow? settingsWindow;

    public AppService(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        this.serviceProvider = serviceProvider;

        AppInfo = LoadAppInfo();
    }

    public AppInfo AppInfo { get; }

    public IImmutableList<FileType> SupportedImageTypes { get; } = [FileType.Png, FileType.Bmp];

    public IImmutableList<FileType> SupportedFileTypes { get; } = [FileType.Ico];

    public nint Handle => (App.Current as App)?.Handle ?? nint.Zero;

    public string UserDataPath { get; } = "./"; // todo: replace to user/local folder

    public bool IsImageFileSupported(string? fileName)
    {
        var ext = Path.GetExtension(fileName);

        return ext is not null && SupportedImageTypes.Any(x => x.Equals(ext));
    }

    public async Task ShowSettings()
    {
        if (settingsWindow is null)
        {
            settingsWindow = serviceProvider.GetRequiredService<SettingsWindow>();
            settingsWindow.AppWindow.Closing += OnSettingsWindowClosing;

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }

        settingsWindow.AppWindow.Show(true);
    }

    public async Task<bool> Show(DialogViewModel viewModel)
    {
        var xamlRoot = (App.Current as App)?.XamlRoot;
        if (xamlRoot == null)
        {
            throw new InvalidOperationException("The app isn't initialized.");
        }

        var dialog = new ContentDialog
        {
            DataContext = viewModel,
            Content = serviceProvider.GetRequiredKeyedService<UserControl>(viewModel.GetType().Name),
            XamlRoot = xamlRoot
        };

        dialog.DefaultButton = ContentDialogButton.Primary;

        dialog.Bind(
            ContentDialog.PrimaryButtonCommandProperty,
            nameof(DialogViewModel.PrimaryCommand),
            BindingMode.OneTime);

        dialog.Bind(
            ContentDialog.IsPrimaryButtonEnabledProperty,
            nameof(DialogViewModel.IsPrimaryEnabled),
            BindingMode.OneWay);

        dialog.Bind(
            ContentDialog.PrimaryButtonTextProperty,
            nameof(DialogViewModel.PrimaryText),
            BindingMode.OneWay);

        dialog.Bind(
            ContentDialog.CloseButtonTextProperty,
            nameof(DialogViewModel.CloseText),
            BindingMode.OneWay);

        var result = await dialog.ShowAsync();

        dialog.Content = null;

        return result == ContentDialogResult.Primary;
    }

    private AppInfo LoadAppInfo()
    {
        var info = FileVersionInfo.GetVersionInfo(typeof(AppService).Assembly.Location);

        return new AppInfo
        {
            ProductName = info.ProductName ?? "IcoApp",
            ProductVersion = info.ProductVersion,
            ProductDescription = info.Comments,
            LegalCopyright = info.LegalCopyright,
            FileVersion = new Version(
                info.FileMajorPart,
                info.FileMinorPart,
                info.FileBuildPart,
                info.FilePrivatePart),

            IsPreRelease = Regex.IsMatch(info.ProductVersion ?? "", "[a-zA-Z]")
        };
    }

    private void OnSettingsWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        args.Cancel = true;

        sender.Hide();
    }
}
