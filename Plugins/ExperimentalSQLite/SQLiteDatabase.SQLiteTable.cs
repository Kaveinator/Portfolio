using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExperimentalSQLite {
    public interface ITable {
        string TableName { get; }
    }
    public abstract partial class SQLiteDatabase<TDatabase> { // SQLiteTable.cs
        public abstract partial class SQLiteTable<TTable, TRow> : ITable  // SQLiteTable.cs
            where TTable : SQLiteTable<TTable, TRow>
            where TRow : SQLiteTable<TTable, TRow>.SQLiteRow {
            public readonly TDatabase Database;
            public readonly string TableName;
            string ITable.TableName => TableName;
            protected List<TRow> CachedRows = new List<TRow>();
            public SQLiteTable(TDatabase db, string tableName) {
                Database = db;
                TableName = tableName;
            }

            // Code for table level operations go here
        }
    }
}
