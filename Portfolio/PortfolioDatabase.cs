using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExperimentalSQLite;
using Portfolio.Projects;

namespace Portfolio {
    public partial class PortfolioDatabase : SQLiteDatabase<PortfolioDatabase> {
        public static PortfolioDatabase? Instance { get; private set; }
        public static PortfolioDatabase GetOrCreate() => Instance = Instance ?? new PortfolioDatabase();

        public static EventLogger Logger = EventLogger.GetOrCreate<PortfolioDatabase>();
        public readonly ContactTable ContactInfoTable;

        #region Project/Orginizations
        public readonly OrganizationTable OrganizationTable;
        public readonly ProjectsTable ProjectsTable;

        public readonly TechnologiesTable TechnologiesTable;
        public readonly TechnologiesUsedTable TechnologiesUsedTable;

        public readonly ProjectLinksTable ProjectLinksTable;
        public readonly OrganizationLinksTable OrganizationLinksTable;
        #endregion

        protected override void OnLog(SQLog log) => Logger.Log(log.Message);

        protected PortfolioDatabase() : base($"Data/{nameof(PortfolioDatabase)}") {
            OpenAsync().Wait();
            ContactInfoTable = RegisterTable<ContactTable, ContactInfo>(() => new ContactTable(this));
            OrganizationTable = RegisterTable<OrganizationTable, OrginizationInfo>(() => new OrganizationTable(this));
            ProjectsTable = RegisterTable<ProjectsTable, ProjectInfo>(() => new ProjectsTable(this));

            TechnologiesTable = RegisterTable<TechnologiesTable, TechnologyInfo>(() => new TechnologiesTable(this));
            TechnologiesUsedTable = RegisterTable<TechnologiesUsedTable, TechnologyUsedInfo>(() => new TechnologiesUsedTable(this));

            ProjectLinksTable = RegisterTable<ProjectLinksTable, ProjectLinkInfo>(() => new ProjectLinksTable(this));
            OrganizationLinksTable = RegisterTable<OrganizationLinksTable, OrganizationLinkInfo>(() => new OrganizationLinksTable(this));
            Logger.Log("Initilized Database");
        }
    }
    public class ContactTable : PortfolioDatabase.SQLiteTable<ContactTable, ContactInfo> {
        public ContactTable(PortfolioDatabase db) : base(db, nameof(ContactInfo)) { }

        public override ContactInfo ConstructRow() => new ContactInfo(this);
    }
    public class ContactInfo : ContactTable.SQLiteRow {
        public override IEnumerable<IDbCell> Fields => new IDbCell[] { Id, Timestamp, Name, Email, Subject, Message };
        public readonly DbPrimaryCell Id = new DbPrimaryCell();
        public readonly DbCell<DateTime> Timestamp = new DbCell<DateTime>(nameof(Timestamp), DbType.DateTime, DateTime.Now, constraints: DbCellFlags.NotNull);
        public readonly DbCell<string> Name = new DbCell<string>(nameof(Name), DbType.String, constraints: DbCellFlags.NotNull);
        public readonly DbCell<string> Email = new DbCell<string>(nameof(Email), DbType.String, constraints: DbCellFlags.NotNull);
        public readonly DbCell<string> Subject = new DbCell<string>(nameof(Subject), DbType.String, constraints: DbCellFlags.NotNull);
        public readonly DbCell<string> Message = new DbCell<string>(nameof(Message), DbType.String, constraints: DbCellFlags.NotNull);
        public override bool IsInDb => Id != -1L;

        public ContactInfo(ContactTable table) : base(table) {}

        /// <summary>Call to create this</summary>
        public ContactInfo(ContactTable table, string name, string email, string subject, string message) : base(table) {
            Name.Value = name;
            Email.Value = email;
            Subject.Value = subject;
            Message.Value = message;
        }
    }
}
