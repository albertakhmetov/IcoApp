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

public class ConfirmationDialogViewModel : DialogViewModel
{
    private string? iconGlyph, text;

    public string? IconGlyph
    {
        get => iconGlyph;
        set => Set(ref iconGlyph, value);
    }

    public string? Text
    {
        get => text;
        set => Set(ref text, value);
    }

    public string ConfirmationText
    {
        get => PrimaryText;
        set
        {
            PrimaryText = value;
            Invalidate(nameof(ConfirmationText));
        }
    }

    protected override void OnPrimaryExecuted(object? parameter)
    {
        ;
    }
}
