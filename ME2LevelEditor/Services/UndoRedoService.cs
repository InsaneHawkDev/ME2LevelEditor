using System.Collections.Generic;

namespace ME2LevelEditor.Services
{
    public class UndoRedoService
    {
        private readonly Stack<IEditCommand> _undoStack = new Stack<IEditCommand>();
        private readonly Stack<IEditCommand> _redoStack = new Stack<IEditCommand>();
        private int _savePointDepth;

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;
        public bool IsDirty => _undoStack.Count != _savePointDepth;

        public void Execute(IEditCommand command)
        {
            command.Execute();
            _undoStack.Push(command);
            _redoStack.Clear();
        }

        public void Undo()
        {
            if (!CanUndo) return;
            var command = _undoStack.Pop();
            command.Undo();
            _redoStack.Push(command);
        }

        public void Redo()
        {
            if (!CanRedo) return;
            var command = _redoStack.Pop();
            command.Execute();
            _undoStack.Push(command);
        }

        public void MarkSavePoint()
        {
            _savePointDepth = _undoStack.Count;
        }

        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            _savePointDepth = 0;
        }
    }
}
