using System.Data;
using ExperimentalSQLite;
using WebServer.Models;

namespace Portfolio.Projects {
    public class ProjectsTable : PortfolioDatabase.SQLiteTable<ProjectsTable, ProjectInfo> {
        public ProjectsTable(PortfolioDatabase database) : base(database, nameof(ProjectInfo)) { }

        public override ProjectInfo ConstructRow() => new ProjectInfo(this);
    }
    public class ProjectInfo : ProjectsTable.SQLiteRow, IPageModel {
        public override IEnumerable<IDbCell> Fields => new IDbCell[] { ProjectId, OrginizationId, UrlName, Name, Role, ThumbnailOverrideHref, HeaderOverrideHref, HeaderVerticalAnchorOverride, BriefText, OverviewMarkdown, IsPublished };
        public Dictionary<string, object> Values => Fields.Omit(ProjectId, OrginizationId, IsPublished).ToDictionary(cell => cell.ColumnName, cell => cell.Value);

        public readonly DbPrimaryCell ProjectId = new DbPrimaryCell(nameof(ProjectId));
        public override bool IsInDb => ProjectId.Value > 0;
        public readonly DbForeignCell<long?, long> OrginizationId;
        public readonly DbStringCell UrlName = new DbStringCell(nameof(UrlName), constraints: DbCellFlags.UniqueKey | DbCellFlags.NotNull, collation: StringCollation.NOCASE);
        public readonly DbCell<string> Name = new DbCell<string>(nameof(Name), DbType.String, constraints: DbCellFlags.NotNull);
        public readonly DbCell<string> Role = new DbCell<string>(nameof(Role), DbType.String, constraints: DbCellFlags.NotNull);
        public readonly DbCell<string?> ThumbnailOverrideHref = new DbCell<string?>(nameof(ThumbnailOverrideHref), DbType.String, null, DbCellFlags.None);
        public readonly DbCell<string?> HeaderOverrideHref = new DbCell<string?>(nameof(HeaderOverrideHref), DbType.String, null, DbCellFlags.None);
        public readonly DbCell<byte?> HeaderVerticalAnchorOverride = new DbCell<byte?>(nameof(HeaderVerticalAnchorOverride), DbType.Byte);
        public readonly DbCell<string> BriefText = new DbCell<string>(nameof(BriefText), DbType.String, constraints: DbCellFlags.NotNull);
        public readonly DbCell<string> OverviewMarkdown = new DbCell<string>(nameof(OverviewMarkdown), DbType.String, constraints: DbCellFlags.NotNull);
        public readonly DbCell<bool> IsPublished = new DbCell<bool>(nameof(IsPublished), DbType.Boolean, false, DbCellFlags.NotNull);

        public ProjectInfo(ProjectsTable table) : base(table) {
            OrganizationTable orgsTable = table.Database.OrganizationTable;
            OrginizationId = new DbForeignCell<long?, long>(nameof(OrginizationId), orgsTable, orgsTable.Schema.OrginizationId);
        }
    }
}