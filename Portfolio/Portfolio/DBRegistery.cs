using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExperimentalSQLite;

namespace WebServer {
    public abstract class DBRegistery<TDatabase, TTable, TRow, TValue> : SQLiteDatabase<TDatabase>.SQLiteTable<TTable, TRow>
        where TDatabase : SQLiteDatabase<TDatabase>
        where TTable : DBRegistery<TDatabase, TTable, TRow, TValue>
        where TRow : DBRegistery<TDatabase, TTable, TRow, TValue>.Row {
        protected DBRegistery(TDatabase db, string tableName) : base(db, tableName) { }

        public void TryPull(ref TRow row) {
            using (var cmd = new SQLiteCommand($"SELECT * FROM `{TableName}` WHERE `{row.Key.ColumnName}` = @{row.Key.ColumnName};", Database.Connection)) {
                cmd.Parameters.Add(new SQLiteParameter($"@{row.Key.ColumnName}", row.Key.DataType) {
                    Value = row.Key.Value
                });
                DbDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows) {
                    _ = reader.Read();
                    row.Pull(reader);
                }
            }
        }

        public class Row : SQLiteRow {
            public override IEnumerable<IDbCell> Fields => new IDbCell[] { Id, Key, Value };
            public override bool IsInDb => 0 < Id;
            public readonly DbPrimaryCell Id = new DbPrimaryCell();
            public readonly DbCell<string> Key = new DbCell<string>(nameof(Key), DbType.String, string.Empty, DbCellFlags.UniqueKey | DbCellFlags.NotNull);
            public readonly DbCell<TValue> Value;

            public Row(TTable table, DbCell<TValue> valueField) : base(table) {
                Value = valueField;
            }

            public virtual TRow Pull(DbDataReader reader) {
                long id = reader.GetInt64(reader.GetOrdinal(Id.ColumnName));
                if (IsInDb && Id != id)
                    throw new InvalidOperationException($"Registry Id collision. The Pull method was called with rowId of {id}, cached rowId is {Id}");
                else Id.Value = id;
                Key.Value = reader.GetString(reader.GetOrdinal(Key.ColumnName));
                return this as TRow;
            }
        }
    }
}
