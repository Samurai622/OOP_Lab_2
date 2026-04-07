using System;
using Calculator.Models;

namespace Calculator.Commands;

public interface IUndoableCommand { void Execute(); void Undo(); }

public class StateCommand : IUndoableCommand
{
    private readonly Action _action;
    private readonly Func<CalculatorState> _getState;
    private readonly Action<CalculatorState> _setState;
    private CalculatorState? _stateBefore, _stateAfter;

    public StateCommand(Action action, Func<CalculatorState> getState, Action<CalculatorState> setState)
    {
        _action = action; _getState = getState; _setState = setState;
    }

    public void Execute()
    {
        if (_stateAfter == null) { _stateBefore = _getState(); _action(); _stateAfter = _getState(); }
        else _setState(_stateAfter);
    }
    public void Undo() { if (_stateBefore != null) _setState(_stateBefore); }
}