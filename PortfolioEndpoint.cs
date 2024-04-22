using System.Net;
using System.Web;
using System.Collections.Specialized;
using WebServer.Http;
using SimpleJSON;
using Portfolio.Commands;
using System.Text.RegularExpressions;
using System.Text;

namespace Portfolio {
    internal class PortfolioEndpoint : HttpEndpointHandler {
        public readonly PortfolioDatabase Database;
        public PortfolioEndpoint(HttpServer server) : base("kavemans.dev", server) {
            Database = PortfolioDatabase.GetOrCreate();
            AddEventCallback("/contact", OnContactRequest);
        }

        // Site Key: 6LfV48IpAAAAANnkENaFMFCoq1DASzzdqbiGbrzW
        // Secret Key: 6LfV48IpAAAAAJntMMii2GLXMXEQc1n5PxlV629p
        static Regex EmailValidator = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
        async Task<HttpResponse?> OnContactRequest(HttpListenerRequest request) {
            if (!request.HttpMethod.Equals(HttpMethod.Post.Method, StringComparison.InvariantCultureIgnoreCase))
                return null; // By returning null, underlaying server will take care of GET
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
            string message = query[nameof(message)]?.Trim() ?? string.Empty;
            string? recaptchaToken = query[nameof(recaptchaToken)];
            string remoteIp = request.Headers["CF-Connecting-IP"] ?? request.RemoteEndPoint.Address.ToString();
            bool? captchaValid = null;
            if (string.IsNullOrEmpty(recaptchaToken)) {
                success = false;
                errorMessage = "Captcha Token Missing!";
                goto shipResponse;
            }
            captchaValid = await IsCaptchaValid(recaptchaToken, remoteIp);
            if (!captchaValid.Value) {
                success = false;
                errorMessage = "Captcha Validation Failed!";
                goto shipResponse;
            }
            if (name.Length < 2) {
                success = false;
                errorMessage = "Name is too short!";
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

            success = true;
            var info = new ContactInfo(Database.ContactInfoTable, name, email ?? "", message).Push();
        shipResponse:
            jsonResponse.Add(nameof(success), success);
            jsonResponse.Add(nameof(errorMessage), errorMessage);
#if DEBUG
            if (Program.Mode == Mode.Development) {
                JSONNode dump = new JSONObject();
                dump.Add(nameof(name), name);
                dump.Add(nameof(email), email);
                dump.Add(nameof(message), message);
                dump.Add(nameof(recaptchaToken), recaptchaToken);
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
            const string secretKey = "6LfV48IpAAAAAJntMMii2GLXMXEQc1n5PxlV629p";
            using (HttpClient httpClient = new HttpClient()) {
                var content = new StringContent($"secret={secretKey}&response={captchaResponse}&remoteip={remoteIp}", Encoding.UTF8, "application/x-www-form-urlencoded");
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