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
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using IcoApp.Core.Models;
using IcoApp.Core.Services;

internal class IcoService : IIcoService
{
    private readonly BehaviorSubject<bool> modifiedSubject;
    private readonly BehaviorSubject<string?> fileNameSubject;

    public IcoService()
    {
        modifiedSubject = new BehaviorSubject<bool>(false);
        fileNameSubject = new BehaviorSubject<string?>(null);

        Frames = new ItemCollection<IcoFrame>();
        Frames.CollectionChanged += Frames_CollectionChanged;

        Modified = modifiedSubject.Throttle(TimeSpan.FromMilliseconds(200)).AsObservable();
        FileName = fileNameSubject.Throttle(TimeSpan.FromMilliseconds(200)).AsObservable();
    }

    public IObservable<bool> Modified { get; }

    public IObservable<string?> FileName { get; }

    public ItemCollectionBase<IcoFrame> Frames { get; }

    public void CreateNew()
    {
        foreach (var i in Frames.List)
        {
            i.Dispose();
        }

        Frames.RemoveAll();

        fileNameSubject.OnNext(null);
        modifiedSubject.OnNext(false);
    }

    public void Load(string fileName)
    {
        throw new NotImplementedException();
    }

    public void Save()
    {
        throw new NotImplementedException();
    }

    public void SaveAs(string fileName)
    {
        throw new NotImplementedException();
    }

    private void Frames_CollectionChanged(object? sender, EventArgs e)
    {
        modifiedSubject.OnNext(true);
    }
}
