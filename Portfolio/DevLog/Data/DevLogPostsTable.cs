using System.Data;
using ExperimentalSQLite;
using WebServer.Models;

namespace Portfolio.DevLog.Data {
    public class DevLogPostsTable : PortfolioDatabase.SQLiteTable<DevLogPostsTable, DevLogPostInfo> {
        public DevLogPostsTable(PortfolioDatabase database) : base(database, "DevLogPosts") { }

        public override DevLogPostInfo ConstructRow() => new DevLogPostInfo(this);
    }
    public class DevLogPostInfo : DevLogPostsTable.SQLiteRow, IPageModel {
        public override IEnumerable<IDbCell> Fields => new IDbCell[] { PostId, TitleText, UrlName, ContentMarkdown, CreatedTimestamp, LastUpdatedTimestamp, IsPublished };
        public Dictionary<string, object> Values => new(Fields.Omit(IsPublished).ToDictionary(field => field.ColumnName, field => field.Value)) {
            { "CreatedDate", $"{CreatedTimestamp.Value:MMM d yyyy}" },
            { "LastUpdatedDate", $"{LastUpdatedTimestamp.Value:MMM d yyyy}" }
        };
        public override bool IsInDb => PostId.Value > 0;
        public readonly DbPrimaryCell PostId = new DbPrimaryCell(nameof(PostId));
        public readonly DbStringCell UrlName = new DbStringCell(nameof(UrlName), constraints: DbCellFlags.NotNull | DbCellFlags.UniqueKey, collation: StringCollation.NOCASE);
        public readonly DbStringCell TitleText = new DbStringCell(nameof(TitleText), constraints: DbCellFlags.NotNull, collation: StringCollation.NOCASE);
        public readonly DbCell<string> ContentMarkdown = new DbCell<string>(nameof(ContentMarkdown), DbType.String, constraints: DbCellFlags.NotNull);
        public readonly DbDateTimeCell<DateTime> CreatedTimestamp = new DbDateTimeCell<DateTime>(nameof(CreatedTimestamp), DateTime.Now, constraints: DbCellFlags.NotNull);
        public readonly DbDateTimeCell<DateTime> LastUpdatedTimestamp = new DbDateTimeCell<DateTime>(nameof(LastUpdatedTimestamp), DateTime.Now, constraints: DbCellFlags.NotNull);
        public readonly DbCell<bool> IsPublished = new DbCell<bool>(nameof(IsPublished), DbType.Boolean, false, DbCellFlags.NotNull);
        
        public DevLogPostInfo(DevLogPostsTable table) : base(table) { }
    }
}
