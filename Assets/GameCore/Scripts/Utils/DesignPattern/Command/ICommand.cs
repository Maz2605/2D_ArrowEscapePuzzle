namespace GameCore.Utils.DesignPattern.Command
{
    public interface ICommand
    {
        bool Execute();
        void Undo();
    }
}