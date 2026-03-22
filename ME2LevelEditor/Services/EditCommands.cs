using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ME2LevelEditor.Models;
using ME2LevelEditor.ViewModels;

namespace ME2LevelEditor.Services
{
    public class AddObstacleCommand : IEditCommand
    {
        private readonly ObstacleViewModel _obstacle;
        private readonly ObservableCollection<ObstacleViewModel> _collection;

        public string Description => "Add Obstacle";

        public AddObstacleCommand(ObstacleViewModel obstacle, ObservableCollection<ObstacleViewModel> collection)
        {
            _obstacle = obstacle;
            _collection = collection;
        }

        public void Execute() => _collection.Add(_obstacle);
        public void Undo() => _collection.Remove(_obstacle);
    }

    public class AddSectionCommand : IEditCommand
    {
        private readonly SectionViewModel _section;
        private readonly ObservableCollection<SectionViewModel> _collection;

        public string Description => "Add Section";

        public AddSectionCommand(SectionViewModel section, ObservableCollection<SectionViewModel> collection)
        {
            _section = section;
            _collection = collection;
        }

        public void Execute() => _collection.Add(_section);
        public void Undo() => _collection.Remove(_section);
    }

    public class MoveObstacleCommand : IEditCommand
    {
        private readonly ObstacleViewModel _obstacle;
        private readonly int _oldSampleId;
        private readonly int _newSampleId;

        public string Description => "Move Obstacle";

        public MoveObstacleCommand(ObstacleViewModel obstacle, int oldSampleId, int newSampleId)
        {
            _obstacle = obstacle;
            _oldSampleId = oldSampleId;
            _newSampleId = newSampleId;
        }

        public void Execute() => _obstacle.SampleId = _newSampleId;
        public void Undo() => _obstacle.SampleId = _oldSampleId;
    }

    public class DeleteObstacleCommand : IEditCommand
    {
        private readonly ObstacleViewModel _obstacle;
        private readonly ObservableCollection<ObstacleViewModel> _collection;
        private int _removedIndex;

        public string Description => "Delete Obstacle";

        public DeleteObstacleCommand(ObstacleViewModel obstacle, ObservableCollection<ObstacleViewModel> collection)
        {
            _obstacle = obstacle;
            _collection = collection;
        }

        public void Execute()
        {
            _removedIndex = _collection.IndexOf(_obstacle);
            _collection.Remove(_obstacle);
        }

        public void Undo()
        {
            if (_removedIndex >= 0 && _removedIndex <= _collection.Count)
                _collection.Insert(_removedIndex, _obstacle);
            else
                _collection.Add(_obstacle);
        }
    }

    public class ModifyObstacleTypeCommand : IEditCommand
    {
        private readonly ObstacleViewModel _obstacle;
        private readonly ObstacleType _oldType;
        private readonly int? _oldHeldDuration;
        private readonly ObstacleType _newType;
        private readonly int? _newHeldDuration;

        public string Description => "Modify Obstacle Type";

        public ModifyObstacleTypeCommand(ObstacleViewModel obstacle, ObstacleType newType, int? newHeldDuration)
        {
            _obstacle = obstacle;
            _oldType = obstacle.Type;
            _oldHeldDuration = obstacle.HeldDuration;
            _newType = newType;
            _newHeldDuration = newHeldDuration;
        }

        public void Execute()
        {
            _obstacle.Type = _newType;
            _obstacle.HeldDuration = _newHeldDuration;
        }

        public void Undo()
        {
            _obstacle.Type = _oldType;
            _obstacle.HeldDuration = _oldHeldDuration;
        }
    }

    public class ModifyHeldDurationCommand : IEditCommand
    {
        private readonly ObstacleViewModel _obstacle;
        private readonly int? _oldDuration;
        private readonly int? _newDuration;

        public string Description => "Modify Held Duration";

        public ModifyHeldDurationCommand(ObstacleViewModel obstacle, int? oldDuration, int? newDuration)
        {
            _obstacle = obstacle;
            _oldDuration = oldDuration;
            _newDuration = newDuration;
        }

