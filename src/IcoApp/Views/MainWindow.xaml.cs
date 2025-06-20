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

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using IcoApp.Core.Services;
using IcoApp.Core.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinRT.Interop;

public partial class MainWindow : Window, IAppWindow
{
    public MainWindow(
        IcoViewModel icoViewModel, 
        IcoFramesViewModel icoFramesViewModel,
        URViewModel urViewModel)
    {
        IcoViewModel = icoViewModel;
        IcoFramesViewModel = icoFramesViewModel;
        URViewModel = urViewModel;

        this.InitializeComponent();

        ExtendsContentIntoTitleBar = true;

        //var presenter = OverlappedPresenter.Create();
        ////presenter.PreferredMinimumWidth = 600;
        ////presenter.PreferredMaximumWidth = 800;
        ////presenter.PreferredMinimumHeight = 600;
        //presenter.SetBorderAndTitleBar(true, true);

        // AppWindow.SetPresenter(presenter);
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        SetTitleBar(AppTitleBar);
    }

    public nint Handle => WindowNative.GetWindowHandle(this);

    public IcoViewModel IcoViewModel { get; }

    public IcoFramesViewModel IcoFramesViewModel { get; }

    public URViewModel URViewModel { get; }

    public void Show()
    {
        AppWindow.Show(true);
    }
}
