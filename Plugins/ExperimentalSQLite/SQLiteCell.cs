﻿using System;
using System.Data;

namespace ExperimentalSQLite {
    public interface IDbCell {
        string ColumnName { get; }
        DbType DataType { get; }
        DbCellFlags Constraints { get; }
        bool IsDirty { get; }
        object Value { get; }
        object CachedValue { get; }

        void OnSaved();
        void SetValue(object value);
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

        private TValue TemporaryForceSetCachedValue { set => CachedValue = value; }
        #endregion

        public readonly string ColumnName;
        public readonly DbType DataType;
        public readonly DbCellFlags Constraints;
        public TValue Value;
        TValue CachedValue;
        public bool IsDirty => !Value.Equals(CachedValue);
        public DbCell(string name, DbType type, TValue? defaultValue = default, DbCellFlags constraints = DbCellFlags.None) {
            ColumnName = name;
            DataType = type;
            CachedValue = Value = defaultValue;
            Constraints = constraints;
        }

        public static implicit operator TValue(DbCell<TValue> cell) => cell.Value;

        public override string ToString() => Value.ToString();

        public override int GetHashCode() => Value.GetHashCode();

        public override bool Equals(object other) {
            if (other is DbCell<TValue> otherCell)
                return otherCell.Value.Equals(Value);
            if (other is TValue otherValue)
                return otherValue.Equals(Value);
            return false;
        }

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
    public class DbPrimaryCell : DbCell<long> {
        const DbCellFlags PrimaryBaseFlags = DbCellFlags.PrimaryKey | DbCellFlags.UniqueKey | DbCellFlags.NotNull;
        const DbCellFlags PrimaryAutoFlags = PrimaryBaseFlags | DbCellFlags.AutoIncrement;
        public DbPrimaryCell(string name = "Id", bool autoIncrement = true) 
            : base(name, DbType.Int64, -1L, autoIncrement ? PrimaryAutoFlags : PrimaryBaseFlags) 
            { }
    }
}