using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExperimentalSQLite {
    public abstract partial class SQLiteDatabase<TDatabase> { // SQLiteRow.cd
        public abstract partial class SQLiteTable<TTable, TRow> { // SQLiteRow.cd
            public abstract class SQLiteRow {
                public readonly TTable Table;
                public abstract bool IsInDb { get; }
                public TDatabase Database => Table.Database;
                public SQLiteRow(TTable table) {
                    Table = table;
                    if (Table != null && this is TRow tRow)
                        Table.CachedRows.Add(tRow);
                }

                public abstract IEnumerable<IDbCell> Fields { get; }

                /// <summary>Push/commit the changes to DB. you'll need to override this method if you have more than one primary key or if you have no primary/unique keys</summary>
                /// <returns>The row with the updated primary key if it was inserted</returns>
                /// <exception cref="InvalidOperationException">Thrown when no primary/unique keys are found</exception>
                public virtual TRow Push()  {
                    IEnumerable<IDbCell> schema = Fields;

                    // Filter out auto-increment primary key cells and unique key cells
                    IEnumerable<IDbCell> primaryKeyCells = schema.Where(cell => cell.Constraints.HasFlag(DbCellFlags.PrimaryKey) && cell.Constraints.HasFlag(DbCellFlags.AutoIncrement));
                    IEnumerable<IDbCell> uniqueKeyCells = schema.Where(cell => cell.Constraints.HasFlag(DbCellFlags.UniqueKey) && !cell.Constraints.HasFlag(DbCellFlags.AutoIncrement));

                    IEnumerable<IDbCell> dirtyCells;

                    if (!IsInDb) {
                        // Assume all fields are dirty since we are inserting
                        dirtyCells = schema.Where(cell => !cell.Constraints.HasFlag(DbCellFlags.AutoIncrement));
                        // Construct the insert command
                        List<SQLiteParameter> parameters = new List<SQLiteParameter>();
                        string columnNames = string.Join(", ", dirtyCells.Select(cell => $"`{cell.ColumnName}`"));
                        string valuePlaceholders = string.Join(", ", dirtyCells.Select(cell => {
                            string paramName = $"@{cell.ColumnName}";
                            parameters.Add(new SQLiteParameter(paramName, cell.DataType) {
                                Value = cell.Value
                            });
                            return paramName;
                        }));
                        string insertCommand = $"INSERT INTO `{Table.TableName}`({columnNames}) VALUES({valuePlaceholders});";

                        using (SQLiteCommand cmd = new SQLiteCommand(insertCommand, Table.Database.Connection)) {
                            // Add parameters for each dirty cell
                            cmd.Parameters.AddRange(parameters.ToArray());

                            // Execute the insert command
                            int rowsAffected = cmd.ExecuteNonQuery();
                            Table.Database.OnLog(new SQLog($"Row inserted. Rows affected: {rowsAffected}", cmd, SQLogFlags.Info));

                            // If the table has a primary key that is not auto-increment, update cached values of primary key cells
                            if (primaryKeyCells.Any()) {
                                // Retrieve the last inserted row to get the primary key value

                                /*using (SQLiteCommand selectLastInsertRowCmd = new SQLiteCommand("SELECT last_insert_rowid();", Table.Database.Connection)) {
                                    long lastInsertedRowId = (long)selectLastInsertRowCmd.ExecuteScalar();

                                    // Update the cached value of primary key cells
                                    foreach (var cell in primaryKeyCells) {
                                        cell.SetValue(lastInsertedRowId);
                                    }
                                }*/
                                foreach (var cell in primaryKeyCells) // Only works if there is 1 autoincremented primary key
                                    cell.SetValue(Table.Database.Connection.LastInsertRowId);
                            }

                            // Reset dirty cell states
                            foreach (IDbCell cell in dirtyCells)
                                cell.OnSaved();
                        }
                    }
                    else {
                        dirtyCells = schema.Where(cell => cell.IsDirty && !cell.Constraints.HasFlag(DbCellFlags.AutoIncrement));

                        // If none of the fields are dirty, return early
                        if (!dirtyCells.Any()) return this as TRow;

                        // Construct the update command based on the primary key or unique keys
                        string whereClause = "";
                        string setClause = "";
                        List<SQLiteParameter> parameters = new List<SQLiteParameter>();
                        if (primaryKeyCells.Any()) {
                            // Add conditions for the primary key values
                            foreach (IDbCell cell in primaryKeyCells) {
                                string cachedParamName = $"@Cached{cell.ColumnName}";
                                whereClause += $" `{cell.ColumnName}` = {cachedParamName}";
                                parameters.Add(new SQLiteParameter(cachedParamName, cell.DataType) {
                                    Value = cell.CachedValue
                                });
                            }
                        }
                        else if (uniqueKeyCells.Any()) {
                            foreach (IDbCell cell in uniqueKeyCells) {
                                string cachedParamName = $"@Cached{cell.ColumnName}";
                                whereClause += $" `{cell.ColumnName}` = {cachedParamName}";
                                parameters.Add(new SQLiteParameter(cachedParamName, cell.DataType) {
                                    Value = cell.CachedValue
                                });
                            }
                        }
                        else {
                            // Unable to construct update command without primary key or unique keys
                            throw new InvalidOperationException("Cannot save row: no primary key or unique keys found.");
                        }

                        foreach (IDbCell cell in dirtyCells) {
                            string cachedParamName = $"@{cell.ColumnName}";
                            setClause += $" `{cell.ColumnName}` = {cachedParamName}";
                            parameters.Add(new SQLiteParameter(cachedParamName, cell.DataType) {
                                Value = cell.Value
                            });
                        }

                        using (SQLiteCommand cmd = new SQLiteCommand(
                            $"UPDATE `{Table.TableName}` SET{setClause} WHERE{whereClause};",
                            Table.Database.Connection
                        )) {
                            cmd.Parameters.AddRange(parameters.ToArray());

                            // Execute the update command
                            int rowsAffected = cmd.ExecuteNonQuery();
                            Console.WriteLine($"Row updated. Rows affected: {rowsAffected}");
                            foreach (IDbCell cell in dirtyCells)
                                cell.OnSaved(); // Will reset cached values
                        }
                    }
                    return this as TRow;
                }
            }
        }
    }
}
