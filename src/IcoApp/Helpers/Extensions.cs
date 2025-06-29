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
namespace IcoApp.Helpers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using IcoApp.Core.Models;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Windows.Services.Store;
using Windows.UI;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;

internal static class Extensions
{
    public static void Bind(this FrameworkElement element, DependencyProperty property, string path, BindingMode mode)
    {
        var binding = new Binding
        {
            Path = new PropertyPath(path),
            Mode = mode
        };

        element.SetBinding(property, binding);
    }

    public static Color ToColor(this string hexValue)
    {
        ArgumentException.ThrowIfNullOrEmpty(hexValue);

        var value = hexValue.StartsWith('#') ? hexValue.AsSpan(1) : hexValue.AsSpan();
        if (value.Length != 6 && value.Length != 8)
        {
            throw new ArgumentOutOfRangeException(nameof(hexValue));
        }

        byte alpha = 0xFF;
        if (value.Length == 8)
        {
            alpha = byte.Parse(value.Slice(0, 2), System.Globalization.NumberStyles.HexNumber);
            value = value.Slice(2);
        }

        var red = byte.Parse(value.Slice(0, 2), System.Globalization.NumberStyles.HexNumber);
        var green = byte.Parse(value.Slice(2, 2), System.Globalization.NumberStyles.HexNumber);
        var blue = byte.Parse(value.Slice(4, 2), System.Globalization.NumberStyles.HexNumber);

        return Color.FromArgb(alpha, red, green, blue);
    }

    public static async void UpdateTheme(this Window window, bool isDarkTheme)
    {
        if (window.Content is FrameworkElement element)
        {
            element.RequestedTheme = isDarkTheme ? ElementTheme.Dark : ElementTheme.Light;
            window.AppWindow.TitleBar.PreferredTheme = isDarkTheme ? TitleBarTheme.Dark : TitleBarTheme.Light;

            await Task.Delay(TimeSpan.FromMilliseconds(100));

            FixButtons(window.AppWindow.TitleBar);
        }
    }

    private static void FixButtons(AppWindowTitleBar titleBar)
    {
        ArgumentNullException.ThrowIfNull(titleBar);

        var isDarkTheme = titleBar.PreferredTheme == TitleBarTheme.Dark;

        if (isDarkTheme)
        {
            titleBar.ButtonForegroundColor = "#FFFFFF".ToColor();
            titleBar.ButtonHoverForegroundColor = "#FFFFFF".ToColor();
            titleBar.ButtonHoverBackgroundColor = "#0FFFFFFF".ToColor();
        }
        else
        {
            titleBar.ButtonForegroundColor = "#191919".ToColor();
            titleBar.ButtonHoverForegroundColor = "#191919".ToColor();
            titleBar.ButtonHoverBackgroundColor = "#09000000".ToColor();
        }
    }

    [DllImport("UxTheme.dll", EntryPoint = "#132", SetLastError = true)]
    private static extern bool ShouldAppsUseDarkMode();
}
