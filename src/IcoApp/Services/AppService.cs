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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IcoApp.Core.Models;
using IcoApp.Core.Services;

internal class AppService : IAppService
{
    public IImmutableList<FileType> SupportedImageTypes { get; } = [FileType.Png, FileType.Bmp];

    public IImmutableList<FileType> SupportedFileTypes { get; } = [FileType.Ico];

    public nint Handle => (App.Current as App)?.Handle ?? nint.Zero;

    public string UserDataPath { get; } = "./"; // todo: replace to user/local folder

}
