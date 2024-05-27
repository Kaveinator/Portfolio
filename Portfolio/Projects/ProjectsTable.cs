using System.Data;
using ExperimentalSQLite;

namespace Portfolio.Projects {
    public class ProjectsTable : PortfolioDatabase.SQLiteTable<ProjectsTable, ProjectInfo> {
        public ProjectsTable(PortfolioDatabase database) : base(database, nameof(ProjectInfo)) { }

        public override ProjectInfo ConstructRow() => new ProjectInfo(this);
    }
    public class ProjectInfo : ProjectsTable.SQLiteRow {
        public override IEnumerable<IDbCell> Fields => new IDbCell[] { ProjectId, OrginizationId, UrlName, Name, Role, BriefText, OverviewMarkdown, IsPublished };
        public readonly DbPrimaryCell ProjectId = new DbPrimaryCell(nameof(ProjectId));
        public override bool IsInDb => ProjectId.Value > 0;
        public readonly DbForeignCell<long> OrginizationId;
        public readonly DbCell<string> UrlName = new DbCell<string>(nameof(UrlName), DbType.String, constraints: DbCellFlags.NotNull);
        public readonly DbCell<string> Name = new DbCell<string>(nameof(Name), DbType.String, constraints: DbCellFlags.NotNull);
        public readonly DbCell<string> Role = new DbCell<string>(nameof(Role), DbType.String, constraints: DbCellFlags.NotNull);
        public readonly DbCell<string> BriefText = new DbCell<string>(nameof(BriefText), DbType.String, constraints: DbCellFlags.NotNull);
        public readonly DbCell<string> OverviewMarkdown = new DbCell<string>(nameof(OverviewMarkdown), DbType.String, constraints: DbCellFlags.NotNull);
        public readonly DbCell<bool> IsPublished = new DbCell<bool>(nameof(IsPublished), DbType.Boolean, false, DbCellFlags.NotNull);

        public ProjectInfo(ProjectsTable table) : base(table) {
            OrganizationTable orgsTable = table.Database.OrganizationTable;
            OrginizationId = new DbForeignCell<long>(nameof(OrginizationId), orgsTable, orgsTable.Schema.OrginizationId);
        }
    }
}