using System.Collections.Specialized;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Text;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using WebServer.Http;
using ExperimentalSQLite;
using WebServer.Models;

namespace Portfolio.TechAcademy {
    internal partial class TechAcademyDemoEndpoint { // ServerLocalTimeController.cs
        class CarInsuranceController {
            public const string StandardPath = "/orgs/TechAcademy/CarInsuranceMVC";
            static string TemplatesPath => StandardPath; // They are same literals, but this one binds to Internal Templates
            static string BindPath => StandardPath; // And this one to the request URIs
            public readonly TechAcademyDemoEndpoint Endpoint;
            static InsuranceEntities Table;

            public CarInsuranceController(TechAcademyDemoEndpoint endpoint) {
                Endpoint = endpoint;
                Table = Table ?? Endpoint.DemoDatabase.RegisterTable(
                    () => new InsuranceEntities(Endpoint.DemoDatabase),
                    () => new Insuree()
                );
                Endpoint.TryAddEventCallback(@$"^{BindPath}/demo", async _ => await OnView(ViewType.Simple));
                Endpoint.TryAddEventCallback(@$"^{BindPath}/compact", async _ => await OnView(ViewType.Compact));
                Endpoint.TryAddEventCallback(@$"^{BindPath}/create", OnCreate);
                Endpoint.TryAddEventCallback(@$"^{BindPath}/edit", OnEdit);
                Endpoint.TryAddEventCallback(@$"^{BindPath}/details", async req => await OnDetails(req, DetailsView.Details));
                Endpoint.TryAddEventCallback(@$"^{BindPath}/delete", async req => await OnDetails(req, DetailsView.Delete));
            }

            HttpResponse? RenderLayout(string title, object content) {
                int year = DateTime.Now.Year;
                if (!Endpoint.TryGetTemplate($"{StandardPath}/_Layout.html", out var result, out var statusModel, new LayoutModel(title, content)))
                    return Endpoint.GetGenericStatusPage(statusModel);
                return new HttpResponse() {
                    StatusCode = HttpStatusCode.OK,
                    AllowCaching = false,
                    ContentString = result,
                    MimeString = "text/html"
                };
            }

            enum ViewType { Simple, Compact }
            async Task<HttpResponse?> OnView(ViewType type) {
                StringBuilder rowsHtml = new StringBuilder();
                foreach (Insuree insuree in (await Table.GetAllAsync()).Where(i => !i.IsArchived.Value))
                    rowsHtml.Append(Endpoint.GetTemplate($"{TemplatesPath}/{type}.Row.Html", insuree));
                
                if (!Endpoint.TryGetTemplate(
                    $"{StandardPath}/{type}.html", out var content, out var statusModel,
                    new DetailsModel(this, $"{TemplatesPath}/{type}.Row.Html", (await Table.GetAllAsync()).Where(i => !i.IsArchived.Value))
                )) return Endpoint.GetGenericStatusPage(statusModel);
                return RenderLayout("Home", content);
            }
            public class DetailsModel : IPageModel {
                Dictionary<string, object> IPageModel.Values => new() {
                    { nameof(Rows), string.Join(string.Empty, Rows.Select(row => Controller.Endpoint.TryGetTemplate(RowModelPath, out var renderedHtml, out var _, row) ? renderedHtml : string.Empty)) }
                };
                readonly CarInsuranceController Controller;
                public IEnumerable<Insuree> Rows;
                readonly string RowModelPath;

                public DetailsModel(CarInsuranceController controller, string rowModelPath, IEnumerable<Insuree> rows) {
                    Controller = controller;
                    RowModelPath = rowModelPath;
                    Rows = rows;
                }
            }

