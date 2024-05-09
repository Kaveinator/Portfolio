using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Portfolio.Commands {
    internal struct ConfigUtility : ICommand {
        // - Reload command
        // - Save command
        internal enum Action { Load, Save, List }
        static EventLogger Logger = EventLogger.GetOrCreate<ConfigUtility>();
        public string Name => "Configuration Utility";
        public string Description => "Load/Save Configurations";
        public string[] Aliases => new[] { "properties", "configs", "conf" };

        public void Execute(string[] args) {
            if (args.Length == 1) { Help(args); return; }
            if (!Enum.TryParse<Action>(args[1], true, out Action action)) {
                Logger.LogWarning($"Unable to parse '{args[1]}' as Action", true);
                return;
            }
            string configName = "*";
            if (args.Length >= 3)
                configName = string.Join(' ', args.Skip(2)).Trim();
            switch (action) {
                case Action.Load:
                    if (configName == "*")
                        Properties.ReloadAll();
                    else if (Properties.TryGet(configName, out JSONFile config)) {
                        config.Load();
                        Logger.LogExplicit($"Loaded '{configName}'");
                    }
                    else Logger.LogWarning($"Config '{configName}' not found", true);
                    break;
                case Action.Save:
                    if (configName == "*")
                        Properties.SaveAll();
                    else if (Properties.TryGet(configName, out JSONFile config)) {
                        config.Save();
                        Logger.LogExplicit($"Loaded '{configName}'");
                    }
                    else Logger.LogWarning($"Config '{configName}' not found", true);
                    break;
                case Action.List:
                    Logger.LogExplicit(string.Join(", ", Properties.Configs.Keys.Select(s => $"\"{s}\"")));
                    break;
            }

        }

        public string Help(string[] args)
            => $"{Description}\nAliases: {string.Join(", ", Aliases)}\nUsage: \n> <{string.Join("/", Aliases)}> <Action: {string.Join("/", Enum.GetNames<Mode>())}> [string: configName] [?]\n" +
            $"\nOptions:" +
            $"\n  Action:" +
            $"\n    {Action.Load} - Load configuration" +
            $"\n    {Action.Save} - Save configuration" +
            $"\n    {Action.List} - List configurations" +
            $"\n  configName - Specifies the config to load/save, when list is used this parameter is ignored" +
            $"\n  ? - Help menu" +
            $"\n" +
            $"\nNote: Some values are using only during initilization, so once reloaded, program might not reflect values in configs" +
            $"\n" +
            $"\nExamples:" +
            $"\n > {args[0]} {Action.Save} *" +
            $"\n > {args[0]} {Action.Load} Program";

    }
}
