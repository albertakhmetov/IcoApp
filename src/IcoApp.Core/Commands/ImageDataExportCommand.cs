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
namespace IcoApp.Core.Commands;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IcoApp.Core.Models;
using IcoApp.Core.Services;

public class ImageDataExportCommand : IAppCommand<ImageDataExportCommand.Parameters>
{
    public Task ExecuteAsync(Parameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(parameters.Image);
        ArgumentException.ThrowIfNullOrEmpty(parameters.FileName);

        using var dataStream = parameters.Image.GetStream();
        using var outputStream = File.Create(parameters.FileName);

        dataStream.CopyTo(outputStream);

        return Task.CompletedTask;
    }

    public sealed class Parameters
    {
        public required ImageData Image { get; init; }

        public required string FileName { get; init; }
    }
}
