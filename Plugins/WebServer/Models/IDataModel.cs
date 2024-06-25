using System.Collections.Generic;
using WebServer.Http;

namespace WebServer.Models {
    public interface IDataModel {
        /// <summary>
        /// Reflection would have made this so much easier,
        /// but one of my goals for this project is to not use
        /// reflection.
        /// </summary>
        Dictionary<string, object> Values { get; }
    }
    public interface IPageModel : IDataModel {
        HttpResponse Render();
    }
    public interface IPageComponentModel : IDataModel {
        string Render();
    }
    public class DynamicPageModel : IDataModel {
        public Dictionary<string, object> Values = new();
        Dictionary<string, object> IDataModel.Values => Values;
        
        public DynamicPageModel Add(string key, object value) {
            if (!Values.TryAdd(key, value))
                throw new Exception($"Failed to add key '{key}'! Could be a duplicate key");
            return this;
        }
    }
}
