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
using IcoApp.Core.Helpers;
using IcoApp.Core.Services;
using Windows.Storage;
using Windows.Storage.Pickers;

class FileService : IFileService
{
    private readonly IApp app;
    private readonly IImmutableList<string> SupportedExts = [".png", ".bmp"];

    private string UserDataPath => "./"; // todo: replace to user/local folder

    public Stream? ReadUserFile(string fileName)
    {
        var file = new FileInfo(Path.Combine(UserDataPath, fileName));

        return file.Exists ? file.OpenRead() : null;
    }

    public Stream WriteUserFile(string fileName, bool overwrite)
    {
        var file = new FileInfo(Path.Combine(UserDataPath, fileName));
        if (overwrite && file.Exists)
        {
            file.Delete();
        }

        return file.OpenWrite();
    }

    public FileService(IApp app)
    {
        ArgumentNullException.ThrowIfNull(app);

        this.app = app;
    }

    public bool IsSupported(string? fileName)
    {
        var ext = Path.GetExtension(fileName);

        return string.IsNullOrEmpty(ext) is false
            && SupportedExts.Any(x => x.Equals(ext, StringComparison.InvariantCultureIgnoreCase));
    }

    public async Task<IList<string>> PickMultipleFilesAsync()
    {
        var openPicker = new FileOpenPicker();
        WinRT.Interop.InitializeWithWindow.Initialize(openPicker, app.Handle);

        openPicker.ViewMode = PickerViewMode.Thumbnail;
        openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;

        SupportedExts.ForEach(x => openPicker.FileTypeFilter.Add(x));

        var files = await openPicker.PickMultipleFilesAsync();
        return files?.Select(x => x.Path).ToArray() ?? [];
    }
}
