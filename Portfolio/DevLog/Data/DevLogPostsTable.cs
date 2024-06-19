using System.Data;
using ExperimentalSQLite;

namespace Portfolio.DevLog.Data {
    public class DevLogPostsTable : PortfolioDatabase.SQLiteTable<DevLogPostsTable, DevLogPostInfo> {
        public DevLogPostsTable(PortfolioDatabase database) : base(database, "DevLogPosts") { }

        public override DevLogPostInfo ConstructRow() => new DevLogPostInfo(this);
    }
    public class DevLogPostInfo : DevLogPostsTable.SQLiteRow {
        public override IEnumerable<IDbCell> Fields => new IDbCell[] { PostId, TitleText, UrlName, ContentMarkdown, CreatedTimestamp, LastUpdatedTimestamp, IsPublished };
        public override bool IsInDb => PostId.Value > 0;
        public readonly DbPrimaryCell PostId = new DbPrimaryCell(nameof(PostId));
        public readonly DbCell<string> UrlName = new DbCell<string>(nameof(UrlName), DbType.String, constraints: DbCellFlags.NotNull | DbCellFlags.UniqueKey);
        public readonly DbCell<string> TitleText = new DbCell<string>(nameof(TitleText), DbType.String, constraints: DbCellFlags.NotNull);
        public readonly DbCell<string> ContentMarkdown = new DbCell<string>(nameof(ContentMarkdown), DbType.String, constraints: DbCellFlags.NotNull);
        public readonly DbDateTimeCell<DateTime> CreatedTimestamp = new DbDateTimeCell<DateTime>(nameof(CreatedTimestamp), DateTime.Now, constraints: DbCellFlags.NotNull);
        public readonly DbDateTimeCell<DateTime> LastUpdatedTimestamp = new DbDateTimeCell<DateTime>(nameof(LastUpdatedTimestamp), DateTime.Now, constraints: DbCellFlags.NotNull);
        public readonly DbCell<bool> IsPublished = new DbCell<bool>(nameof(IsPublished), DbType.Boolean, false, DbCellFlags.NotNull);

        public DevLogPostInfo(DevLogPostsTable table) : base(table) { }
    }
}
