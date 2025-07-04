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
namespace IcoApp.Helpers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using IcoApp.Core.Helpers;
using IcoApp.Core.Models;
using IcoApp.Core.ViewModels;
using IcoApp.FileFormat.Internal;

static class Converters
{
    public static bool Not(bool value) => !value;

    public static bool And(bool a, bool b) => a && b;

    public static bool Or(bool a, bool b) => a || b;

    public static ImageData? GetFrameImage(FramesItemViewModel? viewModel)
    {
        return viewModel?.Frame?.Image;
    }

    public static ImageSource? LoadImage(ImageData imageData, int width, int height)
    {
        return LoadImage(imageData, width, height, 48);
    }

    public static ImageSource? LoadImage(ImageData imageData, int width, int height, double maxSize)
    {
        if (imageData == null || imageData.Size == 0)
        {
            return null;
        }

        try
        {
            using var stream = imageData.GetStream().AsRandomAccessStream();

            var bitmapImage = new BitmapImage();

            var factor = maxSize / Math.Max(width, height);

            if (factor < 1)
            {
                bitmapImage.DecodePixelWidth = Convert.ToInt32(factor * width);
                bitmapImage.DecodePixelHeight = Convert.ToInt32(factor * height);
                bitmapImage.DecodePixelType = DecodePixelType.Logical;
            }

            bitmapImage.SetSource(stream);

            return bitmapImage;
        }
        catch
        {
            return null;
        }
    }

    public static Visibility VisibleIf(object value)
    {
        return value != null ? Visibility.Visible : Visibility.Collapsed;
    }

    public static Visibility VisibleIf(bool value)
    {
        return value == true ? Visibility.Visible : Visibility.Collapsed;
    }

    public static Visibility VisibleIf(int value)
    {
        return value > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public static Visibility VisibleIf(double value)
    {
        return VisibleIf(value.ToInt32());
    }

    public static Visibility VisibleIfNot(object value)
    {
        return value == null ? Visibility.Visible : Visibility.Collapsed;
    }

    public static Visibility VisibleIfNot(bool value)
    {
        return value == false ? Visibility.Visible : Visibility.Collapsed;
    }

    public static Visibility VisibleIfNot(int value)
    {
        return value == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public static Visibility VisibleIfNot(double value)
    {
        return VisibleIf(value.ToInt32());
    }
}
