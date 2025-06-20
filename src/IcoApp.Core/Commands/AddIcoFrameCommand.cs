namespace IcoApp.Core.Commands;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IcoApp.Core.Models;
using IcoApp.Core.Services;

public class AddIcoFrameCommand : UndoableAppCommand<AddIcoFrameCommand.Parameters>
{
    private readonly IIcoService icoService;

    private IImmutableList<IcoFrame>? frames;

    public AddIcoFrameCommand(IIcoService icoService)
    {
        ArgumentNullException.ThrowIfNull(icoService);

        this.icoService = icoService;
    }

    protected override async Task<bool> ExecuteAsync(Parameters parameters)
    {
        if (parameters.FileNames.Count == 0)
        {
            return false;
        }

        var frames = new List<IcoFrame>();
        foreach (var fileName in parameters.FileNames)
        {
            var dataStream = LoadImageData(fileName);

            frames.Add(CreateIcoFrame(dataStream));
        }

        await icoService.Frames.AddAsync(frames);

        this.frames = frames.ToImmutableArray();

        return true;
    }

    private static MemoryStream LoadImageData(string fileName)
    {
        using var fileStream = File.OpenRead(fileName);

        var dataStream = new MemoryStream();
        fileStream.CopyTo(dataStream);
        dataStream.Position = 0;

        return dataStream;
    }

    private static IcoFrame CreateIcoFrame(MemoryStream dataStream)
    {
        using var image = Image.FromStream(dataStream);

        if (image.RawFormat.Equals(ImageFormat.Png))
        {
            dataStream.Position = 0;
            return new IcoFrame(image.Width, image.Height, dataStream);
        }
        else
        {
            using var imageStream = new MemoryStream();
            image.Save(imageStream, ImageFormat.Png);
            imageStream.Flush();
            imageStream.Position = 0;
            return new IcoFrame(image.Width, image.Height, imageStream);
        }
    }

    protected override void Redo()
    {
        throw new NotImplementedException();
    }

    protected override void Undo()
    {
        throw new NotImplementedException();
    }

    public sealed class Parameters
    {
        public required IImmutableList<string> FileNames { get; init; }
    }
}