        public void Execute() => _obstacle.HeldDuration = _newDuration;
        public void Undo() => _obstacle.HeldDuration = _oldDuration;
    }

    public class MoveSectionCommand : IEditCommand
    {
        private readonly SectionViewModel _section;
        private readonly int _oldStart;
        private readonly int _newStart;

        public string Description => "Move Section";

        public MoveSectionCommand(SectionViewModel section, int oldStart, int newStart)
        {
            _section = section;
            _oldStart = oldStart;
            _newStart = newStart;
        }

        public void Execute() => _section.StartSample = _newStart;
        public void Undo() => _section.StartSample = _oldStart;
    }

    public class ResizeSectionCommand : IEditCommand
    {
        private readonly SectionViewModel _section;
        private readonly int _oldStart;
        private readonly int _oldLength;
        private readonly int _newStart;
        private readonly int _newLength;

        public string Description => "Resize Section";

        public ResizeSectionCommand(SectionViewModel section, int oldStart, int oldLength, int newStart, int newLength)
        {
            _section = section;
            _oldStart = oldStart;
            _oldLength = oldLength;
            _newStart = newStart;
            _newLength = newLength;
        }

        public void Execute()
        {
            _section.StartSample = _newStart;
            _section.Length = _newLength;
        }

        public void Undo()
        {
            _section.StartSample = _oldStart;
            _section.Length = _oldLength;
        }
    }

    public class DeleteSectionCommand : IEditCommand
    {
        private readonly SectionViewModel _section;
        private readonly ObservableCollection<SectionViewModel> _collection;
        private int _removedIndex;

        public string Description => "Delete Section";

        public DeleteSectionCommand(SectionViewModel section, ObservableCollection<SectionViewModel> collection)
        {
            _section = section;
            _collection = collection;
        }

        public void Execute()
        {
            _removedIndex = _collection.IndexOf(_section);
            _collection.Remove(_section);
        }

        public void Undo()
        {
            if (_removedIndex >= 0 && _removedIndex <= _collection.Count)
                _collection.Insert(_removedIndex, _section);
            else
                _collection.Add(_section);
        }
    }

    public class ModifySectionCommand : IEditCommand
    {
        private readonly SectionViewModel _section;
        private readonly IntensityLevel _oldIntensity;
        private readonly bool _oldAngelJump;
        private readonly IntensityLevel _newIntensity;
        private readonly bool _newAngelJump;

        public string Description => "Modify Section";

        public ModifySectionCommand(SectionViewModel section, IntensityLevel newIntensity, bool newAngelJump)
        {
            _section = section;
            _oldIntensity = section.Intensity;
            _oldAngelJump = section.IsAngelJump;
            _newIntensity = newIntensity;
            _newAngelJump = newAngelJump;
        }

        public void Execute()
        {
            _section.Intensity = _newIntensity;
            _section.IsAngelJump = _newAngelJump;
        }

        public void Undo()
        {
            _section.Intensity = _oldIntensity;
            _section.IsAngelJump = _oldAngelJump;
        }
    }

    public class ModifyHeaderCommand<T> : IEditCommand
    {
        private readonly Action<T> _setter;
        private readonly T _oldValue;
        private readonly T _newValue;

        public string Description => "Modify Header";

        public ModifyHeaderCommand(Action<T> setter, T oldValue, T newValue)
        {
            _setter = setter;
            _oldValue = oldValue;
            _newValue = newValue;
        }

        public void Execute() => _setter(_newValue);
        public void Undo() => _setter(_oldValue);
    }

    public class CompositeCommand : IEditCommand
    {
        private readonly List<IEditCommand> _commands;

        public string Description { get; }

        public CompositeCommand(string description, List<IEditCommand> commands)
        {
            Description = description;
            _commands = commands;
        }

        public void Execute()
        {
            foreach (var command in _commands)
                command.Execute();
        }

        public void Undo()
        {
            for (int i = _commands.Count - 1; i >= 0; i--)
                _commands[i].Undo();
        }
    }
}
