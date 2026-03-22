using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace ME2LevelEditor.ViewModels
{
    public enum SelectionMode
    {
        None,
        SingleObstacle,
        SingleSection,
        MultipleObstacles,
        MultipleSections
    }

    public class SelectionViewModel : ViewModelBase
    {
        public ObservableCollection<ObstacleViewModel> SelectedObstacles { get; }
        public ObservableCollection<SectionViewModel> SelectedSections { get; }

        public SelectionMode Mode
        {
            get
            {
                if (SelectedObstacles.Count == 0 && SelectedSections.Count == 0)
                    return SelectionMode.None;
                if (SelectedObstacles.Count == 1 && SelectedSections.Count == 0)
                    return SelectionMode.SingleObstacle;
                if (SelectedSections.Count == 1 && SelectedObstacles.Count == 0)
                    return SelectionMode.SingleSection;
                if (SelectedObstacles.Count >= 2)
                    return SelectionMode.MultipleObstacles;
                return SelectionMode.MultipleSections;
            }
        }

        public ObstacleViewModel SingleObstacle =>
            Mode == SelectionMode.SingleObstacle ? SelectedObstacles[0] : null;

        public SectionViewModel SingleSection =>
            Mode == SelectionMode.SingleSection ? SelectedSections[0] : null;

        public int SelectionCount => SelectedObstacles.Count + SelectedSections.Count;

        public SelectionViewModel()
        {
            SelectedObstacles = new ObservableCollection<ObstacleViewModel>();
            SelectedSections = new ObservableCollection<SectionViewModel>();
            SelectedObstacles.CollectionChanged += OnCollectionChanged;
            SelectedSections.CollectionChanged += OnCollectionChanged;
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(Mode));
            OnPropertyChanged(nameof(SelectionCount));
            OnPropertyChanged(nameof(SingleObstacle));
            OnPropertyChanged(nameof(SingleSection));
        }

        public void Clear()
        {
            foreach (var obstacle in SelectedObstacles)
                obstacle.IsSelected = false;
            foreach (var section in SelectedSections)
                section.IsSelected = false;
            SelectedObstacles.Clear();
            SelectedSections.Clear();
        }

        public void Select(ObstacleViewModel obstacle)
        {
            Clear();
            obstacle.IsSelected = true;
            SelectedObstacles.Add(obstacle);
        }

        public void Select(SectionViewModel section)
        {
            Clear();
            section.IsSelected = true;
            SelectedSections.Add(section);
        }

        public void ToggleSelect(ObstacleViewModel obstacle)
        {
            if (SelectedObstacles.Contains(obstacle))
            {
                obstacle.IsSelected = false;
                SelectedObstacles.Remove(obstacle);
            }
            else
            {
                obstacle.IsSelected = true;
                SelectedObstacles.Add(obstacle);
            }
        }

        public void ToggleSelect(SectionViewModel section)
        {
            if (SelectedSections.Contains(section))
            {
                section.IsSelected = false;
                SelectedSections.Remove(section);
            }
            else
            {
                section.IsSelected = true;
                SelectedSections.Add(section);
            }
        }

        public void SelectRange(IEnumerable<ObstacleViewModel> obstacles)
        {
            Clear();
            foreach (var obstacle in obstacles)
            {
                obstacle.IsSelected = true;
                SelectedObstacles.Add(obstacle);
            }
        }
    }
}
