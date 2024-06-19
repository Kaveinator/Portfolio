﻿using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExperimentalSQLite;

namespace Portfolio.Data {
    public class DevLogTags : PortfolioDatabase.SQLiteTable<DevLogTags, DevLogTagInfo> {
        public DevLogTags(PortfolioDatabase database) : base(database, nameof(DevLogTags)) { }

        public override DevLogTagInfo ConstructRow() => new DevLogTagInfo(this);
    }
    public class DevLogTagInfo : DevLogTags.SQLiteRow {
        public override IEnumerable<IDbCell> Fields => new IDbCell[] { TagId, ClassName, TagName, ParentTagId };
        public override bool IsInDb => TagId.Value > 0;
        public readonly DbPrimaryCell TagId = new DbPrimaryCell(nameof(TagId));
        public readonly DbCell<string> ClassName = new DbCell<string>(nameof(ClassName), DbType.String, constraints: DbCellFlags.NotNull | DbCellFlags.UniqueKey);
        public readonly DbCell<string> TagName = new DbCell<string>(nameof(TagName), DbType.String, constraints: DbCellFlags.NotNull);
        public readonly DbForeignCellWithCheck<long?, long> ParentTagId;

        public DevLogTagInfo(DevLogTags table) : base(table) {
            string parentTagIdName = nameof(ParentTagId);
            ParentTagId = new DbForeignCellWithCheck<long?, long>(parentTagIdName, table, TagId,
                $"`{parentTagIdName}` ISNULL OR `{TagId.ColumnName}` <> `{parentTagIdName}`", null
            );
        }
    }
}