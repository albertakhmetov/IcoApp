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
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IcoApp.Core.Models;
using IcoApp.Core.Services;

public class IcoFileFramesViewModel : ViewModel
{
    private readonly IIcoFileService icoFileService;

    private ObservableCollection<IcoFileFrame> baseFrames;
    private IcoFileFrame? currentFrame;

    public IcoFileFramesViewModel(IIcoFileService icoFileService)
    {
        ArgumentNullException.ThrowIfNull(icoFileService);

        this.icoFileService = icoFileService;

        baseFrames = new ObservableCollection<IcoFileFrame>();
        Frames = new ReadOnlyObservableCollection<IcoFileFrame>(baseFrames);

        AddFrameCommand = new RelayCommand(_ => AddFrame());
        RemoveFrameCommand = new RelayCommand(x => RemoveFrame(x as IcoFileFrame));
    }

    public ReadOnlyObservableCollection<IcoFileFrame> Frames { get; }

    public IcoFileFrame? CurrentFrame
    {
        get => currentFrame;
        set => Set(ref currentFrame, value);
    }

    public RelayCommand AddFrameCommand { get; }

    public RelayCommand RemoveFrameCommand { get; }    
    
    private void AddFrame()
    {
        throw new NotImplementedException();
    }

    private void RemoveFrame(IcoFileFrame? icoFileFrame)
    {
        throw new NotImplementedException();
    }
}
