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
namespace IcoApp.Core.Models;

using System.Collections.ObjectModel;
using System.Collections.Specialized;

public class ItemObservableCollection<T> : ObservableCollection<T> where T : IComparable<T>
{
    public void Set(IEnumerable<T>? items)
    {
        Items.Clear();
        foreach (var i in (items ?? []).Where(x => x != null).OrderBy(x => x))
        {
            Items.Add(i);
        }

        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    protected override void InsertItem(int index, T item)
    {
        // ignore null values
        if (item == null)
        {
            return;
        }

        var i = 0;
        while (Items.Count > i && item.CompareTo(Items[i]) > 0)
        {
            i++;
        }

        base.InsertItem(i, item);
    }

    //public void Insert(IEnumerable<T>? items, int? startingIndex = 0)
    //{
    //    if (items == null || !items.Any())
    //    {
    //        return;
    //    }

    //    var args = new NotifyCollectionChangedEventArgs(
    //        NotifyCollectionChangedAction.Add,
    //        items!.ToArray(),
    //        startingIndex == null ? Count : startingIndex.Value);

    //    if (startingIndex == null)
    //    {
    //        foreach (var i in items)
    //        {
    //            Items.Add(i);
    //        }
    //    }
    //    else
    //    {
    //        var index = startingIndex.Value;
    //        foreach (var i in items)
    //        {
    //            Items.Insert(index++, i);
    //        }
    //    }

    //    OnCollectionChanged(args);
    //}
}
