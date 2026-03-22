namespace ME2LevelEditor.Services
{
    public interface IEditCommand
    {
        string Description { get; }
        void Execute();
        void Undo();
    }
}
