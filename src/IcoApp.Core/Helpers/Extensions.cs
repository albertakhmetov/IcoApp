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
namespace IcoApp.Core.Helpers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;

public static class Extensions
{
    public static T DisposeWith<T>(this T obj, CompositeDisposable compositeDisposable) where T : IDisposable
    {
        compositeDisposable.Add(obj);

        return obj;
    }

    public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
    {
        foreach (var i in enumerable ?? [])
        {
            action?.Invoke(i);
        }
    }

    public static IList<T> ForEach<T>(this IEnumerable<T> enumerable, Func<T, bool> action)
    {
        var executedItems = new List<T>();

        foreach (var i in enumerable ?? [])
        {
            if (action?.Invoke(i) == true)
            {
                executedItems.Add(i);
            }
        }

        return executedItems;
    }

    public static int ToInt32(this long value)
    {
        return Convert.ToInt32(value);
    }

    public static int ToInt32(this double value)
    {
        return Convert.ToInt32(value);
    }
}
