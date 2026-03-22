using System;
using ME2LevelEditor.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ME2LevelEditor.Tests.Services
{
    [TestClass]
    public class UndoRedoServiceTests
    {
        private UndoRedoService _service;

        [TestInitialize]
        public void Setup()
        {
            _service = new UndoRedoService();
        }

        private class TestCommand : IEditCommand
        {
            private readonly Action _execute;
            private readonly Action _undo;

            public string Description => "Test";

            public TestCommand(Action execute, Action undo)
            {
                _execute = execute;
                _undo = undo;
            }

            public void Execute() => _execute();
            public void Undo() => _undo();
        }

        [TestMethod]
        public void Execute_AddsToUndoStackAndRunsCommand()
        {
            int counter = 0;
            var cmd = new TestCommand(() => counter++, () => counter--);

            _service.Execute(cmd);

            Assert.AreEqual(1, counter);
            Assert.IsTrue(_service.CanUndo);
            Assert.IsFalse(_service.CanRedo);
        }

        [TestMethod]
        public void Undo_ReversesCommandAndEnablesRedo()
        {
            int counter = 0;
            var cmd = new TestCommand(() => counter++, () => counter--);
            _service.Execute(cmd);

            _service.Undo();

            Assert.AreEqual(0, counter);
            Assert.IsFalse(_service.CanUndo);
            Assert.IsTrue(_service.CanRedo);
        }

        [TestMethod]
        public void Redo_ReAppliesCommand()
        {
            int counter = 0;
            var cmd = new TestCommand(() => counter++, () => counter--);
            _service.Execute(cmd);
            _service.Undo();

            _service.Redo();

            Assert.AreEqual(1, counter);
            Assert.IsTrue(_service.CanUndo);
            Assert.IsFalse(_service.CanRedo);
        }

        [TestMethod]
        public void Execute_AfterUndo_ClearsRedoStack()
        {
            int counter = 0;
            var cmd1 = new TestCommand(() => counter++, () => counter--);
            var cmd2 = new TestCommand(() => counter += 10, () => counter -= 10);
            _service.Execute(cmd1);
            _service.Undo();

            _service.Execute(cmd2);

            Assert.IsFalse(_service.CanRedo);
            Assert.AreEqual(10, counter);
        }

        [TestMethod]
        public void MarkSavePoint_TracksDirtyState()
        {
            int counter = 0;
            var cmd = new TestCommand(() => counter++, () => counter--);

            _service.MarkSavePoint();
            Assert.IsFalse(_service.IsDirty);

            _service.Execute(cmd);
            Assert.IsTrue(_service.IsDirty);

            _service.Undo();
            Assert.IsFalse(_service.IsDirty);
        }

        [TestMethod]
        public void Clear_ResetsAllState()
        {
            int counter = 0;
            var cmd = new TestCommand(() => counter++, () => counter--);
            _service.Execute(cmd);
            _service.MarkSavePoint();

            _service.Clear();

            Assert.IsFalse(_service.CanUndo);
            Assert.IsFalse(_service.CanRedo);
            Assert.IsFalse(_service.IsDirty);
        }
    }
}
