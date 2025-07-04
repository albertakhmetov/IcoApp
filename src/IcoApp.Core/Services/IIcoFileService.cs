﻿/*  Copyright © 2025, Albert Akhmetov <akhmetov@live.com>   
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
namespace IcoApp.Core.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IcoApp.Core.Commands;
using IcoApp.Core.Models;

public interface IIcoFileService
{
    IObservable<bool> Modified { get; }

    IObservable<string?> FileName { get; }

    IObservable<ItemCollectionAction<Frame>> Frames { get; }

    IObservable<bool> CanUndo { get; }

    IObservable<bool> CanRedo { get; }

    void Undo();

    void Redo();

    IAppCommand<T> CreateCommand<T>();

    Task CreateNew();

    Task Load(string fileName);

    Task Save();

    Task SaveAs(string fileName);
}
