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
namespace IcoApp.Controls;

using System.Collections.Immutable;
using System.Numerics;
using System.Reactive.Linq;
using System.Windows.Input;
using IcoApp.Core.Models;
using IcoApp.FileFormat.Internal;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.Storage.Streams;

public sealed partial class ImageViewer : UserControl
{
    public static DependencyProperty ImageDataProperty = DependencyProperty.Register(
        nameof(ImageData),
        typeof(ImageData),
        typeof(ImageViewer),
        new PropertyMetadata(null, OnImageDataPropertyChanged));

    public static DependencyProperty IsActualSizeProperty = DependencyProperty.Register(
        nameof(IsActualSize),
        typeof(double),
        typeof(ImageViewer),
        new PropertyMetadata(false, OnIsActualSizePropertyChanged));

    private static async void OnImageDataPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ImageViewer viewer)
        {
            await viewer.CancelResourcesLoadingTask();

            viewer.resourcesLoadingTask = viewer.LoadImageResourcesAsync(viewer.ImageCanvas);
            await viewer.resourcesLoadingTask;

            viewer.ImageCanvas.Invalidate();
        }
    }

    private static void OnIsActualSizePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ImageViewer viewer)
        {
            viewer.ImageCanvas.Invalidate();
        }
    }

    private const int ROUND_SIZE = 4, MINI_SIZE = 64, LINE_WIDTH = 2;

    private Task? resourcesLoadingTask;

    private CanvasBitmap? image;
    private CanvasImageBrush? imageBrush;

    private double imageOffsetX, imageOffsetY;
    private double scaleFactor = 1;

    public ImageViewer()
    {
        this.InitializeComponent();

        var down = Observable
            .FromEventPattern<PointerRoutedEventArgs>(this, nameof(Control.PointerPressed))
            .Where(i => IsLeftButton(i.EventArgs));

        var up = Observable
            .FromEventPattern<PointerRoutedEventArgs>(this, nameof(Control.PointerReleased))
            .Where(i => IsLeftButton(i.EventArgs));

        var move = Observable
            .FromEventPattern<PointerRoutedEventArgs>(this, nameof(Control.PointerMoved))
            .Where(i => IsLeftButton(i.EventArgs))
            .TakeUntil(up);

        down.Select(i => GetStartPoint(i.EventArgs))
            .SelectMany(start => move.Select(i => CalculateDelta(start, i.EventArgs)))
            .Subscribe(i => SetImageOffset(i.X, i.Y));
    }

    private bool IsLeftButton(PointerRoutedEventArgs e)
    {
        return e.GetCurrentPoint(this).Properties.IsLeftButtonPressed;
    }

    private Point GetStartPoint(PointerRoutedEventArgs eventArgs)
    {
        var start = eventArgs.GetCurrentPoint(this).Position;

        return new Point(start.X - imageOffsetX, start.Y - imageOffsetY);
    }

    private Point CalculateDelta(Point start, PointerRoutedEventArgs eventArgs)
    {
        var current = eventArgs.GetCurrentPoint(this).Position;

        return new Point(current.X - start.X, current.Y - start.Y);
    }

    public ImageData? ImageData
    {
        get => (ImageData?)GetValue(ImageDataProperty);
        set => SetValue(ImageDataProperty, value);
    }

    public bool IsActualSize
    {
        get => (bool)GetValue(IsActualSizeProperty);
        set => SetValue(IsActualSizeProperty, value);
    }

    public double ScaleFactor
    {
        get => scaleFactor;
        set
        {
            scaleFactor = Math.Max(MinScaleFactor, Math.Min(MaxScaleFactor, value));

            CheckImageOffset();
            ImageCanvas.Invalidate();
        }
    }

    public Size ImageSize { get; private set; }

    public double ScaledImageWidth => ImageSize.Width * ScaleFactor;

    public double ScaledImageHeight => ImageSize.Height * ScaleFactor;

    public double MinScaleFactor { get; private set; }

    public double MaxScaleFactor { get; private set; }

    protected override void OnPointerWheelChanged(PointerRoutedEventArgs e)
    {
        var delta = e.GetCurrentPoint(this).Properties.MouseWheelDelta / 120d;

        ScaleFactor += Math.Sign(delta) * (MaxScaleFactor - MinScaleFactor) / 50d;
    }

    protected override void OnManipulationDelta(ManipulationDeltaRoutedEventArgs e)
    {
        if (Math.Abs(1 - e.Delta.Scale) < 0.0001)
        {
            return;
        }

        var delta = e.Delta.Scale > 1 ? 1 : -1;

        ScaleFactor += delta * (MaxScaleFactor - MinScaleFactor) / 100d;
    }

    private void ResetImageOffset()
    {
        imageOffsetX = 0;
        imageOffsetY = 0;
    }

    private void SetImageOffset(double x, double y)
    {
        imageOffsetX = x;
        imageOffsetY = y;

        CheckImageOffset();
        ImageCanvas.Invalidate();
    }

    private void CheckImageOffset()
    {
        if (ScaledImageWidth <= ImageCanvas.ActualWidth)
        {
            imageOffsetX = 0;
        }
        else
        {
            imageOffsetX = Math.Min(
                (ScaledImageWidth - ImageCanvas.ActualWidth) / 2,
                Math.Max(imageOffsetX, (ImageCanvas.ActualWidth - ScaledImageWidth) / 2));
        }

        if (ScaledImageHeight <= ImageCanvas.ActualHeight)
        {
            imageOffsetY = 0;
        }
        else
        {
            imageOffsetY = Math.Min(
                (ScaledImageHeight - ImageCanvas.ActualHeight) / 2,
                Math.Max(imageOffsetY, (ImageCanvas.ActualHeight - ScaledImageHeight) / 2));
        }
    }

    private async Task CreateResourcesAsync(CanvasControl sender)
    {
        await CancelResourcesLoadingTask();
        resourcesLoadingTask = LoadImageResourcesAsync(sender);

        await resourcesLoadingTask;
    }

    private async Task CancelResourcesLoadingTask()
    {
        if (resourcesLoadingTask != null)
        {
            resourcesLoadingTask.AsAsyncAction().Cancel();
            try
            {
                await resourcesLoadingTask;
            }
            catch
            { }

            resourcesLoadingTask = null;
        }
    }

    private async Task LoadImageResourcesAsync(CanvasControl sender)
    {
        imageBrush?.Dispose();
        image?.Dispose();

        if (ImageData is null)
        {
            image = null;
            imageBrush = null;
        }
        else
        {
            using var stream = ImageData.GetStream();

            image = await CanvasBitmap.LoadAsync(ImageCanvas, stream.AsRandomAccessStream());
            scaleFactor = 1;
            ResetImageOffset();

            ImageSize = new Size(image == null ? 0 : image.Size.Width, image == null ? 0 : image.Size.Height);

            var maxSide = Math.Max(ImageSize.Width, ImageSize.Height);

            MinScaleFactor = 16 / maxSide;
            MaxScaleFactor = 512 / maxSide;

            imageBrush = new CanvasImageBrush(sender.Device, image)
            {
                Interpolation = CanvasImageInterpolation.NearestNeighbor,
                ExtendX = CanvasEdgeBehavior.Mirror,
                ExtendY = CanvasEdgeBehavior.Mirror,
                SourceRectangle = new Rect(new Point(0, 0), ImageSize)
            };
        }
    }

    private void ImageCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        CheckImageOffset();
    }

    private void ImageCanvas_CreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
    {
        args.TrackAsyncAction(CreateResourcesAsync(sender).AsAsyncAction());
    }

    private void ImageCanvas_Draw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        // Loading isn't completed
        if (resourcesLoadingTask != null && !resourcesLoadingTask.IsCompletedSuccessfully)
        {
            return;
        }

        // Image isn't loaded
        if (imageBrush == null)
        {
            return;
        }

        var width = ScaledImageWidth;
        var height = ScaledImageHeight;

        var left = imageOffsetX + (sender.ActualWidth - width) / 2;
        var top = imageOffsetY + (sender.ActualHeight - height) / 2;

        imageBrush.Transform =
            Matrix3x2.CreateTranslation(Convert.ToSingle(left / ScaleFactor), Convert.ToSingle(top / ScaleFactor)) *
            Matrix3x2.CreateScale(Convert.ToSingle(ScaleFactor));

        var pixelSize = Convert.ToSingle(scaleFactor);
        if (pixelSize < 5)
        {
            args.DrawingSession.FillRectangle(
                rect: new Rect(
                    x: left,
                    y: top,
                    width: width,
                    height: height),
                brush: imageBrush);
        }
        else
        {
            for (var i = 0; i < ImageSize.Width; i++)
            {
                for (var j = 0; j < ImageSize.Height; j++)
                {
                    args.DrawingSession.FillRectangle(
                        rect: new Rect(
                            x: left + i * pixelSize,
                            y: top + j * pixelSize,
                            width: pixelSize - LINE_WIDTH,
                            height: pixelSize - LINE_WIDTH),
                        brush: imageBrush);
                }
            }
        }

        // render mini image
        if (ScaledImageWidth > sender.ActualWidth || ScaledImageHeight > sender.ActualHeight)
        {
            var factorX = MINI_SIZE / ImageSize.Width;
            var factorY = MINI_SIZE / ImageSize.Height;

            var miniWidth = factorX * ImageSize.Width;
            var miniHeight = factorY * ImageSize.Height;

            DrawBackground(sender, args);

            args.DrawingSession.DrawImage(
                image: image,
                destinationRectangle: new Rect(
                    x: ROUND_SIZE + 2,
                    y: sender.ActualHeight - (MINI_SIZE + miniHeight) / 2 - ROUND_SIZE - 2,
                    width: miniWidth,
                    height: miniHeight),
                sourceRectangle: new Rect(
                    x: 0,
                    y: 0,
                    width: ImageSize.Width,
                    height: ImageSize.Height),
                opacity: 1,
                interpolation: CanvasImageInterpolation.NearestNeighbor);

            DrawSelection(sender, args, left, top);
        }
    }

    private static void DrawBackground(CanvasControl sender, CanvasDrawEventArgs args)
    {
        if (Application.Current.Resources["ControlSolidFillColorDefaultBrush"] is SolidColorBrush ctrlBrush &&
            Application.Current.Resources["SurfaceStrokeColorDefaultBrush"] is SolidColorBrush ctrlBorderBrush)
        {
            var rect = new Rect(
                x: 2,
                y: sender.ActualHeight - MINI_SIZE - ROUND_SIZE * 2 - 2,
                width: MINI_SIZE + ROUND_SIZE * 2,
                height: MINI_SIZE + ROUND_SIZE * 2);

            args.DrawingSession.FillRoundedRectangle(
                rect: rect,
                radiusX: ROUND_SIZE,
                radiusY: ROUND_SIZE,
                color: ctrlBrush.Color);

            args.DrawingSession.DrawRoundedRectangle(
                rect: rect,
                radiusX: ROUND_SIZE,
                radiusY: ROUND_SIZE,
                color: ctrlBorderBrush.Color);
        }
    }

    private void DrawSelection(CanvasControl sender, CanvasDrawEventArgs args, double x, double y)
    {
        if (Application.Current.Resources["ControlStrongFillColorDefaultBrush"] is SolidColorBrush ctrlFillBrush)
        {
            var imageFactorX = MINI_SIZE / ScaledImageWidth;
            var imageFactorY = MINI_SIZE / ScaledImageHeight;

            var sX = ScaledImageWidth > sender.ActualWidth
                ? Math.Abs(x * imageFactorX)
                : 0;

            var sY = ScaledImageHeight > sender.ActualHeight
                ? Math.Abs(y * imageFactorY)
                : 0;

            var sWidth = ScaledImageWidth > sender.ActualWidth
                ? imageFactorX * sender.ActualWidth
                : MINI_SIZE;

            var sHeight = ScaledImageHeight > sender.ActualHeight
                ? imageFactorY * sender.ActualHeight
                : MINI_SIZE;

            args.DrawingSession.FillRectangle(
                x: Convert.ToSingle(2 + ROUND_SIZE + sX),
                y: Convert.ToSingle(sender.ActualHeight - MINI_SIZE - ROUND_SIZE - 2 + sY),
                w: Convert.ToSingle(sWidth),
                h: Convert.ToSingle(sHeight),
                color: ctrlFillBrush.Color);
        }
    }
}
