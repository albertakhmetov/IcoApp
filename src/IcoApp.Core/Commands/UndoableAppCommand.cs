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
namespace IcoApp.Core.Commands;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public abstract class UndoableAppCommand<T> : IAppCommand<T>, IUndoable
{
    public bool CanUndo { get; private set; } = false;

    public bool CanRedo { get; private set; } = false;

    public bool IsExecuted { get; private set; } = false;

    async Task IAppCommand<T>.ExecuteAsync(T parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        IsExecuted = await ExecuteAsync(parameters);
        CanUndo = IsExecuted;
    }

    void IUndoable.Redo()
    {
        if (IsExecuted is false)
        {
            throw new InvalidOperationException("The command is not executed.");
        }

        Undo();

        CanUndo = false;
        CanRedo = true;
    }

    void IUndoable.Undo()
    {
        if (IsExecuted is false)
        {
            throw new InvalidOperationException("The command is not executed.");
        }

        Undo();

        CanUndo = true;
        CanRedo = false;
    }

    protected abstract Task<bool> ExecuteAsync(T parameters);

    protected abstract void Undo();

    protected abstract void Redo();
}
