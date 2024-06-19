using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExperimentalSQLite;

namespace Portfolio.Data {
    public class DevLogPosts : PortfolioDatabase.SQLiteTable<DevLogPosts, DevLogPostInfo> {
        public DevLogPosts(PortfolioDatabase database) : base(database, nameof(DevLogPostInfo)) { }

        public override DevLogPostInfo ConstructRow() => new DevLogPostInfo(this);
    }
    public class DevLogPostInfo : DevLogPosts.SQLiteRow {
        public override IEnumerable<IDbCell> Fields => new IDbCell[] { PostId, TitleText, ContentMarkdown, CreatedTimestamp, LastUpdatedTimestamp, IsPublished };
        public override bool IsInDb => PostId.Value > 0;
        public readonly DbPrimaryCell PostId = new DbPrimaryCell(nameof(PostId));
        public readonly DbCell<string> TitleText = new DbCell<string>(nameof(TitleText), DbType.String, constraints: DbCellFlags.NotNull);
        public readonly DbCell<string> ContentMarkdown = new DbCell<string>(nameof(ContentMarkdown), DbType.String, constraints: DbCellFlags.NotNull);
        public readonly DbDateTimeCell<DateTime> CreatedTimestamp = new DbDateTimeCell<DateTime>(nameof(CreatedTimestamp), DateTime.Now, constraints: DbCellFlags.NotNull);
        public readonly DbDateTimeCell<DateTime> LastUpdatedTimestamp = new DbDateTimeCell<DateTime>(nameof(LastUpdatedTimestamp), DateTime.Now, constraints: DbCellFlags.NotNull);
        public readonly DbCell<bool> IsPublished = new DbCell<bool>(nameof(IsPublished), DbType.Boolean, false, DbCellFlags.NotNull);

        public DevLogPostInfo(DevLogPosts table) : base(table) { }
    }
}
