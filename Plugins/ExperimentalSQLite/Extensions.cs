using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExperimentalSQLite {
    public static class Extensions {
        public static string GetDbTypeAsString(this DbType dbType) {
            switch (dbType) {
                case DbType.Int32:
                case DbType.Int64:
                    return "INTEGER";
                case DbType.Decimal:
                    return "REAL";
                case DbType.String:
                    return "TEXT";
                case DbType.Binary:
                    return "BLOB";
                case DbType.DateTime:
                    return "DATETIME";
                default:
                    throw new NotSupportedException($"Unsupported DbType: {dbType}");
            }
        }
    }
}
