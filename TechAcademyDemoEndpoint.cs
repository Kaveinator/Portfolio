using System.Net;
using System.Web;
using System.Collections.Specialized;
using WebServer.Http;
using SimpleJSON;
using Portfolio.Commands;
using System.Text.RegularExpressions;
using System.Text;
using System.Security.Cryptography;
using ExperimentalSQLite;

namespace Portfolio {
    internal partial class TechAcademyDemoEndpoint : HttpEndpointHandler {
        public readonly TechAcademyDemoDatabase DemoDatabase;
        readonly ServerLocalTimeController ServerLocalTimeMVCDemo;
        readonly NewsLetterController NewsLetterMVCDemo;
        readonly CarInsuranceController CarInsuranceDemo;
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