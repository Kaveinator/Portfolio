using ExperimentalSQLite;
using Portfolio.Projects;

namespace Portfolio.Orgs.Data
{
    public class OrganizationLinksTable : AbstractLinksTable<OrganizationLinksTable, OrganizationLinkInfo>
    {
        public OrganizationLinksTable(PortfolioDatabase db) : base(db, nameof(OrganizationLinkInfo)) { }

        public override OrganizationLinkInfo ConstructRow() => new OrganizationLinkInfo(this);
    }

    public class OrganizationLinkInfo : OrganizationLinksTable.Row
    {
        public override IEnumerable<IDbCell> Fields => base.Fields.Include(OrganizationId);
        public readonly DbForeignCell<long> OrganizationId;
        public OrganizationLinkInfo(OrganizationLinksTable table) : base(table)
        {
            OrganizationTable orgsTable = table.Database.OrganizationTable;
            OrganizationId = new DbForeignCell<long>(nameof(OrganizationId), orgsTable, orgsTable.Schema.OrganizationId, constraints: DbCellFlags.NotNull);
        }
    }
}