            HttpResponse? OnCreate(HttpListenerRequest request) {
                string? content;
                if (request.HttpMethod.Equals(HttpMethod.Post.Method, StringComparison.InvariantCultureIgnoreCase)) {
                    // Parse querystring? content;
                    NameValueCollection formData;
                    using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                        formData = HttpUtility.ParseQueryString(reader.ReadToEnd());

                    string header, message;
                    if (!new Insuree(Table).TryParse(formData, out Insuree insuree)) {
                        return Endpoint.GetGenericStatusPage(new StatusPageModel(HttpStatusCode.BadRequest, 
                            subtitle: "The information that was submitted was invalid or empty"
                        ));
                    }
                    _ = insuree.Push();

                    // Redirect to details
                    return new HttpResponse() {
                        StatusCode = HttpStatusCode.Redirect,
                        ContentString = Endpoint.BuildUri(request, $"{BindPath}/Details?id={insuree.Id}").ToString()
                    };
                }
                else if (!Endpoint.TryGetTemplate($"{TemplatesPath}/Create.html", out content, out var statusModel))
                    return Endpoint.GetGenericStatusPage(statusModel);
                return RenderLayout("Home", content);
            }

            async Task<HttpResponse?> OnEdit(HttpListenerRequest request) {
                NameValueCollection query = HttpUtility.ParseQueryString(request.Url?.Query ?? "");
                Func<HttpResponse> genericResponse = () => Endpoint.GetGenericStatusPage(new StatusPageModel(HttpStatusCode.BadRequest,
                    subtitle: "The information that was submitted was invalid, empty or the row doesn't exist"
                ));
                long id; Insuree? insuree;
                if (!long.TryParse(query[nameof(id)] ?? string.Empty, out id)
                    || (insuree = await Table.Get(id)) is null
                    || insuree.IsArchived
                ) return genericResponse();

                if (request.HttpMethod.Equals(HttpMethod.Post.Method, StringComparison.InvariantCultureIgnoreCase)) {
                    NameValueCollection formData;
                    using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                        formData = HttpUtility.ParseQueryString(reader.ReadToEnd());
                    // Check if data is valid
                    if (!insuree.TryParse(formData, out insuree))
                        return genericResponse();
                    _ = insuree.Push(); // Save new data

                    // Redirect to details
                    return new HttpResponse() {
                        StatusCode = HttpStatusCode.Redirect,
                        ContentString = Endpoint.BuildUri(request, $"{BindPath}/Details?id={insuree.Id}").ToString()
                    };
                }

                return RenderLayout("Home", Endpoint.GetTemplate($"{TemplatesPath}/Edit.Html", insuree));
            }

            enum DetailsView { Details, Delete }
            async Task<HttpResponse?> OnDetails(HttpListenerRequest request, DetailsView view) {
                NameValueCollection query = HttpUtility.ParseQueryString(request.Url?.Query ?? "");
                Func<HttpResponse> genericResponse = () => Endpoint.GetGenericStatusPage(new StatusPageModel(HttpStatusCode.BadRequest,
                    subtitle: "The information that was submitted was invalid, empty or the row doesn't exist"
                ));
                long id; Insuree? insuree;
                if (!long.TryParse(query[nameof(id)] ?? string.Empty, out id)
                    || (insuree = await Table.Get(id)) is null
                    || insuree.IsArchived
                ) return genericResponse();
                if (view == DetailsView.Delete && request.HttpMethod.Equals(HttpMethod.Post.Method, StringComparison.InvariantCultureIgnoreCase)) {
                    await insuree.ArchiveAsync();
                    return new HttpResponse() {
                        StatusCode = HttpStatusCode.Redirect,
                        ContentString = Endpoint.BuildUri(request, $"{BindPath}/demo").ToString()
                    };
                }
                StringBuilder table = new StringBuilder();
                foreach (var field in insuree.Fields.Where(cell => cell != insuree.IsArchived)) {
                    table.Append(Endpoint.GetTemplate($"{TemplatesPath}/Details.Row.html", new DynamicPageModel()
                        .Add(nameof(field.ColumnName), field.ColumnName)
                        .Add(nameof(field.Value), field.Value)
                    ));
                }
                if (!Endpoint.TryGetTemplate($"{TemplatesPath}/{view}.html", out var content, out var statusModel, new DynamicPageModel()
                    .Add(nameof(insuree.Id), insuree.Id)
                    .Add(nameof(table), table)
                )) return Endpoint.GetGenericStatusPage(statusModel);
                return RenderLayout("Home", content);
            }

