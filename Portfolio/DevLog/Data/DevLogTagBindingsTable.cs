using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExperimentalSQLite;

namespace Portfolio.DevLog.Data {
    public class DevLogTagBindingsTable : PortfolioDatabase.SQLiteTable<DevLogTagBindingsTable, DevLogTagBindingInfo> {
        internal readonly DevLogPostsTable PostsTableRefrence;
        internal readonly DevLogTagsTable TagsTableRefrence;
        // This is another method of getting refrence tables, you could still refrence them through Database, since they are defined there, but it makes it more obvious ig
        public DevLogTagBindingsTable(PortfolioDatabase database, DevLogPostsTable postsRefrence, DevLogTagsTable tagsRefrence)
            : base(database, nameof(DevLogTagBindingsTable)) {
            PostsTableRefrence = postsRefrence;
            TagsTableRefrence = tagsRefrence;
        }

        public override DevLogTagBindingInfo ConstructRow() => new DevLogTagBindingInfo(this);
    }
    public class DevLogTagBindingInfo : DevLogTagBindingsTable.SQLiteRow {
        public override IEnumerable<IDbCell> Fields => new IDbCell[] { BindingId, PostId, TagId, Priority };
        public override bool IsInDb => BindingId.Value > 0;
        public readonly DbPrimaryCell BindingId = new DbPrimaryCell(nameof(BindingId));
        public readonly DbForeignCell<long> PostId;
        public readonly DbForeignCell<long> TagId;
        public readonly DbCell<ushort> Priority = new DbCell<ushort>(nameof(Priority), DbType.UInt16, 0, DbCellFlags.NotNull);

        public DevLogTagBindingInfo(DevLogTagBindingsTable table) : base(table) {
            PostId = new DbForeignCell<long>(nameof(PostId), table.PostsTableRefrence, table.PostsTableRefrence.Schema.PostId, constraints: DbCellFlags.NotNull);
            TagId = new DbForeignCell<long>(nameof(TagId), table.TagsTableRefrence, table.TagsTableRefrence.Schema.TagId, constraints: DbCellFlags.NotNull);
        }
    }
}
