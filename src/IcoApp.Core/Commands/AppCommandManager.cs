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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

public class AppCommandManager : IAppCommandManager
{
    private readonly IServiceProvider serviceProvider;
    private readonly History undoHistory = [], redoHistory = [];

    public AppCommandManager(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        this.serviceProvider = serviceProvider;
    }

    public async Task ExecuteAsync<T>(T parameters)
    {
        var command = serviceProvider.GetRequiredService<IAppCommand<T>>();

        await command.ExecuteAsync(parameters);

        if (command is IUndoable undoableCommand)
        {
            undoHistory.Push(undoableCommand);
        }

        redoHistory.Clear();
    }

    public void Undo()
    {
        if (undoHistory.TryPop(out var undoableCommand))
        {
            undoableCommand.Undo();

            redoHistory.Push(undoableCommand);
        }
    }

    public void Redo()
    {
        if (redoHistory.TryPop(out var undoableCommand))
        {
            undoableCommand.Redo();

            undoHistory.Push(undoableCommand);
        }
    }

    private sealed class History : IEnumerable<IUndoable>
    {
        private readonly LinkedList<IUndoable> items = [];

        public int? MaxSize { get; init; } = 10;

        public int Count => items.Count;

        public IUndoable Pop()
        {
            if (TryPop(out var undoable))
            {
                return undoable!;
            }
            else
            {
                throw new InvalidOperationException("The history is empty");
            }
        }

        public bool TryPop([MaybeNullWhen(false)] out IUndoable undoable)
        {
            if (items.Count > 0)
            {
                undoable = items.Last();
                items.RemoveLast();
                return true;
            }
            else
            {
                undoable = null;
                return false;
            }
        }

        public IUndoable Peek()
        {
            if (items.Count == 0)
            {
                throw new InvalidOperationException("The history is empty");
            }

            return items.Last();
        }

        public void Push(IUndoable undoable)
        {
            ArgumentNullException.ThrowIfNull(undoable);

            if (MaxSize is not null)
            {
                while (Count >= MaxSize)
                {
                    if (items.First?.Value is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }

                    items.RemoveFirst();
                }
            }

            items.AddLast(undoable);
        }

        public void Clear()
        {
            foreach (var i in items)
            {
                if (i is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            items.Clear();
        }

        public IEnumerator<IUndoable> GetEnumerator()
        {
            return items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
