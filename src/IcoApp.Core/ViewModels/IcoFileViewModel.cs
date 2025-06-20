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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IcoApp.Core.Services;

public class IcoFileViewModel : ViewModel
{
    private readonly IIcoFileService icoFileService;

    public IcoFileViewModel(IIcoFileService icoFileService)
    {
        ArgumentNullException.ThrowIfNull(icoFileService);

        this.icoFileService = icoFileService;

        NewFileCommand = new RelayCommand(_ => NewFile());
        OpenFileCommand = new RelayCommand(_ => OpenFile());
        SaveFileCommand = new RelayCommand(_ => SaveFile());
    }

    public string FileName { get; }

    public bool IsModified { get; }

    public RelayCommand NewFileCommand { get; }

    public RelayCommand OpenFileCommand { get; }

    public RelayCommand SaveFileCommand { get; }

    private void NewFile()
    {
        throw new NotImplementedException();
    }

    private void OpenFile()
    {
        throw new NotImplementedException();
    }

    private void SaveFile()
    {
        throw new NotImplementedException();
    }
}