            public class InsuranceEntities : TechAcademyDemoDatabase.SQLiteTable<InsuranceEntities, Insuree> {
                public InsuranceEntities(TechAcademyDemoDatabase db) : base(db, nameof(InsuranceEntities)) { }

                public async Task<IEnumerable<Insuree>> GetAllAsync() {
                    var queue = new Queue<Insuree>();
                    using (SQLiteCommand cmd = new SQLiteCommand($"SELECT * FROM `{TableName}` WHERE `{Schema.IsArchived.ColumnName}` = 0;", Database.Connection)) {
                        var reader = await cmd.ExecuteReaderAsync();
                        while (await reader.ReadAsync()) {
                            Insuree insuree = (CachedRows.FirstOrDefault(r => r?.Id == Schema.SetIdFromReader(reader).Id, null)
                                ?? new Insuree(this)).Pull(reader);
                            queue.Enqueue(insuree);
                        }
                        await reader.DisposeAsync();
                    }
                    return queue;
                }

                public async Task<Insuree?> Get(long id) {
                    var dummy = new Insuree();
                    using (SQLiteCommand cmd = new SQLiteCommand($"SELECT * FROM `{TableName}` WHERE `{dummy.Id.ColumnName}` = {id};", Database.Connection)) {
                        var reader = await cmd.ExecuteReaderAsync();
                        while (reader.HasRows && await reader.ReadAsync()) {
                            Insuree insuree = (CachedRows.FirstOrDefault(r => r!.Id == id, null)
                                ?? new Insuree(this)).Pull(reader);
                            foreach (var field in insuree.Fields)
                                field.OnSaved();
                            return insuree;
                        }
                        await reader.DisposeAsync();
                    }
                    return null;
                }

                public override Insuree ConstructRow() => new Insuree(this);
            }
            public class Insuree : InsuranceEntities.SQLiteRow, IPageModel {
                public override IEnumerable<IDbCell> Fields => new IDbCell[] {
                    Id, FirstName, LastName, EmailAddress, DateOfBirth,
                    CarYear, CarMake, CarModel,
                    HasDUI, SpeedingTickets, IsFullCoverage, Quote,
                    IsArchived
                };
                public IEnumerable<IDbCell> NonPublicFields => new IDbCell[] { IsArchived };
                
                Dictionary<string, object> IPageModel.Values {
                    get {
                        Dictionary<string, object> result = new();
                        foreach (IDbCell cell in Fields) {
                            if (cell == IsArchived) continue;
                            object? value = null;
                            if (cell == DateOfBirth)
                                value = DateOfBirth.Value.ToString("yyyy-MM-dd");
                            result.Add(cell.ColumnName, value ?? cell.Value);
                        }
                        return result;
                        //return Fields.Where(cell => !NonPublicFields.Contains(cell)).ToDictionary(cell => cell.ColumnName, cell => cell == DateOfBirth ? DateOfBirth.Value.ToString("yyyy-MM-dd") : cell.Value);
                    }
                }

                public override bool IsInDb => 0 < Id;

