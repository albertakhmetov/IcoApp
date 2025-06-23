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
using System.Windows.Input;

namespace IcoApp.Core.ViewModels;

public abstract class DialogViewModel : ObservableObject
{
    private bool isPrimaryEnabled;
    private string primaryText, closeText;

    protected DialogViewModel()
    {
        PrimaryCommand = new RelayCommand(OnPrimaryExecuted);

        isPrimaryEnabled = true;
        primaryText = "Ok";
        closeText = "Cancel";
    }

    public ICommand PrimaryCommand { get; }

    public bool IsPrimaryEnabled
    {
        get => isPrimaryEnabled;
        protected set => Set(ref isPrimaryEnabled, value);
    }

    public string PrimaryText
    {
        get => primaryText;
        protected set => Set(ref primaryText, value);
    }

    public string CloseText
    {
        get => closeText;
        protected set => Set(ref closeText, value);
    }

    protected abstract void OnPrimaryExecuted(object? parameter);
}