using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Portfolio.Commands {
    public struct StopCommand : ICommand {
        public string Name => "Stop command";
        public string Description => "Stops the program and exit";
        public string[] Aliases => new[] { "exit", "shutdown", "stop", "kill" };

        public void Execute(string[] args) {
            if (args.Length > 1) { Help(args); return; }
            Program.Endpoint?.Stop();
            Environment.Exit(0);
        }
        public string Help(string[] args)
            => $"{Description}\nAliases: {string.Join(", ", Aliases)}\nUsage: \n> <{string.Join('/', Aliases)}> [?]\n" +
            $"\nOptions:" +
            $"\n  ? - Help menu";
    }
}
