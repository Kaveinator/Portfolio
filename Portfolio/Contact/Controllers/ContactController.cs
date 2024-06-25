using System.Net;
using System.Web;
using System.Collections.Specialized;
using WebServer.Http;
using SimpleJSON;
using System.Text.RegularExpressions;
using System.Text;
using WebServer.Models;

namespace Portfolio.Contact.Controllers {
    public class ReCaptchaConfig : IDataModel {
        public static ReCaptchaConfig? Instance { get; private set; }
        public static ReCaptchaConfig GetOrCreate() => Instance ?? (Instance = new ReCaptchaConfig());

        readonly JSONFile ConfigFile = Properties.GetOrCreate<ReCaptchaConfig>();
        Dictionary<string, object> IDataModel.Values => new Dictionary<string, object>() {
            { nameof(SiteKey), SiteKey }
        };
        public string? SecretKey => ConfigFile.GetValueOrDefault(nameof(SecretKey), null);
        public string? SiteKey => ConfigFile.GetValueOrDefault(nameof(SiteKey), null);
    }
    public class ContactController {
        public readonly PortfolioEndpoint Endpoint;
        static ReCaptchaConfig Config = ReCaptchaConfig.GetOrCreate();
        public ContactController(PortfolioEndpoint endpoint) {
            Endpoint = endpoint;
            Endpoint.TryAddEventCallback(@"^/contact$", OnContactRequest);
        }

        static Regex EmailValidator = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
        async Task<HttpResponse?> OnContactRequest(HttpListenerRequest request) {
            if (!request.HttpMethod.Equals(HttpMethod.Post.Method, StringComparison.InvariantCultureIgnoreCase)) {
                if (!Endpoint.TryGetTemplate("/Contact.html", out string content, out StatusPageModel statusModel, Config))
                    return Endpoint.GetGenericStatusPage(statusModel);
                return new HttpResponse(HttpStatusCode.OK, content, "text/html", true);
            }
            // Parse query
            NameValueCollection query;
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                query = HttpUtility.ParseQueryString(reader.ReadToEnd());

            // Form Fields
            JSONObject jsonResponse = new JSONObject();
            bool success;
            string errorMessage = default!;
            string name = query[nameof(name)]?.Trim() ?? string.Empty;
            string? email = query[nameof(email)]?.Trim();
            string subject = query[nameof(subject)]?.Trim() ?? string.Empty;
            string message = query[nameof(message)]?.Trim() ?? string.Empty;
            string? recaptchaToken = query[nameof(recaptchaToken)];
            string remoteIp = request.Headers["CF-Connecting-IP"] ?? request.RemoteEndPoint.Address.ToString();
            bool? captchaValid = null;
            if (string.IsNullOrEmpty(recaptchaToken)) {
                success = false;
                errorMessage = "Captcha Token Missing!";
                goto shipResponse;
            }
            if (name.Length < 2) {
                success = false;
                errorMessage = "Name is too short!";
                goto shipResponse;
            }
            if (subject.Length < 2) {
                success = false;
                errorMessage = "Subject is too short!";
                goto shipResponse;
            }
            if (message.Length < 10) {
                success = false;
                errorMessage = "Message is too short!";
                goto shipResponse;
            }
            if (!string.IsNullOrEmpty(email) && !EmailValidator.IsMatch(email)) {
                success = false;
                errorMessage = "Double check your email field!";
                goto shipResponse;
            }
            captchaValid = await IsCaptchaValid(recaptchaToken, remoteIp);
            /*if (!captchaValid.Value) {
                success = false;
                errorMessage = "Captcha Validation Failed!";
                goto shipResponse;
            }*/

            success = true;
            var info = new ContactInfo(Endpoint.Database.ContactInfoTable, name, email ?? "", subject, message, captchaValid.Value).Push();
        shipResponse:
            jsonResponse.Add(nameof(success), success);
            jsonResponse.Add(nameof(errorMessage), errorMessage);
#if DEBUG
            if (Program.Mode == Mode.Development) {
                JSONNode dump = new JSONObject();
                dump.Add(nameof(name), name);
                dump.Add(nameof(email), email!);
                dump.Add(nameof(message), message);
                dump.Add(nameof(recaptchaToken), recaptchaToken!);
                dump.Add(nameof(remoteIp), remoteIp);
                dump.Add(nameof(captchaValid), captchaValid);
                jsonResponse.Add(nameof(dump), dump);
            }
#endif
            return new HttpResponse() {
                StatusCode = HttpStatusCode.OK,
                MimeString = "text/json",
                ContentString = jsonResponse.ToString(),
                AllowCaching = false
            };
        }

        static async Task<bool> IsCaptchaValid(string captchaResponse, string remoteIp) {
            using (HttpClient httpClient = new HttpClient()) {
                var content = new StringContent($"secret={Config.SecretKey}&response={captchaResponse}&remoteip={remoteIp}", Encoding.UTF8, "application/x-www-form-urlencoded");
                HttpResponseMessage response = await httpClient.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);
                if (response.IsSuccessStatusCode) {
                    JSONNode obj = JSONNode.Parse(await response.Content.ReadAsStringAsync());
                    // Deserialize the JSON response
                    return obj["success"].AsBool;
                } else {
                    // Handle errors
                    return false;
                }
            }
        }
    }
}