                public readonly DbPrimaryCell    Id              = new DbPrimaryCell();
                public readonly DbCell<string>   FirstName       = new DbCell<string>   (nameof(FirstName),       DbType.String,  constraints: DbCellFlags.NotNull);
                public readonly DbCell<string>   LastName        = new DbCell<string>   (nameof(LastName),        DbType.String,  constraints: DbCellFlags.NotNull);
                public readonly DbCell<string>   EmailAddress    = new DbCell<string>   (nameof(EmailAddress),    DbType.String,  constraints: DbCellFlags.NotNull);
                public readonly DbCell<DateTime> DateOfBirth     = new DbCell<DateTime> (nameof(DateOfBirth),     DbType.Date,    constraints: DbCellFlags.NotNull);
                public readonly DbCell<ushort>   CarYear         = new DbCell<ushort>   (nameof(CarYear),         DbType.UInt16,  constraints: DbCellFlags.NotNull);
                public readonly DbCell<string>   CarMake         = new DbCell<string>   (nameof(CarMake),         DbType.String,  constraints: DbCellFlags.NotNull);
                public readonly DbCell<string>   CarModel        = new DbCell<string>   (nameof(CarModel),        DbType.String,  constraints: DbCellFlags.NotNull);
                public readonly DbCell<bool>     HasDUI          = new DbCell<bool>     (nameof(HasDUI),          DbType.Boolean, constraints: DbCellFlags.NotNull);
                public readonly DbCell<int>      SpeedingTickets = new DbCell<int>      (nameof(SpeedingTickets), DbType.Int32,   constraints: DbCellFlags.NotNull);
                public readonly DbCell<bool>     IsFullCoverage  = new DbCell<bool>     (nameof(IsFullCoverage),  DbType.Boolean, constraints: DbCellFlags.NotNull);
                public readonly DbCell<decimal>  Quote           = new DbCell<decimal>  (nameof(Quote),           DbType.Decimal, constraints: DbCellFlags.NotNull);
                public readonly DbCell<bool>     IsArchived      = new DbCell<bool>     (nameof(IsArchived),      DbType.Boolean, constraints: DbCellFlags.NotNull);

                /// <summary>Only call when you won't use this for data purposes</summary>
                public Insuree() : base(null!) { }

                /// <summary>Call to create this</summary>
                public Insuree(InsuranceEntities table, string firstName, string lastName, string email) : base(table) {
                    FirstName.Value = firstName;
                    LastName.Value = lastName;
                    EmailAddress.Value = email;
                }

                /// <summary>Used to create from reader, I didn't make pulling from DB yet</summary>
                public Insuree(InsuranceEntities table) : base(table) { }

                public Insuree SetIdFromReader(DbDataReader reader) {
                    Id.Value = reader.GetInt64(reader.GetOrdinal(Id.ColumnName));
                    return this;
                }

                public Insuree Pull(DbDataReader reader) {
                    long id = reader.GetInt64(reader.GetOrdinal(Id.ColumnName));
                    if (IsInDb && Id.Value != id)
                        throw new InvalidOperationException($"Attempting to pull data from Database, but there is an row Id mismatch! [ Local: {Id.Value}; Incomming: {id};]");
                    else Id.Value = Id.Value = id;
                    FirstName.Value = reader.GetString(reader.GetOrdinal(FirstName.ColumnName));
                    LastName.Value = reader.GetString(reader.GetOrdinal(LastName.ColumnName));
                    EmailAddress.Value = reader.GetString(reader.GetOrdinal(EmailAddress.ColumnName));

                    FirstName.Value = reader.GetString(reader.GetOrdinal(FirstName.ColumnName));
                    LastName.Value = reader.GetString(reader.GetOrdinal(LastName.ColumnName));
                    EmailAddress.Value = reader.GetString(reader.GetOrdinal(EmailAddress.ColumnName));
                    DateOfBirth.Value = reader.GetDateTime(reader.GetOrdinal(DateOfBirth.ColumnName));
                    if (ushort.TryParse(reader.GetValue(reader.GetOrdinal(CarYear.ColumnName)).ToString(), out CarYear.Value))
                        { }// CarYear.TemporaryForceSetCachedValue = CarYear.Value;
                    CarMake.Value = reader.GetString(reader.GetOrdinal(CarMake.ColumnName));
                    CarModel.Value = reader.GetString(reader.GetOrdinal(CarModel.ColumnName));
                    HasDUI.Value = reader.GetBoolean(reader.GetOrdinal(HasDUI.ColumnName));
                    SpeedingTickets.Value = reader.GetInt16(reader.GetOrdinal(SpeedingTickets.ColumnName));
                    IsFullCoverage.Value = reader.GetBoolean(reader.GetOrdinal(IsFullCoverage.ColumnName));
                    Quote.Value = reader.GetDecimal(reader.GetOrdinal(Quote.ColumnName));
                    IsArchived.Value = reader.GetBoolean(reader.GetOrdinal(IsArchived.ColumnName));
                    return this;
                }

