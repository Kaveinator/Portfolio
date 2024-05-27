using System.Collections.Specialized;
using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;
using System.Data.SQLite;
using System.Net;
using System.Web;
using WebServer.Http;
using ExperimentalSQLite;
using WebServer.Models;

namespace Portfolio.TechAcademy {
    internal partial class TechAcademyDemoEndpoint { // ServerLocalTimeController.cs
        class NewsLetterController {
            static NewsletterSubscriptionTable Table;
            public const string TemplatesPath = "/orgs/TechAcademy/NewsLetterMVC/";
            public readonly TechAcademyDemoEndpoint Endpoint;

            public NewsLetterController(TechAcademyDemoEndpoint endpoint) {
                Endpoint = endpoint;
                Endpoint.AddEventCallback("/orgs/TechAcademy/NewsLetterMVC/demo", OnDemoRequest);
                Endpoint.AddEventCallback("/orgs/TechAcademy/NewsLetterMVC/Admin", OnAdminRequest);
                Endpoint.AddEventCallback("/orgs/TechAcademy/NewsLetterMVC/Unsubscribe", OnDeleteRequest);
                Table = Table ?? Endpoint.DemoDatabase.RegisterTable(
                    () => new NewsletterSubscriptionTable(Endpoint.DemoDatabase),
                    () => new NewsletterSubscription()
                );
            }

            static Regex EmailValidator = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
            HttpResponse? OnDemoRequest(HttpListenerRequest request) {
                string? content;
                StatusPageModel statusPageModel;
                if (request.HttpMethod.Equals(HttpMethod.Post.Method, StringComparison.InvariantCultureIgnoreCase)) {
                    // Parse querystring? content;
                    NameValueCollection query;
                    using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                        query = HttpUtility.ParseQueryString(reader.ReadToEnd());

                    string? firstName = query[nameof(firstName)],
                        lastName = query[nameof(lastName)],
                        emailAddress = query[nameof(emailAddress)];

                    bool inputValid = !string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName) && EmailValidator.IsMatch(emailAddress ?? string.Empty);

                    string header;
                    string message;
                    if (inputValid) {
                        NewsletterSubscription sub = new NewsletterSubscription(Table, firstName!, lastName!, emailAddress!).Push();
                        header = "Success!";
                        message = $"<span class=\"text-success\">The info has been pushed, with id {sub.Id}. <a href=\"./Admin\">View Data</a></span>";
                    } else {
                        header = "Bad Request";
                        message = "<span class=\"text-danger\">Form had fields that were invalid and/or empty. <a href=\"./demo\">Go back</a></span>";
                    }
                    content = Endpoint.TryGetTemplate($"{TemplatesPath}/Home.Result.html", out string _content, out var _, new DynamicPageModel()
                        .Add(nameof(header), header)
                        .Add(nameof(message), message)
                    ) ? _content : $"{header} - {message}";
                }
                else if (!Endpoint.TryGetTemplate($"{TemplatesPath}/Home.html", out content, out statusPageModel))
                    return Endpoint.GetGenericStatusPage(statusPageModel);
                
                if (!Endpoint.TryGetTemplate($"{TemplatesPath}/Layout.html", out var result, out statusPageModel, new DynamicPageModel()
                    .Add(nameof(content), content)
                )) return Endpoint.GetGenericStatusPage(statusPageModel);

                return new HttpResponse() {
                    StatusCode = HttpStatusCode.OK,
                    AllowCaching = false,
                    ContentString = result,
                    MimeString = "text/html"
                };
            }

            async Task<HttpResponse?> OnAdminRequest(HttpListenerRequest request) {
                string content = "";
                // Get all content
                var subs = (await Table.GetAllAsync()).Where(sub => !sub.IsDeleted.Value);
                foreach (NewsletterSubscription sub in subs) {
                    content += Endpoint.GetTemplate($"{TemplatesPath}/Admin.Row.html", sub);
                }
                if (!Endpoint.TryGetTemplate($"{TemplatesPath}/Admin.html", out content, out var statusPageModel, new DynamicPageModel()
                    .Add(nameof(content), content)
                )) return Endpoint.GetGenericStatusPage(statusPageModel);

                if (!Endpoint.TryGetTemplate($"{TemplatesPath}/Layout.html", out var result, out statusPageModel, new DynamicPageModel()
                    .Add(nameof(content), content)
                )) return Endpoint.GetGenericStatusPage(statusPageModel);
                
                return new HttpResponse() {
                    StatusCode = HttpStatusCode.OK,
                    AllowCaching = false,
                    ContentString = result,
                    MimeString = "text/html"
                };
            }

            async Task<HttpResponse?> OnDeleteRequest(HttpListenerRequest request) {
                if (request.HttpMethod.Equals(HttpMethod.Post.Method, StringComparison.InvariantCultureIgnoreCase)) {
                    // Parse querystring? content;
                    NameValueCollection query;
                    using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                        query = HttpUtility.ParseQueryString(reader.ReadToEnd());

                    string id = query[nameof(id)] ?? string.Empty;
                    if (!long.TryParse(id, out long idValue)) return Endpoint.GetGenericStatusPage(new StatusPageModel(HttpStatusCode.BadRequest,
                        subtitle: "Failed to parse Id"
                    ));
                    NewsletterSubscription? sub = await Table.Get(idValue);
                    if (sub == null) return Endpoint.GetGenericStatusPage(new StatusPageModel(HttpStatusCode.NotFound,
                        subtitle: $"Row with Id of {idValue} was not found"
                    ));
                    await sub.DeleteAsync();
                }
                return new HttpResponse() {
                    StatusCode = HttpStatusCode.Redirect,
                    ContentString = Endpoint.BuildUri(request, "/orgs/TechAcademy/NewsletterMVC/Admin").ToString()
                };
            }

