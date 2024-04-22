using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExperimentalSQLite {
    public struct SQLog {
        public DateTime Timestamp;
        public SQLogFlags Flags;
        public string? MockedCommand;
        public string Message;

        public SQLog(string message, SQLiteCommand? command = null, SQLogFlags tags = SQLogFlags.None) {
            Timestamp = DateTime.Now;
            Message = message;
            Flags = SQLogFlags.None;
            Flags = tags;

            if (command != null) {
                string commandText = command.CommandText;
                // Replace parameter placeholders with their values
                foreach (SQLiteParameter param in command.Parameters) {
                    if (param is null) continue;
                    string? parameterValueString = param.Value is null || param.Value == DBNull.Value ? "NULL"
                        : param.Value is string || param.Value is char ? $"'{param.Value}'" : param.Value.ToString();
                    commandText = commandText.Replace(param.ParameterName, parameterValueString);
                }
                MockedCommand = commandText;
            } else MockedCommand = null;
        }
    }
    [Flags] public enum SQLogFlags {
        None = 0,
        Info = 1 << 1,
        Warn = 1 << 2,
        Error = 1 << 3,
        Fatal = 1 << 4
    }
}
