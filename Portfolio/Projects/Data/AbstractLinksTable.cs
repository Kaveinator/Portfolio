using System.Data;
using ExperimentalSQLite;
using Markdig.Parsers;
using WebServer.Models;

namespace Portfolio.Projects.Data {
    public abstract class AbstractLinksTable<TTable, TRow> : PortfolioDatabase.SQLiteTable<TTable, TRow>
        where TTable : AbstractLinksTable<TTable, TRow>
        where TRow : AbstractLinksTable<TTable, TRow>.Row {
        protected AbstractLinksTable(PortfolioDatabase db, string tableName) : base(db, tableName) { }

        public class Row : SQLiteRow, IPageComponentModel {
            public override IEnumerable<IDbCell> Fields => new IDbCell[] { LinkId, IconOverride, LinkTextOverride, Href, IsPublished };
            public Dictionary<string, object> Values => Fields.Omit(LinkId, IsPublished).ToDictionary(field => field.ColumnName, field => field.Value);
            public override bool IsInDb => 0 < LinkId;
            public readonly DbPrimaryCell LinkId = new DbPrimaryCell(nameof(LinkId));
            public readonly DbCell<string?> IconOverride = new DbCell<string>(nameof(IconOverride), DbType.String);
            public readonly DbCell<string?> LinkTextOverride = new DbCell<string>(nameof(LinkTextOverride), DbType.String);
            public readonly DbCell<string> Href = new DbCell<string>(nameof(Href), DbType.String, constraints: DbCellFlags.NotNull);
            //public readonly DbCell<bool> IsLiveDemo = new DbCell<bool>(nameof(IsLiveDemo), DbType.Boolean, false, DbCellFlags.NotNull);
            public readonly DbCell<bool> IsPublished = new DbCell<bool>(nameof(IsPublished), DbType.Boolean, false, DbCellFlags.NotNull);

            public Row(TTable table) : base(table) { }

            public virtual string Render() {
                if (!Uri.TryCreate(Href, UriKind.RelativeOrAbsolute, out Uri url))
                    return string.Empty;
                string html = $"<a href=\"{url}\" target=\"_blank\"";
                if (IconOverride.Value != null)
                    html += $" style=\"--icon: '{IconOverride.Value}';\"";
                html += $">{(LinkTextOverride.Value != null ? LinkTextOverride.Value : $"{(url.Host + url.LocalPath).TrimEnd('/')}")}</a>";
                return html;
            }
        }
    }
}