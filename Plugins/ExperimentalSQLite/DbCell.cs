using System;
using System.Data;
using System.Data.Common;
using System.Globalization;

namespace ExperimentalSQLite {
    public interface IDbCell {
        string ColumnName { get; }
        DbType DataType { get; }
        DbCellFlags Constraints { get; }
        bool IsDirty { get; }
        object Value { get; }
        object CachedValue { get; }

        string ToCreateTableString();

        void OnSaved();
        void SetValue(object value);
        void FromReader(DbDataReader reader);
    }
    public class DbCell<TValue> : IDbCell {
        #region IDbCell Implementation
        string IDbCell.ColumnName => ColumnName;
        DbType IDbCell.DataType => DataType;
        DbCellFlags IDbCell.Constraints => Constraints;
        object IDbCell.Value => Value;
        object IDbCell.CachedValue => CachedValue;
        void IDbCell.OnSaved() => CachedValue = Value;
        void IDbCell.SetValue(object value) {
            if (value is TValue tValue)
                CachedValue = Value = tValue;
        }

        public virtual string ToCreateTableString()
            => $"`{ColumnName}` {DataType.GetDbTypeAsString()}"
                + (Constraints.HasFlag(DbCellFlags.PrimaryKey) ? " PRIMARY KEY" : "")
                + (Constraints.HasFlag(DbCellFlags.UniqueKey) ? " UNIQUE" : "")
                + (Constraints.HasFlag(DbCellFlags.NotNull) ? " NOT NULL" : "");
        #endregion

        public readonly string ColumnName;
        public readonly DbType DataType;
        public readonly DbCellFlags Constraints;
        public TValue Value;
        TValue CachedValue;
        public bool IsDirty => !Value.Equals(CachedValue);
        public DbCell(string name, DbType type, TValue defaultValue = default, DbCellFlags constraints = DbCellFlags.None) {
            ColumnName = name;
            DataType = type;
            CachedValue = Value = defaultValue;
            Constraints = constraints;
        }

        public virtual void FromReader(DbDataReader reader) {
            int ordinal = reader.GetOrdinal(ColumnName);
            Type valueType = typeof(TValue);
            TValue incomingValue = reader.IsDBNull(ordinal) ? default!
                : (TValue)Convert.ChangeType(reader.GetValue(ordinal), Nullable.GetUnderlyingType(valueType) ?? valueType);
            bool overrideCurrentValue = (CachedValue ?? incomingValue).Equals(Value);
            CachedValue = incomingValue;
            if (overrideCurrentValue)
                Value = incomingValue;
        }

        public override string ToString() => Value?.ToString() ?? string.Empty;

        public override int GetHashCode() => Value?.GetHashCode() ?? default;

        public override bool Equals(object other) {
            if (other is DbCell<TValue> otherCell)
                return otherCell.Value.Equals(Value);
            if (other is TValue otherValue)
                return otherValue.Equals(Value);
            return false;
        }

        // Implicit conversion from DbCell<TValue> to TValue
        public static implicit operator TValue(DbCell<TValue> cell) => cell.Value!;

