using System.Data;
using ExperimentalSQLite;

namespace Portfolio.Projects {
    public class ProjectLinksTable : AbstractLinksTable<ProjectLinksTable, ProjectLinkInfo> {
        public ProjectLinksTable(PortfolioDatabase db) : base(db, nameof(ProjectLinkInfo)) { }

        public override ProjectLinkInfo ConstructRow() => new ProjectLinkInfo(this);
    }
    
    public class ProjectLinkInfo : ProjectLinksTable.Row {
        public override IEnumerable<IDbCell> Fields => base.Fields.Include(ProjectId, IsLiveDemo);

        public readonly DbForeignCell<long> ProjectId;
        public readonly DbCell<bool> IsLiveDemo = new DbCell<bool>(nameof(IsLiveDemo), DbType.Boolean, false, DbCellFlags.NotNull);

        public ProjectLinkInfo(ProjectLinksTable table) : base(table) {
            ProjectsTable projects = table.Database.ProjectsTable;
            ProjectId = new DbForeignCell<long>(nameof(ProjectId), projects, projects.Schema.ProjectId, constraints: DbCellFlags.NotNull);
        }
    }
}