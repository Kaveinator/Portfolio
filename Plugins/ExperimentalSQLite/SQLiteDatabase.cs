using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExperimentalSQLite {
    public abstract partial class SQLiteDatabase<TDatabase> : IDisposable // SQLiteDatabase.cs
        where TDatabase : SQLiteDatabase<TDatabase> {
        public readonly string DatabaseName;
        public readonly FileInfo FileInfo;
        public readonly SQLiteConnection Connection;
        public ConnectionState State => Connection?.State ?? ConnectionState.Closed;
        protected readonly List<ITable> RegistedTables = new List<ITable>();

        protected SQLiteDatabase(string databaseName) {
            DatabaseName = databaseName;
            FileInfo = new FileInfo(Path.Combine(Environment.CurrentDirectory, $"{databaseName}.db3"));
            if (!FileInfo.Directory!.Exists)
                FileInfo.Directory.Create();
            Connection = new SQLiteConnection($"URI=file:{FileInfo.FullName}");
        }

        protected abstract void OnLog(SQLog log);

        #region IDisposable Implementation
        public bool IsDisposed { get; private set; } = false;
        public void Dispose() {
            if (IsDisposed) return;
            Connection?.Dispose();
            IsDisposed = true;
            GC.SuppressFinalize(this);
        }

        ~SQLiteDatabase() => Dispose();
        #endregion

        #region Database-Level Operations
        public SQLiteConnection Open() {
            if (State.HasFlag(ConnectionState.Open))
                return Connection;
            return Connection.OpenAndReturn();
        }

        public async Task<SQLiteConnection> OpenAsync() {
            if (!State.HasFlag(ConnectionState.Open))
                await Connection.OpenAsync();
            return Connection;
        }

        const string TableExistsQuery = "SELECT COUNT(*) FROM `sqlite_master` WHERE `type` = 'table' AND `name` = $tableName;";
        public bool TableExists(string tableName) {
            using (SQLiteCommand cmd = new SQLiteCommand(TableExistsQuery, Connection)) {
                cmd.Parameters.Add(new SQLiteParameter("$tableName", DbType.String) {
                    Value = tableName
                });
                try {
                    object countObj = cmd.ExecuteScalar();
                    if (countObj is long count)
                        return count > 0;
                    else throw new Exception("Expected row count, but received type " + countObj.GetType().FullName);
                } catch (Exception ex) {
                    throw new AggregateException($"An error occured while doing TableExists(); [ DatabaseName: {DatabaseName}; tableName: {tableName}; State: {State} ]", ex);
                }
            }
        }

        public async Task<bool> TableExistsAsync(string tableName) {
            using (SQLiteCommand cmd = new SQLiteCommand(TableExistsQuery, Connection)) {
                cmd.Parameters.Add(new SQLiteParameter("$tableName", DbType.String) {
                    Value = tableName
                });
                try {
                    object? countObj = await cmd.ExecuteScalarAsync();
                    if (countObj is long count)
                        return count > 0;
                    else throw new Exception("Expected row count, but received type " + countObj?.GetType().FullName);
                }
                catch (Exception ex) {
                    throw new AggregateException($"An error occured while doing TableExists(); [ DatabaseName: {DatabaseName}; tableName: {tableName}; State: {State} ]", ex);
                }
            }
        }

        public TTable RegisterTable<TTable, TRow>(Func<TTable> tableInitilizer, Func<TRow>? schemaConstructor = null)
            where TTable : SQLiteTable<TTable, TRow>
            where TRow : SQLiteTable<TTable, TRow>.SQLiteRow
        {
            TTable table = tableInitilizer();
            // Check if there is already RegisterTable called that contains the same table.TableName
            if (table is null)
                throw new NullReferenceException($"Failed to register table. tableInitilizer returned a null table[ Database: {typeof(TDatabase).FullName}; table: {typeof(TTable).FullName} ]");
            ITable? cachedTable = RegistedTables.FirstOrDefault(otherTable => table.TableName == otherTable?.TableName, null);
            if (cachedTable != null)
                throw new Exception($"Failed to register table '{table.TableName}'. Another table with that name was already registed with that name. [ table: {typeof(TTable).FullName}; cachedTable: {cachedTable.GetType().FullName} ]");
            TRow schema = table.Schema = schemaConstructor?.Invoke() ?? table.ConstructRow();
            if (table.CachedRows.Contains(schema))
                table.CachedRows.Remove(schema);
            if (!TableExists(table.TableName)) {
                string cmdText = $"CREATE TABLE IF NOT EXISTS `{table.TableName}` (";
                
                // Construct the table schema
                StringBuilder foreignCellClause = new StringBuilder();
                foreach (IDbCell cell in schema.Fields) {
                    cmdText += $"{cell.ToCreateTableString()},";
                    if (cell is IDbForeignCell foreignCell)
                        foreignCellClause.Append($"FOREIGN KEY (`{cell.ColumnName}`) REFERENCES `{foreignCell.ForeignTableRefrence.TableName}`(`{foreignCell.ForeignCellRefrence.ColumnName}`),");
                }
                cmdText += foreignCellClause;
                cmdText = cmdText.TrimEnd(',') + ");";

                using (SQLiteCommand cmd = new SQLiteCommand(cmdText, Connection)) {
                    int rowsAffected = cmd.ExecuteNonQuery();
                    OnLog(new SQLog($"Table '{table.TableName}' created. Rows affected: {rowsAffected}"));
                    //Console.WriteLine($"Table '{table.TableName}' created. Rows affected: {rowsAffected}");
                }
            }
            RegistedTables.Add(table);
            return table;
        }
        #endregion
    }
}