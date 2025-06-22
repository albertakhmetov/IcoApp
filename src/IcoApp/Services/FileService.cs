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
using IcoApp.Core.Models;
using IcoApp.Core.Services;
using Microsoft.UI.Xaml;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

class FileService : IFileService
{
    private readonly IAppService appService;

    public FileService(IAppService appService)
    {
        ArgumentNullException.ThrowIfNull(appService);

        this.appService = appService;
    }

    public Stream? ReadUserFile(string fileName)
    {
        var file = new FileInfo(Path.Combine(appService.UserDataPath, fileName));

        return file.Exists ? file.OpenRead() : null;
    }

    public Stream WriteUserFile(string fileName, bool overwrite)
    {
        var file = new FileInfo(Path.Combine(appService.UserDataPath, fileName));
        if (overwrite && file.Exists)
        {
            file.Delete();
        }

        return file.OpenWrite();
    }

    public async Task<IList<string>> PickFilesForOpenAsync(IImmutableList<FileType> fileTypes)
    {
        var openPicker = new FileOpenPicker();
        InitializeWithWindow.Initialize(openPicker, appService.Handle);

        openPicker.ViewMode = PickerViewMode.Thumbnail;
        openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;

        fileTypes.ForEach(x => openPicker.FileTypeFilter.Add(x.Extension));

        var files = await openPicker.PickMultipleFilesAsync();
        return files?.Select(x => x.Path).ToArray() ?? [];
    }

    public async Task<string?> PickFileForOpenAsync(IImmutableList<FileType> fileTypes)
    {
        var openPicker = new FileOpenPicker();
        InitializeWithWindow.Initialize(openPicker, appService.Handle);

        openPicker.ViewMode = PickerViewMode.Thumbnail;
        openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;

        fileTypes.ForEach(x => openPicker.FileTypeFilter.Add(x.Extension));

        var file = await openPicker.PickSingleFileAsync();
        return file?.Path;
    }

    public async Task<string?> PickFileForSaveAsync(IImmutableList<FileType> fileTypes, string? suggestedFileName = null)
    {
        var savePicker = new FileSavePicker();
        InitializeWithWindow.Initialize(savePicker, appService.Handle);

        savePicker.SuggestedFileName = suggestedFileName ?? string.Empty;
        savePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;

        foreach (var category in fileTypes.GroupBy(x => x.Description))
        {
            savePicker.FileTypeChoices.Add(category.Key, category.Select(x => x.Extension).ToArray());
        }

        var file = await savePicker.PickSaveFileAsync();
        return file?.Path;
    }
}
