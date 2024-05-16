using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExperimentalSQLite;
using WebServer;

namespace Portfolio.Portfolio {
    /*public class Journal<TJournal> : SQLiteDatabase<TJournal>
        where TJournal : SQLiteDatabase<TJournal> {
        public Journal(string name) : base($"logs/{name}") { }

        protected override void OnLog(SQLog log) => throw new NotImplementedException();

        public class EventLog<TTable, TClass, TEvent>
            : SQLiteTable<TTable, Event<>>


            where TJournal : SQLiteDatabase<TJournal> {

        }
    }*/
    public abstract class EventLog<TDatabase, TEventLog, TEvent, TSource>
        : SQLiteDatabase<TDatabase>.SQLiteTable<TEventLog, TEvent>
        where TDatabase : SQLiteDatabase<TDatabase>
        where TEventLog : EventLog<TDatabase, TEventLog, TEvent, TSource>
        where TEvent : EventLog<TDatabase, TEventLog, TEvent, TSource>.Event
        where TSource : class
    {
        public EventLog(TDatabase journal, string tableName)
            : base(journal, tableName) { }

        public abstract class Event : SQLiteRow {
            public override bool IsInDb => EventId.Value > 0;
            public override IEnumerable<IDbCell> Fields => new IDbCell[] { EventId, Severity, Timestamp, Message  };
            public readonly DbPrimaryCell EventId = new DbPrimaryCell(nameof(EventId));
            public readonly DbCell<EventLevel> Severity = new DbCell<EventLevel>(nameof(Severity), DbType.Int16, default, DbCellFlags.NotNull);
            public readonly DbCell<DateTime> Timestamp = new DbCell<DateTime>(nameof(Timestamp), DbType.DateTime, DateTime.Now, DbCellFlags.NotNull);
            public readonly DbCell<string> Message = new DbCell<string>(nameof(Message), DbType.String, null, DbCellFlags.NotNull);

            public Event(TEventLog table, EventLevel severity, string message) : base(table) {
                Severity.Value = severity;
                Message.Value = message;
            }

            public Event(TEventLog table, Exception ex, bool isFatal = false) : base(table) {
                Severity.Value = isFatal ? EventLevel.Critical : EventLevel.Error;
                Message.Value = ex.ToString();
            }
        }
    }
}