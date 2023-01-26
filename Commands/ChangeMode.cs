using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebServer.Commands {
    public struct ProgramMode : ICommand {
        static EventLogger Logger = EventLogger.GetOrCreate<ProgramMode>();
        public string Name => "Change Mode";
        public string Description => "Change program mode";
        public string[] Aliases => new[] { "mode", "m" };

        const string CompileFlag = "-b";
        public void Execute(string[] args) {
            if (args.Length == 1) {
                Logger.LogExplicit($"Current mode is '{Program.Mode}'");
                return;
            }
            if (!Enum.TryParse<Mode>(args[1].Trim(), true, out Mode mode)) {
                Logger.LogExplicit($"Unable to parse Mode '{args[1]}'. (Values: {string.Join('/', Enum.GetNames<Mode>())})");
                return;
            }
            Program.Mode = mode;
            //Logger.LogExplicit($"Mode set to '{Program.Mode}'");
            if (mode == Mode.Production && args.Contains(CompileFlag)) {
                BuildCommand.Build();
            }
        }

        public string Help(string[] args)
            => $"{Description}\nAliases: {string.Join(", ", Aliases)}\nUsage: \n> <{string.Join("/", Aliases)}> <Mode> [-s] [?]\n" +
            $"\nOptions:" +
            $"\n  <Mode> - Values: {string.Join("/", Enum.GetNames<Mode>())}" +
            $"\n  {CompileFlag} - Build/rebuild static/production folder (only effective if <Action> parameter is set to 'Production'" +
            $"\n  ? - Help menu" +
            $"" +
            $"\nExamples:" +
            $"\n > {args[0]} {Mode.Development}" +
            $"\n > {args[0]} {Mode.Production} -b";
    }
}
