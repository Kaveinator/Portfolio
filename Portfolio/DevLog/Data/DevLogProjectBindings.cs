
using ExperimentalSQLite;
using Portfolio.Projects.Data;

namespace Portfolio.DevLog.Data
{
    public class DevLogProjectBindingsTable : PortfolioDatabase.SQLiteTable<DevLogProjectBindingsTable, DevLogProjectBindingInfo> {
        internal readonly DevLogPostsTable PostsTableRefrence;
        internal readonly ProjectsTable ProjectsTableRefrence;

        public DevLogProjectBindingsTable(PortfolioDatabase database, DevLogPostsTable postsRefrence, ProjectsTable projectsTable)
            : base(database, "DevLogProjectBindings") {
            PostsTableRefrence = postsRefrence;
            ProjectsTableRefrence = projectsTable;
        }

        public override DevLogProjectBindingInfo ConstructRow() => new DevLogProjectBindingInfo(this);
    }
    public class DevLogProjectBindingInfo : DevLogProjectBindingsTable.SQLiteRow {
        public override IEnumerable<IDbCell> Fields => new IDbCell[] { BindingId, PostId, ProjectId };
        public override bool IsInDb => BindingId.Value > 0;
        public readonly DbPrimaryCell BindingId = new DbPrimaryCell(nameof(BindingId));
        public readonly DbForeignCell<long> PostId;
        public readonly DbForeignCell<long> ProjectId;

        public DevLogProjectBindingInfo(DevLogProjectBindingsTable table) : base(table) {
            PostId = new DbForeignCell<long>(nameof(PostId), table.PostsTableRefrence, table.PostsTableRefrence.Schema.PostId, constraints: DbCellFlags.NotNull);
            ProjectId = new DbForeignCell<long>(nameof(ProjectId), table.ProjectsTableRefrence, table.ProjectsTableRefrence.Schema.ProjectId, constraints: DbCellFlags.NotNull);
        }
    }
}
