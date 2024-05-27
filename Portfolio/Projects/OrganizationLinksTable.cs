namespace Portfolio.Projects {
    public class OrganizationLinksTable : AbstractLinksTable<OrganizationLinksTable, OrganizationLinkInfo> {
        public OrganizationLinksTable(PortfolioDatabase db) : base(db, nameof(OrganizationLinkInfo)) { }

        public override OrganizationLinkInfo ConstructRow() => new OrganizationLinkInfo(this);
    }
    
    public class OrganizationLinkInfo : OrganizationLinksTable.Row {
        public OrganizationLinkInfo(OrganizationLinksTable table) : base(table) { }
    }
}