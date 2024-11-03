using System.Data;
using System.Data.SQLite;
using ExperimentalSQLite;
using Portfolio.Projects.Data;
using WebServer.Models;

namespace Portfolio.Orgs.Data {
    public class OrganizationTable : PortfolioDatabase.SQLiteTable<OrganizationTable, OrganizationInfo> {
        public OrganizationTable(PortfolioDatabase database) : base(database, nameof(OrganizationInfo)) { }

        public override OrganizationInfo ConstructRow() => new OrganizationInfo(this);

        public IEnumerable<OrganizationInfo> GetOrgInfoFromProjectInfo(IEnumerable<ProjectInfo> projInfo, bool ensureOrgPublised) {
            // Since the values are all ints, I won't use SQL parameters here
            if (!projInfo.Any()) return Array.Empty<OrganizationInfo>();
            projInfo = projInfo.Where(projInfo => projInfo.OrganizationId.Value != null);
            string whereClause = $"({string.Join(" OR ", projInfo.Select(techItem => $"`{Schema.OrganizationId.ColumnName}` = {techItem.OrganizationId.Value}"))})" +
                $" AND `{Schema.IsPublished.ColumnName}` = {true};";
            using (SQLiteCommand cmd = new($"SELECT * FROM `{TableName}` WHERE {whereClause};", Database.Connection)) {
                return ReadFromReader(cmd.ExecuteReader());
            }
        }

        public OrganizationInfo? GetOrgFromProject(ProjectInfo project) {
            long? orgId = project?.OrganizationId.Value;
            if (orgId == null) return null;
            return Query(1,
                new WhereClause<long>(Schema.OrganizationId, "=", orgId.Value)
            ).FirstOrDefault(defaultValue: null);
        }
    }
    public class OrganizationInfo : OrganizationTable.SQLiteRow, IDataModel {
        public override IEnumerable<IDbCell> Fields => new IDbCell[] { OrganizationId, UrlName, Name, Role, StartTimestamp, EndTimestamp, BriefText, OverviewSubheaderOverride, OverviewMarkdown, IsPublished };
        public Dictionary<string, object> Values => new(Fields.Omit(OrganizationId, IsPublished).ToDictionary(cell => cell.ColumnName, cell => cell.Value)) {
            { "Duration", StartTimestamp.Value.ToDurationString(EndTimestamp.Value) }
        };
        public readonly DbPrimaryCell OrganizationId = new DbPrimaryCell(nameof(OrganizationId));
        public override bool IsInDb => OrganizationId.Value > 0;
        public readonly DbStringCell UrlName = new DbStringCell(nameof(UrlName), constraints: DbCellFlags.UniqueKey | DbCellFlags.NotNull, collation: StringCollation.NOCASE);
        public readonly DbCell<string> Name = new DbCell<string>(nameof(Name), DbType.String, constraints: DbCellFlags.NotNull);
        //public readonly DbCell<string> FullName = new DbCell<string>(nameof(FullName), DbType.String, constraints: DbCellFlags.UniqueKey);
        public readonly DbDateTimeCell<DateTime> StartTimestamp = new DbDateTimeCell<DateTime>(nameof(StartTimestamp), constraints: DbCellFlags.NotNull);
        public readonly DbDateTimeCell<DateTime?> EndTimestamp = new DbDateTimeCell<DateTime?>(nameof(EndTimestamp));
        public readonly DbCell<string> Role = new DbCell<string>(nameof(Role), DbType.String, constraints: DbCellFlags.NotNull);
        public readonly DbCell<string> BriefText = new DbCell<string>(nameof(BriefText), DbType.String, constraints: DbCellFlags.NotNull);
        public readonly DbCell<string?> OverviewSubheaderOverride = new DbCell<string?>(nameof(OverviewSubheaderOverride), DbType.String);
        public readonly DbCell<string> OverviewMarkdown = new DbCell<string>(nameof(OverviewMarkdown), DbType.String, constraints: DbCellFlags.NotNull);
        public readonly DbCell<bool> IsPublished = new DbCell<bool>(nameof(IsPublished), DbType.Boolean, false, DbCellFlags.NotNull);

        public OrganizationInfo(OrganizationTable table) : base(table) { }
    }
}