        public static bool operator ==(DbCell<TValue> a, DbCell<TValue> b) {
            if (a is null && b is null) return true;
            if (a is null || b is null) return false;
            return a.Value.Equals(b.Value);
        }
        public static bool operator !=(DbCell<TValue> a, DbCell<TValue> b) => !(a == b);
        public static bool operator ==(TValue a, DbCell<TValue> b) {
            if (b is null) return false;
            return a.Equals(b.Value);
        }
        public static bool operator !=(TValue a, DbCell<TValue> b) => !(a == b);
        public static bool operator ==(DbCell<TValue> a, TValue b) => b == a;
        public static bool operator !=(DbCell<TValue> a, TValue b) => !(a == b);
    }
    [Flags] public enum DbCellFlags {
        None = 0,
        PrimaryKey = 1 << 0,
        UniqueKey = 1 << 1,
        NotNull = 1 << 2,
        AutoIncrement = 1 << 3,
    }
    public class DbDateTimeCell<TDateTime> : DbCell<TDateTime> {
        protected List<string> Formats = new List<string>() { "yyyy-MM-dd HH:mm:ss.ffffff", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd" };
        public DbDateTimeCell(string name, TDateTime defaultValue = default, string? customFormat = null, DbCellFlags constraints = DbCellFlags.None)
            : base(name, DbType.DateTime, defaultValue, constraints) 
        {
            if (!string.IsNullOrEmpty(customFormat) && !Formats.Contains(customFormat))
                Formats.Insert(0, customFormat);
        }

        public override void FromReader(DbDataReader reader) {
            int ordinal = reader.GetOrdinal(ColumnName);
            if (reader.IsDBNull(ordinal)) {
                Value = default;
                return;
            }

            string unparsedDateTime = reader.GetString(ordinal);
            if (!DateTime.TryParseExact(unparsedDateTime, Formats.ToArray(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDateTime))
                throw new FormatException($"Unable to parse '{ColumnName}' as DateTime! Incoming value: `{unparsedDateTime}`");

            Value = (TDateTime)(object)parsedDateTime;
        }
    }
    public class DbPrimaryCell : DbCell<long> {
        const DbCellFlags PrimaryBaseFlags = DbCellFlags.PrimaryKey | DbCellFlags.UniqueKey | DbCellFlags.NotNull;
        const DbCellFlags PrimaryAutoFlags = PrimaryBaseFlags | DbCellFlags.AutoIncrement;
        public DbPrimaryCell(string name = "Id", bool autoIncrement = true) 
            : base(name, DbType.Int64, -1L, autoIncrement ? PrimaryAutoFlags : PrimaryBaseFlags) 
            { }

        public override void FromReader(DbDataReader reader)
            => Value = reader.GetInt64(reader.GetOrdinal(ColumnName));
    }
    public interface IDbForeignCell : IDbCell {
        ITable ForeignTableRefrence { get; }
        IDbCell ForeignCellRefrence { get; }
    }
    public class DbForeignCell<TValue, TForeignValue> : DbCell<TValue>, IDbForeignCell {
        public readonly DbCell<TForeignValue> Refrence;
        public readonly ITable TableRefrence;
        IDbCell IDbForeignCell.ForeignCellRefrence => Refrence;
        ITable IDbForeignCell.ForeignTableRefrence => TableRefrence;

        public DbForeignCell(string name, ITable foreignTable, DbCell<TForeignValue> foreignKeyRefrence, TValue defaultValue = default, DbCellFlags constraints = DbCellFlags.None) 
            : base(name, foreignKeyRefrence.DataType, defaultValue, constraints)
        {
            TableRefrence = foreignTable;
            Refrence = foreignKeyRefrence;
        }
    }

    public class DbForeignCell<TValue> : DbForeignCell<TValue, TValue> {
        public DbForeignCell(string name, ITable foreignTable, DbCell<TValue> foreignKeyRefrence, TValue defaultValue = default, DbCellFlags constraints = DbCellFlags.None)
            : base(name, foreignTable, foreignKeyRefrence, defaultValue, constraints) { }
    }
    public class DbForeignCellWithCheck<TValue, TForeignValue> : DbForeignCell<TValue, TForeignValue> {
        public readonly string CheckString;

        /// <summary>Defines a foreign DbCell with a check, keep in mind the check is not sanitized (yet)</summary>
        /// <param name="name">Name of the cell</param>
        /// <param name="foreignTable">Foreign table refrence</param>
        /// <param name="foreignKeyRefrence">DbCell of the cell</param>
        /// <param name="checkStr">Check string. Not sanitized, so be aware</param>
        /// <param name="defaultValue">Default value, in this case refrence, if TValue is nullable then it will be null (assuming constraints don't have 'NotNull')</param>
        /// <param name="constraints">Constraints for the cell <seealso cref=""/></param>
        public DbForeignCellWithCheck(string name, ITable foreignTable, DbCell<TForeignValue> foreignKeyRefrence, string checkStr, TValue defaultValue = default, DbCellFlags constraints = DbCellFlags.None)
            : base(name, foreignTable, foreignKeyRefrence, defaultValue, constraints)
        {
            CheckString = checkStr;
        }

        public override string ToCreateTableString() {
            return $"`{ColumnName}` {DataType.GetDbTypeAsString()} CHECK({CheckString}) "
                + (Constraints.HasFlag(DbCellFlags.PrimaryKey) ? " PRIMARY KEY" : "")
                + (Constraints.HasFlag(DbCellFlags.UniqueKey) ? " UNIQUE" : "")
                + (Constraints.HasFlag(DbCellFlags.NotNull) ? " NOT NULL" : "");
        }
    }
    public class DbForeignCellWithCheck<TValue> : DbForeignCellWithCheck<TValue, TValue> {
        public DbForeignCellWithCheck(string name, ITable foreignTable, DbCell<TValue> foreignKeyRefrence, string checkStr, TValue defaultValue = default, DbCellFlags constraints = DbCellFlags.None)
            : base(name, foreignTable, foreignKeyRefrence, checkStr, defaultValue, constraints) { }
    }
    public class DbStringCell : DbCell<string> {
        public readonly StringCollation? Collation; 
        public DbStringCell(string name, string? defaultValue = null, DbCellFlags constraints = DbCellFlags.None, StringCollation? collation = null) : base(name, DbType.String, defaultValue, constraints) {
            Collation = collation;
        }

        public override string ToCreateTableString() {
            string cmdText = base.ToCreateTableString();
            if (Collation != null)
                cmdText += $" COLLATE {Collation}";
            return cmdText;
        }
    }
    public enum StringCollation {
        BINARY, // byte-by-byte
        NOCASE, // Case doesn't matter
        RTRIM // Ignores Trailing spaces
    }
}
