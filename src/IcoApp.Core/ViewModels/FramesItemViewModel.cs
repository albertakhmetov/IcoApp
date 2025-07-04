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
using IcoApp.Core.Models;

public class FramesItemViewModel : ViewModel, IComparable<FramesItemViewModel>
{
    public FramesItemViewModel(Frame frame, FramesViewModel framesViewModel)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(framesViewModel);

        Frame = frame;
        ExportCommand = framesViewModel.ExportFrameCommand;
        RemoveCommand = framesViewModel.RemoveFrameCommand;

        Text = $"{Frame.Width}x{Frame.Height}";
        Description = Frame is FrameWithMask ? $"{Frame.BitCount} bit" : "PNG";
    }

    public Frame Frame { get; }

    public string Text { get; }

    public string Description { get; }

    public RelayCommand ExportCommand { get; }

    public RelayCommand RemoveCommand { get; }

    public int CompareTo(FramesItemViewModel? other)
    {
        if (other == null)
        {
            return 1;
        }

        var result = Frame.Width.CompareTo(other.Frame.Width);
        return result == 0 ? Frame.BitCount.CompareTo(other.Frame.BitCount) : result;
    }
}
