namespace IcoApp.Core.Commands;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class AddFrameCommand : UndoableAppCommand<AddFrameCommand.Parameters>
{
    protected override Task ExecuteAsync(Parameters parameters)
    {
        throw new NotImplementedException();
    }

    protected override void Redo()
    {
        throw new NotImplementedException();
    }

    protected override void Undo()
    {
        throw new NotImplementedException();
    }

    public sealed record Parameters
    { }
}
