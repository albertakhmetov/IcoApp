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
using IcoApp.Core.Helpers;
using IcoApp.Core.Models;
using IcoApp.Core.Services;

public class SettingsViewModel : ViewModel
{
    private readonly CompositeDisposable disposable = [];

    private readonly ISettingsService settingsService;
    private readonly IAppService appService;

    private WindowTheme windowTheme;

    public SettingsViewModel(ISettingsService settingsService, IAppService appService)
    {
        ArgumentNullException.ThrowIfNull(settingsService);
        ArgumentNullException.ThrowIfNull(appService);

        this.settingsService = settingsService;
        this.appService = appService;

        WindowThemes = [WindowTheme.System, WindowTheme.Dark, WindowTheme.Light];

        InitSubscriptions();
    }

    public WindowTheme WindowTheme
    {
        get => windowTheme;
        set => settingsService.SetWindowTheme(value);
    }

    public IImmutableList<WindowTheme> WindowThemes { get; }

    public string ProductName => appService.AppInfo.ProductName;

    public string ProductVersion => appService.AppInfo.FileVersion.ToString();

    public string LegalCopyright => appService.AppInfo.LegalCopyright ?? string.Empty;

    public string ProductDescription => appService.AppInfo.ProductDescription ?? string.Empty;

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

        settingsService
            .WindowTheme
            .DistinctUntilChanged()
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x =>
            {
                windowTheme = x;
                Invalidate(nameof(WindowTheme));
            })
            .DisposeWith(disposable);
    }
}
