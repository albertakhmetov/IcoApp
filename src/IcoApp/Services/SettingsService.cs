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
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using IcoApp.Core.Models;
using IcoApp.Core.Services;
using IcoApp.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;

internal class SettingsService : ISettingsService, IDisposable
{
    private readonly IServiceProvider serviceProvider;
    private readonly BehaviorSubject<WindowTheme> windowThemeSubject;
    private SettingsWindow? window;

    public SettingsService(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        this.serviceProvider = serviceProvider;

        windowThemeSubject = new BehaviorSubject<WindowTheme>(Core.Models.WindowTheme.System);

        WindowTheme = windowThemeSubject.AsObservable();
    }

    public IObservable<WindowTheme> WindowTheme { get; }

    public void SetWindowTheme(WindowTheme windowTheme)
    {
        windowThemeSubject.OnNext(windowTheme);
    }

    public void Show()
    {
        if (window == null)
        {
            window = serviceProvider.GetRequiredService<SettingsWindow>();
            window.AppWindow.Closing += OnWindowClosing;
        }

        window.AppWindow.Show();
    }

    public void Dispose()
    {
        if (window?.AppWindow != null)
        {
            window.AppWindow.Closing -= OnWindowClosing;
            window.Close();
        }
    }

    private void OnWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        args.Cancel = true;

        sender?.Hide();
    }
}