                static Regex EmailValidator = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
                public bool TryParse(NameValueCollection reader, out Insuree insuree) {
                    bool validData = (FirstName.Value = reader[nameof(FirstName)] ?? string.Empty).Length > 1;
                    validData &= (LastName.Value = reader[nameof(LastName)] ?? string.Empty).Length > 1;
                    validData &= EmailValidator.IsMatch(EmailAddress.Value = reader[nameof(EmailAddress)] ?? string.Empty);
                    validData &= DateTime.TryParse(reader[nameof(DateOfBirth)] ?? string.Empty, out DateOfBirth.Value);
                    
                    validData &= ushort.TryParse(reader[nameof(CarYear)] ?? string.Empty, out CarYear.Value);
                    validData &= (CarMake.Value = reader[nameof(CarMake)] ?? string.Empty).Length >= 2;
                    validData &= (CarModel.Value = reader[nameof(CarModel)] ?? string.Empty).Length >= 2;


                    HasDUI.Value = reader.AllKeys.Contains(nameof(HasDUI));
                    validData &= int.TryParse(reader[nameof(SpeedingTickets)] ?? string.Empty, out SpeedingTickets.Value);
                    IsFullCoverage.Value = reader.AllKeys.Contains(nameof(IsFullCoverage));

                    insuree = validData ? ComputeQuote() : this;
                    return validData;
                }

                public Insuree ComputeQuote() {
                    Quote.Value = 50;// Req 517.1a: Base 50/m
                    int age = DateTime.Now.Year - DateOfBirth.Value.Year;
                    Quote.Value += age <= 18 ? 100 // Req 517.1b: 18 and under get +100/m
                        : age <= 25 ? 50 : 25; // Req 517.1c, 517.1d: 19-25 get +50/m, 26+ get +25/m
                    Quote.Value += CarYear.Value < 2000 || 2015 < CarYear.Value // Req 517.1e, 517.1f: Add 25/m if older than 2000 and newer than 2015
                        ? 25 : 0;
                    // Req 517.1g, 517.1h: +25/m for Porshe make, +50/m for Porshe 911 Carrera
                    Quote.Value += CarMake.Value.Equals("Porsche", StringComparison.CurrentCultureIgnoreCase)
                        ? CarModel.Value.Equals("911 Carrera", StringComparison.CurrentCultureIgnoreCase)
                        ? 50 : 25 : 0;
                    Quote.Value += Math.Max(SpeedingTickets.Value, 0) * 10; // Req 517.1i: +10/m per ticket
                    decimal multiplier = 1;
                    Quote.Value *= HasDUI.Value ? 1.25m : 1m; // Req 517.1j: if DUI, add 25%
                    Quote.Value *= IsFullCoverage.Value ? 1.50m : 1m; // Req 517.1j: if Full Coverage, add 50%
                    Quote.Value *= multiplier;
                    return this;
                }

                public async Task ArchiveAsync() {
                    if (!IsInDb) return;
                    IsArchived.Value = true;
                    Push();
                    /*
                    using (SQLiteCommand cmd = new SQLiteCommand($"DELETE FROM `{Table.TableName}` WHERE `{Id.ColumnName}` = {Id.Value};", Database.Connection))
                        _ = await cmd.ExecuteNonQueryAsync();
                    Id.Value = -1;*/
                }
            }
            
            class LayoutModel : IPageModel {
                public Dictionary<string, object> Values => new() {
                    { nameof(Title), Title },
                    { nameof(Year), Year },
                    { nameof(Content), Content }
                };

                public string Title;
                public int Year => DateTime.Now.Year;
                public object Content;

                public LayoutModel(string title, object content) {
                    Title = title;
                    Content = content;
                }
            }
        }
    }
}