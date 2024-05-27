using System.Collections.Generic;

namespace WebServer.Models {
    public interface IPageModel {
        /// <summary>
        /// Reflection would have made this so much easier,
        /// but one of my goals for this project is to not use
        /// reflection.
        /// </summary>
        Dictionary<string, object> Values { get; }
    }
    public class DynamicPageModel : IPageModel {
        public Dictionary<string, object> Values = new();
        Dictionary<string, object> IPageModel.Values => Values;
        
        public DynamicPageModel Add(string key, object value) {
            if (!Values.TryAdd(key, value))
                throw new Exception($"Failed to add key '{key}'! Could be a duplicate key");
            return this;
        }
    }
}
