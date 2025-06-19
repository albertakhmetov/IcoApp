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
namespace IcoApp.FileFormat.Internal;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

internal static class Extensions
{
    public static void IsTrue(this bool result, string? errorMessage = null)
    {
        if (!result)
        {
            throw new InvalidOperationException(errorMessage);
        }
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
