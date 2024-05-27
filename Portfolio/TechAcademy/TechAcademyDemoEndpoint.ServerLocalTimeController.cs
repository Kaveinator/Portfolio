using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Net;
using WebServer;
using WebServer.Http;
using ExperimentalSQLite;
using WebServer.Models;

namespace Portfolio.TechAcademy {
    internal partial class TechAcademyDemoEndpoint { // ServerLocalTimeController.cs
        class ServerLocalTimeController : IPageModel { // I would not recommend to make the controller as the Page Model but here we are
            readonly TimestampsTable Registry;
            public readonly TechAcademyDemoEndpoint Endpoint;
            public long RefreshCount;
            public DateTime CurrentDateTime => DateTime.Now;
            Dictionary<string, object> IPageModel.Values => new() {
                { nameof(RefreshCount), RefreshCount },
                { nameof(CurrentDateTime), CurrentDateTime }
            };
            public ServerLocalTimeController(TechAcademyDemoEndpoint endpoint) {
                Endpoint = endpoint;
                Registry = Endpoint.DemoDatabase.RegisterTable(
                    () => new TimestampsTable(Endpoint.DemoDatabase),
                    () => new RecordedTimestamp(null!)
                );
                RefreshCount = Registry.GetCount();
                Endpoint.AddEventCallback("/orgs/TechAcademy/ServerLocalTimeMVC/demo", OnDemoRequest);
            }
            
            HttpResponse? OnDemoRequest(HttpListenerRequest request) {
                if (!Endpoint.TryGetTemplate("/orgs/TechAcademy/ServerLocalTimeMVC.View.html", out string content, out var statusModel, this))
                    return Endpoint.GetGenericStatusPage(statusModel);
                RefreshCount++;
                _ = new RecordedTimestamp(Registry, CurrentDateTime).Push();
                
                return new HttpResponse() {
                    StatusCode = HttpStatusCode.OK,
                    AllowCaching = false,
                    ContentString = content,
                    MimeString = "text/html",
                };
            }
            class TimestampsTable : TechAcademyDemoDatabase.SQLiteTable<TimestampsTable, RecordedTimestamp> {
                public TimestampsTable(TechAcademyDemoDatabase db)
                    : base(db, "ServerLocalTimeRegistry") { }

                public long GetCount() {
                    using SQLiteCommand command = new SQLiteCommand($"SELECT COUNT(1) FROM `{TableName}`", Database.Connection);
                    return (long)command.ExecuteScalar();
                }
            }
            class RecordedTimestamp : TimestampsTable.SQLiteRow {
                public override IEnumerable<IDbCell> Fields => new IDbCell[] { Id, Timestamp };

                public readonly DbPrimaryCell Id = new DbPrimaryCell();
                public override bool IsInDb => 0 < Id;
                public readonly DbCell<DateTime> Timestamp = new DbCell<DateTime>(nameof(Timestamp), DbType.DateTime, default, DbCellFlags.NotNull);

                public RecordedTimestamp(TimestampsTable table) : base(table) {}
                public RecordedTimestamp(TimestampsTable table, DateTime value) : base(table)
                    => Timestamp.Value = value;
            }
            class Int64Registry : DBRegistery<TechAcademyDemoDatabase, Int64Registry, RegistryField, long> {
                public Int64Registry(TechAcademyDemoDatabase db) : base(db, "ServerLocalTimeRegistry") { }
            }
            class RegistryField : Int64Registry.Row {
                public RegistryField(Int64Registry table, string key) : base(table,
                    new DbCell<long>(nameof(Value), DbType.Int64, 0, DbCellFlags.None)
                ) => Key.Value = key;
                public RegistryField(Int64Registry table) : this(table, null!) { }

                public override RegistryField Pull(DbDataReader reader) {
                    _ = base.Pull(reader);
                    Value.Value = reader.GetInt64(reader.GetOrdinal(Value.ColumnName));
                    return this;
                }
            }
        }
    }
}