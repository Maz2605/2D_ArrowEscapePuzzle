using System.Collections.Generic;
using UnityEngine;

namespace GameCore.Utils.DesignPattern.Command
{
    public class CommandInvoker
    {
        private LinkedList<ICommand> _undoHistory = new LinkedList<ICommand>();
        private Stack<ICommand> _redoStack = new Stack<ICommand>();

        private int _maxHistorySize;

        public CommandInvoker(int maxHistorySize = 3)
        {
            _maxHistorySize = maxHistorySize;
        }
        public void ExecuteCommand(ICommand command)
        {
            if (!command.Execute()) return;
            _undoHistory.AddLast(command);
            _redoStack.Clear();

            if (_undoHistory.Count > _maxHistorySize)
            {
                _undoHistory.RemoveFirst();
            }
        }

        public void Undo()
        {
            if (_undoHistory.Count > 0)
            {
                ICommand command = _undoHistory.Last.Value;
                _undoHistory.RemoveLast();
                command.Undo();
                
                _redoStack.Push(command);
            }
            else
            {
                Debug.Log("Hết cái để undo");
            }
        }

        public void Redo()
        {
            if (_redoStack.Count > 0)
            {
                ICommand activeCommand = _redoStack.Pop();
                
                if(!activeCommand.Execute()) return;
                
                _undoHistory.AddLast(activeCommand);

                if (_undoHistory.Count > _maxHistorySize)
                {
                    _undoHistory.RemoveFirst();
                }
            }
        }

        public void ClearHistory()
        {
            _undoHistory.Clear();
            _redoStack.Clear();
        }
        
        public bool CanUndo() => _undoHistory.Count > 0;
        public bool CanRedo() => _redoStack.Count > 0;
    }
}