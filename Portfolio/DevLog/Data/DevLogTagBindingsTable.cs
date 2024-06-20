using System.Data;
using System.Data.SQLite;
using ExperimentalSQLite;

namespace Portfolio.DevLog.Data {
    public class DevLogTagBindingsTable : PortfolioDatabase.SQLiteTable<DevLogTagBindingsTable, DevLogTagBindingInfo> {
        internal readonly DevLogPostsTable PostsTableRefrence;
        internal readonly DevLogTagsTable TagsTableRefrence;
        // This is another method of getting refrence tables, you could still refrence them through Database, since they are defined there, but it makes it more obvious ig
        public DevLogTagBindingsTable(PortfolioDatabase database, DevLogPostsTable postsRefrence, DevLogTagsTable tagsRefrence)
            : base(database, "DevLogTagBindings") {
            PostsTableRefrence = postsRefrence;
            TagsTableRefrence = tagsRefrence;
        }

        public override DevLogTagBindingInfo ConstructRow() => new DevLogTagBindingInfo(this);

        public IEnumerable<DevLogTagBindingInfo> GetAllTagsForPosts(IEnumerable<DevLogPostInfo> posts) {
            if (!posts.Any()) return Array.Empty<DevLogTagBindingInfo>();
            var postIds = posts.Select(post => post.PostId.Value).Distinct();
            string whereClause = string.Join(" OR ", postIds.Select(postId => $"`{Schema.PostId.ColumnName}` = {postId}"));
            using (SQLiteCommand cmd = new($"SELECT * FROM `{TableName}` WHERE {whereClause};", Database.Connection)) {
                return ReadFromReader(cmd.ExecuteReader());
            }
        }

        public IEnumerable<DevLogTagBindingInfo> GetTagBindingsForPost(DevLogPostInfo post) {
            return post == null ? Array.Empty<DevLogTagBindingInfo>()
            : Query(
                new WhereClause<long>(Schema.PostId, "=", post.PostId.Value)
            );
        }
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
