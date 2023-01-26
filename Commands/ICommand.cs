namespace WebServer.Commands {
    public interface ICommand {
        string Name { get; }
        string Description { get; }
        string[] Aliases { get; }
        void Execute(string[] args);
        string Help(string[] args);
    }
}
