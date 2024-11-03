using WebServer.Http;
using ExperimentalSQLite;

namespace Portfolio.TechAcademy {
    public partial class TechAcademyDemoEndpoint : HttpEndpointHandler {
        public readonly TechAcademyDemoDatabase DemoDatabase;
        public readonly ServerLocalTimeController ServerLocalTimeMVCDemo;
        public readonly NewsLetterController NewsLetterMVCDemo;
        public readonly CarInsuranceController CarInsuranceDemo;
        public TechAcademyDemoEndpoint(HttpServer server) : base("kavemans.dev", server) {
            DemoDatabase = TechAcademyDemoDatabase.GetOrCreate();
            ServerLocalTimeMVCDemo = new ServerLocalTimeController(this);
            NewsLetterMVCDemo = new NewsLetterController(this);
            CarInsuranceDemo = new CarInsuranceController(this);
        }
    }
    public partial class TechAcademyDemoDatabase : SQLiteDatabase<TechAcademyDemoDatabase> {
        public static TechAcademyDemoDatabase? Instance { get; private set; }
        public static TechAcademyDemoDatabase GetOrCreate() => Instance = Instance ?? new TechAcademyDemoDatabase();

        public static EventLogger Logger = EventLogger.GetOrCreate<TechAcademyDemoDatabase>();

        protected override void OnLog(SQLog log) => Logger.Log(log.Message);

        protected TechAcademyDemoDatabase() : base($"Data/TTADemo") {
            OpenAsync().Wait();
            Logger.Log("Initilized TTADemo Database");
        }
    }
}