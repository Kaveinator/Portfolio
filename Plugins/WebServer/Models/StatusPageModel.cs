using System.Collections.Generic;
using WebServer.Models;
using System.Net;

namespace WebServer.Models {
    public class StatusPageModel : IDataModel {
        Dictionary<string, object> IDataModel.Values => new() {
            { nameof(StatusCode), (int)StatusCode },
            { nameof(Header), Header },
            { nameof(Details), Details }
        };

        public HttpStatusCode StatusCode;
        public string Header;
        public string Details;

        static readonly Dictionary<HttpStatusCode, (string header, string details)> GenericStatusContent = new() {
            { HttpStatusCode.OK, ("OK", "The request succeeded") },
            { HttpStatusCode.BadRequest, ("Bad Request", "The server cannot or will not process the request due to something that is perceived to be a client error") },
            { HttpStatusCode.Unauthorized, ("Unauthorized", "Authorization Required") },
            { HttpStatusCode.Forbidden, ("Forbidden", "Access to this resource has been revoked") },
            { HttpStatusCode.NotFound, ("Not Found", "Object or Resource not Found") },
            { HttpStatusCode.PreconditionFailed, ("Precondition Failed!", "The server has indicated preconditions which the client does not meet") },
            { HttpStatusCode.InternalServerError, ("Internal Server Error", "The server has encountered a situation it does not know how to handle") },
            { HttpStatusCode.NotImplemented, ("Not Implemented", "he server has encountered a situation it does not know how to handle.") },
            { HttpStatusCode.ServiceUnavailable, ("Service Unavailable", "The server is not ready to handle the request") }
        };
        public StatusPageModel(HttpStatusCode statusCode) {
            StatusCode = statusCode;
            if (!GenericStatusContent.TryGetValue(statusCode, out var tuple))
                tuple = (statusCode.ToString(), "Great! Something happened, not sure what though");
            Header = tuple.header;
            Details = tuple.details;
        }

        public StatusPageModel(HttpStatusCode statusCode, string? title = null, string? subtitle = null) {
            StatusCode = statusCode;
            if (!GenericStatusContent.TryGetValue(statusCode, out var tuple))
                tuple = (statusCode.ToString(), "Great! Something happened, not sure what though");
            Header = title ?? tuple.header;
            Details = subtitle ?? tuple.details;
        }
    }
}