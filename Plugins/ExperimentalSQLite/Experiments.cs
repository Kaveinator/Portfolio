using System.Data.SQLite;
using System.Data;

namespace ExperimentalSQLite {
    public abstract class WhereClause {
        public readonly IDbCell Column;
        public readonly string Operator;
        public readonly object? Value;
        public byte Index;

        public WhereClause(IDbCell column, string _operator, object? value) {
            Column = column;
            Operator = _operator;
            Value = value;
        }

        public override string ToString()
            => $"`{Column.ColumnName}` {Operator} {ParamName}";

        protected string ParamName => $"@{Index}-{Column.ColumnName}-Value";
        
        public void SetIndex(byte index) => Index = index;

        public virtual SQLiteParameter ToParameter()
            => new SQLiteParameter(ParamName, Column.DataType) {
                Value = Value
            };
    }
    public class WhereClause<T> : WhereClause {
        public new readonly DbCell<T> Column;
        public new readonly T Value;

        public WhereClause(DbCell<T> column, string _operator, T value)
            : base(column, _operator, value) {
            Column = column;
            Value = value;
        }

        public WhereClause(DbCell<T> column, char _operator, T value)
            : this(column, _operator.ToString(), value) { }
    }
}