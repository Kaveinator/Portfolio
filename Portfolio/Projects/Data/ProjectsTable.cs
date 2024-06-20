using System.Data;
using System.Data.SQLite;
using ExperimentalSQLite;
using Portfolio.Orgs.Data;
using WebServer.Models;

namespace Portfolio.Projects.Data {
    public class ProjectsTable : PortfolioDatabase.SQLiteTable<ProjectsTable, ProjectInfo> {
        public ProjectsTable(PortfolioDatabase database) : base(database, nameof(ProjectInfo)) { }

        public override ProjectInfo ConstructRow() => new ProjectInfo(this);

        public IEnumerable<ProjectInfo> GetMostRecentProjects(byte limit = 4) {
            // Since the values are all ints, I won't use SQL parameters here
            if (limit == 0) return Array.Empty<ProjectInfo>();
            string cmdText =
                $"WITH RankedProjects AS (" +
                $"\n    SELECT *, ROW_NUMBER() OVER (" +
                $"\n        PARTITION BY {Schema.OrganizationId.ColumnName}" +
                $"\n        ORDER BY {Schema.StartTimestamp.ColumnName} DESC" +
                $"\n    ) AS rowNumInOrg" +
                $"\n    FROM ProjectInfo" +
                $"\n    WHERE IsPublished = 1" +
                $"\n)" +
                $"\nSELECT * FROM RankedProjects WHERE rowNumInOrg = 1" +
                $"\nORDER BY {Schema.StartTimestamp.ColumnName} DESC LIMIT @{nameof(limit)};";
            using (SQLiteCommand cmd = new(cmdText, Database.Connection)) {
                // The readFromReader function automatically reads all rows and returned an IEnumerable<ProjectInfo>
                cmd.Parameters.AddWithValue($"@{nameof(limit)}", limit);
                return ReadFromReader(cmd.ExecuteReader());
            }
        }
    }
    public class ProjectInfo : ProjectsTable.SQLiteRow, IPageModel {
        public override IEnumerable<IDbCell> Fields => new IDbCell[] { ProjectId, OrganizationId, UrlName, Name, Role, StartTimestamp, EndTimestamp, ThumbnailOverrideHref, HeaderOverrideHref, HeaderVerticalAnchorOverride, BriefText, OverviewMarkdown, IsPublished };
        //public Dictionary<string, object> Values => Fields.Omit(ProjectId, OrganizationId, IsPublished).ToDictionary(cell => cell.ColumnName, cell => cell.Value);
        public Dictionary<string, object> Values => new(Fields.Omit(ProjectId, OrganizationId, IsPublished).ToDictionary(cell => cell.ColumnName, cell => cell.Value)) {
            { "Duration", StartTimestamp.Value.ToDurationString(EndTimestamp.Value) }
        };

        public readonly DbPrimaryCell ProjectId = new DbPrimaryCell(nameof(ProjectId));
        public override bool IsInDb => ProjectId.Value > 0;
        public readonly DbForeignCell<long?, long> OrganizationId;
        public readonly DbStringCell UrlName = new DbStringCell(nameof(UrlName), constraints: DbCellFlags.NotNull, collation: StringCollation.NOCASE);
        public readonly DbCell<string> Name = new DbCell<string>(nameof(Name), DbType.String, constraints: DbCellFlags.NotNull);
        public readonly DbCell<string> Role = new DbCell<string>(nameof(Role), DbType.String, constraints: DbCellFlags.NotNull);
        public readonly DbDateTimeCell<DateTime> StartTimestamp = new DbDateTimeCell<DateTime>(nameof(StartTimestamp), constraints: DbCellFlags.NotNull);
        public readonly DbDateTimeCell<DateTime?> EndTimestamp = new DbDateTimeCell<DateTime?>(nameof(EndTimestamp));
        public readonly DbCell<string?> ThumbnailOverrideHref = new DbCell<string?>(nameof(ThumbnailOverrideHref), DbType.String, null, DbCellFlags.None);
        public readonly DbCell<string?> HeaderOverrideHref = new DbCell<string?>(nameof(HeaderOverrideHref), DbType.String, null, DbCellFlags.None);
        public readonly DbCell<byte?> HeaderVerticalAnchorOverride = new DbCell<byte?>(nameof(HeaderVerticalAnchorOverride), DbType.Byte);
        public readonly DbCell<string> BriefText = new DbCell<string>(nameof(BriefText), DbType.String, constraints: DbCellFlags.NotNull);
        public readonly DbCell<string> OverviewMarkdown = new DbCell<string>(nameof(OverviewMarkdown), DbType.String, constraints: DbCellFlags.NotNull);
        public readonly DbCell<bool> IsPublished = new DbCell<bool>(nameof(IsPublished), DbType.Boolean, false, DbCellFlags.NotNull);

        public ProjectInfo(ProjectsTable table) : base(table) {
            OrganizationTable orgsTable = table.Database.OrganizationTable;
            OrganizationId = new DbForeignCell<long?, long>(nameof(OrganizationId), orgsTable, orgsTable.Schema.OrganizationId);
        }
    }
}