            public class NewsletterSubscriptionTable : TechAcademyDemoDatabase.SQLiteTable<NewsletterSubscriptionTable, NewsletterSubscription> {
                public NewsletterSubscriptionTable(TechAcademyDemoDatabase db) : base(db, nameof(NewsletterSubscriptionTable)) { }

                public async Task<IEnumerable<NewsletterSubscription>> GetAllAsync() {
                    Queue<NewsletterSubscription> queue = new Queue<NewsletterSubscription>();
                    var dummy = new NewsletterSubscription();
                    using (SQLiteCommand cmd = new SQLiteCommand($"SELECT * FROM `{TableName}` WHERE `{dummy.IsDeleted.ColumnName}` = {0};", Database.Connection)) {
                        var reader = await cmd.ExecuteReaderAsync();
                        while (await reader.ReadAsync()) {
                            NewsletterSubscription sub = (CachedRows.FirstOrDefault(r => r.Id == dummy.HeaderFromReader(reader).Id, null)
                                ?? new NewsletterSubscription(this)).Pull(reader);
                            queue.Enqueue(sub);
                        }
                        await reader.DisposeAsync();
                    }
                    return queue;
                }

                public async Task<NewsletterSubscription?> Get(long id) {
                    var dummy = new NewsletterSubscription();
                    using (SQLiteCommand cmd = new SQLiteCommand($"SELECT * FROM `{TableName}` WHERE `{dummy.Id.ColumnName}` = {id};", Database.Connection)) {
                        var reader = await cmd.ExecuteReaderAsync();
                        while (reader.HasRows && await reader.ReadAsync()) {
                            return (CachedRows.FirstOrDefault(r => r!.Id == id, null)
                                ?? new NewsletterSubscription(this)).Pull(reader);
                        }
                        await reader.DisposeAsync();
                    }
                    return null;
                }
            }
            public class NewsletterSubscription : NewsletterSubscriptionTable.SQLiteRow, IPageModel {
                public override IEnumerable<IDbCell> Fields => new IDbCell[] { Id, FirstName, LastName, EmailAddress, IsDeleted };
                Dictionary<string, object> IPageModel.Values => Fields.Where(cell => cell != IsDeleted).ToDictionary(cell => cell.ColumnName, cell => cell.Value);
                public readonly DbPrimaryCell Id = new DbPrimaryCell();
                public readonly DbCell<string> FirstName = new DbCell<string>(nameof(FirstName), DbType.String, constraints: DbCellFlags.NotNull);
                public readonly DbCell<string> LastName = new DbCell<string>(nameof(LastName), DbType.String, constraints: DbCellFlags.NotNull);
                public readonly DbCell<string> EmailAddress = new DbCell<string>(nameof(EmailAddress), DbType.String, constraints: DbCellFlags.NotNull);
                public readonly DbCell<bool> IsDeleted = new DbCell<bool>(nameof(IsDeleted), DbType.Boolean, false, constraints: DbCellFlags.NotNull);
                public override bool IsInDb => Id != -1L;

                /// <summary>Only call when you won't use this for data purposes</summary>
                public NewsletterSubscription() : base(null) { }

                /// <summary>Call to create this</summary>
                public NewsletterSubscription(NewsletterSubscriptionTable table, string firstName, string lastName, string email) : base(table) {
                    FirstName.Value = firstName;
                    LastName.Value = lastName;
                    EmailAddress.Value = email;
                }

                /// <summary>Used to create from reader, I didn't make pulling from DB yet</summary>
                public NewsletterSubscription(NewsletterSubscriptionTable table) : base(table) { }

                public NewsletterSubscription HeaderFromReader (DbDataReader reader) {
                    Id.Value = reader.GetInt64(reader.GetOrdinal(Id.ColumnName));
                    return this;
                }

                public NewsletterSubscription Pull(DbDataReader reader) {
                    long id = reader.GetInt64(reader.GetOrdinal(Id.ColumnName));
                    if (IsInDb && Id.Value != id)
                        throw new InvalidOperationException($"Attempting to pull data from Database, but there is an row Id mismatch! [ Local: {Id.Value}; Incomming: {id};]");
                    else Id.Value = id;
                    FirstName.Value = reader.GetString(reader.GetOrdinal(FirstName.ColumnName));
                    LastName.Value = reader.GetString(reader.GetOrdinal(LastName.ColumnName));
                    EmailAddress.Value = reader.GetString(reader.GetOrdinal(EmailAddress.ColumnName));
                    return this;
                }

                public async Task DeleteAsync() {
                    if (!IsInDb) return;
                    IsDeleted.Value = true;
                    Push();
                    /*
                    using (SQLiteCommand cmd = new SQLiteCommand($"DELETE FROM `{Table.TableName}` WHERE `{Id.ColumnName}` = {Id.Value};", Database.Connection))
                        _ = await cmd.ExecuteNonQueryAsync();
                    Id.Value = -1;*/
                }
            }
        }
    }
}