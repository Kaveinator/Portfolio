using System.Data.SQLite;
using System.Data;

namespace ExperimentalSQLite {
    public abstract class WhereClause {
        public abstract SQLiteParameter ToParameter();
    }
    public class WhereClause<T> : WhereClause {
        public readonly DbCell<T> Column;
        public readonly string Operator;
        public readonly T Value;
        string ParamName => $"@{Column.ColumnName}Value";

        public WhereClause(DbCell<T> column, string _operator, T value) {
            Column = column;
            Operator = _operator;
            Value = value;
        }

        public override string ToString()
            => $"`{Column.ColumnName}` {Operator} {ParamName}";

        public override SQLiteParameter ToParameter()
            => new SQLiteParameter(ParamName, Column.DataType) {
            Value = Value
        };
    }
}