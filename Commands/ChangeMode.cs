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
                Logger.LogExplicit($"Unable to parse Mode '{args[1]}'. (Values: {string.Join('/', Enum.GetValues<Mode>().Select(eVal => $"{eVal}({(int)eVal})"))})");
                return;
            }
            // When you assign a new mode to `Program.Mode` the program will log the change already, so only log if there is no change
            if (Program.Mode == mode)
                Logger.LogExplicit($"Mode is already set to '{mode}'");
            Program.Mode = mode;
            //Logger.LogExplicit($"Mode set to '{Program.Mode}'");
            if (mode == Mode.Production && args.Contains(CompileFlag)) {
                BuildCommand.Build();
            }
        }

        public string Help(string[] args)
            => $"{Description}\nAliases: {string.Join(", ", Aliases)}\nUsage: \n> <{string.Join("/", Aliases)}> <Mode> [-b] [?]\n" +
            $"\nOptions:" +
            $"\n  <Mode> - Values: {string.Join("/", Enum.GetValues<Mode>().Select(eVal => $"{eVal}({(int)eVal})"))}" +
            $"\n  {CompileFlag} - Build/rebuild static/production folder (only effective if <Mode> parameter is set to '{Mode.Production}'" +
            $"\n  ? - Help menu" +
            $"" +
            $"\nExamples:" +
            $"\n > {args[0]} {Mode.Development}" +
            $"\n > {args[0]} {Mode.Production} -b";
    }
}
