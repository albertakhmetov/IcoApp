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
namespace IcoApp.Core.Models;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ItemCollection<T> : ItemCollectionBase<T> where T : class
{
    private readonly List<T> baseList = new List<T>();
    private IImmutableList<T>? list;

    public override IImmutableList<T> List
    {
        get
        {
            if (list == null)
            {
                list = baseList.ToImmutableArray();
            }

            return list;
        }
    }

    public override int Count => throw new NotImplementedException();

    public override bool Contains(T item)
    {
        return baseList.Contains(item);
    }

    protected override void Clear()
    {
        baseList.Clear();
    }

    protected override int IndexOf(T item)
    {
        return baseList.IndexOf(item);
    }

    protected override Task InsertAsync(T item, int? index = null)
    {
        if (index is null)
        {
            baseList.Add(item);
        }
        else
        {
            baseList.Insert(index.Value, item);
        }

        return Task.CompletedTask;
    }

    protected override void RemoveAt(int index)
    {
        baseList.RemoveAt(index);
    }

    protected override void OnCollectionChanged()
    {
        base.OnCollectionChanged();

        list = null;
    }
}
