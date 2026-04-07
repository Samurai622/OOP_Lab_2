using System.Collections.Generic;
using Calculator.Commands;

namespace Calculator.Services;

public class HistoryManager
{
    private readonly Stack<IUndoableCommand> _undo = new();
    private readonly Stack<IUndoableCommand> _redo = new();

    public void ExecuteCommand(IUndoableCommand cmd) { cmd.Execute(); _undo.Push(cmd); _redo.Clear(); }
    public void Undo() { if (_undo.Count > 0) { var cmd = _undo.Pop(); cmd.Undo(); _redo.Push(cmd); } }
    public void Redo() { if (_redo.Count > 0) { var cmd = _redo.Pop(); cmd.Execute(); _undo.Push(cmd); } }
}