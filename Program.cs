using SimpleJSON;
using WebServer.Http;
using Portfolio.Commands;

namespace Portfolio {
    public enum Mode { Development, Production }
    internal class Program {
        static EventLogger Logger = EventLogger.GetOrCreate("", true);
        static readonly JSONFile Config = Properties.GetOrCreate<Program>();
        public static HttpServer Endpoint { get; private set; }
        static Mode _Mode = (Mode)-1;
        public static Mode Mode {
            get => _Mode;
            set {
                if (value == _Mode) return;
                _Mode = value;
                Logger.Log($"Mode set to '{value}'");
                UpdateTitle();
            }
        }
        public static PortfolioEndpoint PortfolioEndpoint;
        public static TechAcademyDemoEndpoint TTADemoEndpoint;

        static void Main(string[] args) {
            Console.Title = "Initilizing...";
            Endpoint = new HttpServer();
            if (args.Contains("--purge")) {
                BuildCommand.Purge();
                Mode = Mode.Development;
            }
            else Mode = Config.GetEnumOrDefault<Mode>(nameof(Mode), Mode.Development);
            if (args.Contains("--build")) {
                BuildCommand.Build();
                return;
            }
            UpdateTitle();
            _ = Endpoint.Start();

            Logger.Log($"PortfollioEndpoint binded? {Endpoint.TryRegisterEndpointHandler<PortfolioEndpoint>(() => new PortfolioEndpoint(Endpoint), out PortfolioEndpoint)}");
            Logger.Log($"TTADemo binded? {Endpoint.TryRegisterEndpointHandler<TechAcademyDemoEndpoint>(() => new TechAcademyDemoEndpoint(Endpoint), out TTADemoEndpoint)}");
            EventLog.AddCommand<ProgramMode>();
            EventLog.AddCommand<BuildCommand>();
            EventLog.AddCommand<BrowserCommands>();
            EventLog.AddCommand<StopCommand>();
            EventLog.AddCommand<CacheControl>();
            EventLog.AddCommand<ConfigUtility>();
            while (true) {
                string input = Logger.ReadLine();
                if (input == null) break;
                if (input.Trim().Equals(string.Empty)) continue;
                _ = EventLog.LaunchCommandfromInput(input);
            }
            EventLog.LaunchCommand<StopCommand>();

        }

        public static void UpdateTitle()
            => Console.Title = $"[{_Mode}] WebServer {AssemblyInfo.Current.PrettyPrint()} - [{Endpoint?.TasksCount ?? 0} Tasks]";
    }
}