using System;
using System.Data;
using System.Data.SQLite;
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
            public TRow Schema { get; internal set; }
            string ITable.TableName => TableName;
            internal protected List<TRow> CachedRows = new List<TRow>();
            public SQLiteTable(TDatabase db, string tableName) {
                Database = db;
                TableName = tableName;
            }

            // Code for table level operations go here
            public virtual IEnumerable<TRow> GetAll(int limit = 0) {
                Queue<TRow> queue = new Queue<TRow>();
                // Create the command and add parameters
                string command = $"SELECT * FROM `{TableName}`";
                if (limit > 0)
                    command += $" LIMIT {limit}";
                command += ';';
                using (SQLiteCommand cmd = new SQLiteCommand(command, Database.Connection)) {
                    // Execute the command and read the results
                    using (SQLiteDataReader reader = cmd.ExecuteReader()) {
                        while (reader.Read()) {
                            Schema.PullHeader(reader); // Reads primary/unique keys
                            TRow? row = CachedRows.FirstOrDefault(cachedRow => Schema.CompareHeader(cachedRow));
                            if (row == null) {
                                row = ConstructRow();
                                CachedRows.Add(row);
                            }
                            row.Pull(reader);
                            queue.Enqueue(row);
                        }
                    }
                }
                return queue;
            }

            // TODO: Instead of a `WhereClause` there should be an `IClause` so you can also add OrderByClause or similar
            public virtual IEnumerable<TRow> Query(params WhereClause[] whereClauses) => Query(0, whereClauses);
            public virtual IEnumerable<TRow> Query(int limit, params WhereClause[] whereClauses) {
                if (whereClauses.Length == 0) throw new Exception("Attempting to execute command with no where clauses. Since this will retrieve all records from DB, this has been disallowed. Use GetAll instead");
                Queue<TRow> queue = new Queue<TRow>();
                whereClauses = whereClauses.Where(clause => clause != null).ToArray();
                // Build the query
                string command = $"SELECT * FROM `{TableName}`";
                command += " WHERE " + string.Join(" AND ", whereClauses.Select(wc => wc.ToString()));
                if (limit > 0)
                    command += $" LIMIT {limit}";
                command += ';';

                // Create the command and add parameters
                using (SQLiteCommand cmd = new SQLiteCommand(command, Database.Connection)) {
                    foreach (var sqlParam in whereClauses.Select(wc => wc.ToParameter()))
                        cmd.Parameters.Add(sqlParam);
                
                    // Execute the command and read the results
                    using (SQLiteDataReader reader = cmd.ExecuteReader()) {
                        while (reader.Read()) {
                            Schema.PullHeader(reader); // Reads primary/unique keys
                            TRow? row = CachedRows.FirstOrDefault(cachedRow => Schema.CompareHeader(cachedRow));
                            if (row == null) {
                                row = ConstructRow();
                                CachedRows.Add(row);
                            }
                            row.Pull(reader);
                            queue.Enqueue(row);
                        }
                    }
                }
                return queue;
            }

            public abstract TRow ConstructRow();
        }
    }
}
