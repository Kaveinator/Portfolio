using System.Data;
using System.Data.SQLite;
using System.Runtime.CompilerServices;
using ExperimentalSQLite;

namespace Portfolio.DevLog.Data {
    public class DevLogTagsTable : PortfolioDatabase.SQLiteTable<DevLogTagsTable, DevLogTagInfo> {
        public DevLogTagsTable(PortfolioDatabase database) : base(database, "DevLogTags") { }

        public override DevLogTagInfo ConstructRow() => new DevLogTagInfo(this);

        public IEnumerable<DevLogTagInfo> GetTagsFromBindingsSet(IEnumerable<DevLogTagBindingInfo> bindingsSet) {
            if (!bindingsSet.Any()) return Enumerable.Empty<DevLogTagInfo>();

            var tagIds = bindingsSet.Select(bind => bind.TagId.Value).Distinct();
            string whereClause = string.Join(" OR ", tagIds.Select(tagId => $"`{Schema.TagId.ColumnName}` = {tagId}"));
            using (SQLiteCommand cmd = new SQLiteCommand($"SELECT * FROM `{TableName}` WHERE {whereClause};", Database.Connection))
                return ReadFromReader(cmd.ExecuteReader());
        }
    }
    public class DevLogTagInfo : DevLogTagsTable.SQLiteRow {
        public override IEnumerable<IDbCell> Fields => new IDbCell[] { TagId, ClassName, TagName, ParentTagId };
        public override bool IsInDb => TagId.Value > 0;
        public readonly DbPrimaryCell TagId = new DbPrimaryCell(nameof(TagId));
        public readonly DbCell<string> ClassName = new DbCell<string>(nameof(ClassName), DbType.String, constraints: DbCellFlags.NotNull | DbCellFlags.UniqueKey);
        public readonly DbCell<string> TagName = new DbCell<string>(nameof(TagName), DbType.String, constraints: DbCellFlags.NotNull);
        public readonly DbForeignCellWithCheck<long?, long> ParentTagId;

        public DevLogTagInfo(DevLogTagsTable table) : base(table) {
            string parentTagIdName = nameof(ParentTagId);
            ParentTagId = new DbForeignCellWithCheck<long?, long>(parentTagIdName, table, TagId,
                $"`{parentTagIdName}` ISNULL OR `{TagId.ColumnName}` <> `{parentTagIdName}`", null
            );
        }
    }
    public static class DevLogTagInfoExtensions {
        public static string ToHtml(this IEnumerable<DevLogTagInfo> tags) {
            return string.Join('\n', tags.Select(tag => $"<span class=\"tag {tag.ClassName}\">{tag.TagName}</span>"));
        }
    }
}
