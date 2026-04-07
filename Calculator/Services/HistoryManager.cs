using System.Collections.Generic;
using Calculator;

namespace Calculator.Services;

public class HistoryManager
{
    private readonly Stack<IUndoableCommand> _undoStack = new();
    private readonly Stack<IUndoableCommand> _redoStack = new();

    public void ExecuteCommand(IUndoableCommand command)
    {
        command.Execute();
        _undoStack.Push(command);
        _redoStack.Clear(); // Нова дія скидає історію Redo
    }

    public void Undo()
    {
        if (_undoStack.Count > 0)
        {
            var cmd = _undoStack.Pop();
            cmd.Undo();
            _redoStack.Push(cmd);
        }
    }

    public void Redo()
    {
        if (_redoStack.Count > 0)
        {
            var cmd = _redoStack.Pop();
            cmd.Execute();
            _undoStack.Push(cmd);
        }
    }
}