using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WebServer.Models;
using Portfolio;

namespace WebServer.Http {
    public static class HttpTemplates {
        static EventLogger Logger = EventLogger.GetOrCreate(typeof(HttpTemplates));
        public static DirectoryInfo PrivatePath = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, "Views"));
        public static DirectoryInfo PublicPath = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, "Public"));
        public static StreamReader GetStream(string path, bool isPrivate = true) {
            if (string.IsNullOrEmpty(path))
                return null;
            string fullPath = Path.Combine((isPrivate ? PrivatePath : PublicPath).FullName, path);
            if (!File.Exists(fullPath))
                return null;
            return new StreamReader(fullPath);
        }

        /* Unused Function
        static string Get(string path) {
            if (string.IsNullOrEmpty(path))
                return string.Empty;
            string fullPath = Path.Combine(ModelPath, path);
            if (!File.Exists(fullPath))
                return string.Empty;
            return File.ReadAllText(path);
        }*/

        public static bool TryGet(string path, out string result, IDataModel? pageModel = null) {
            Dictionary<string, object>? parameters = pageModel?.Values;
            result = string.Empty;
            if (string.IsNullOrEmpty(path))
                return false;
            path = path.Replace('\\', '/');
            if (path[0] == '/')
                path = path.Substring(1);
            string fullPath = Path.Combine(PrivatePath.FullName, path);
            if (!File.Exists(fullPath)) return false;
            result = Encoding.Default.GetString(File.ReadAllBytes(fullPath));
            if (parameters != null) {
                foreach (string key in parameters.Keys) {
                    string keyString = $"{{?:{key}}}";
                    object? value = parameters[key];
                    string strValue = value?.ToString() ?? string.Empty;
                    if (value is IPageComponentModel pageComponentModel)
                        strValue = pageComponentModel.Render();
                    else if (value is IEnumerable<IPageComponentModel> pagesComponentModel)
                        strValue = string.Join('\n', pagesComponentModel.Select(component => component.Render()));
                    //if (result.IndexOf(keyString) != -1)
                    result = result.Replace(keyString, strValue);
                }
            }
            if (MimeTypeMap.GetMimeType(path).ToLower().Contains("text"))
                result = Process(result);
            return true;
        }

        public static string Format(string content, IDataModel? pageModel = null) {
            Dictionary<string, object>? parameters = pageModel?.Values;
            if (parameters != null) {
                foreach (string key in parameters.Keys) {
                    string keyString = $"{{?:{key}}}";
                    object? value = parameters[key];
                    string strValue = value?.ToString() ?? string.Empty;
                    if (value is IPageComponentModel pageComponentModel)
                        strValue = pageComponentModel.Render();
                    else if (value is IEnumerable<IPageComponentModel> pagesComponentModel)
                        strValue = string.Join('\n', pagesComponentModel.Select(component => component.Render()));
                    //if (result.IndexOf(keyString) != -1)
                    content = content.Replace(keyString, strValue);
                }
            }
            return content;
        }

        public static string Get(string path, IDataModel? pageModel = null) {
            Dictionary<string, object>? parameters = pageModel?.Values;
            StreamReader reader = GetStream(path);
            if (reader == null)
                return string.Empty;

            StringBuilder result = new StringBuilder();
            // Read line by line  
            string? line;
            if (parameters != null && parameters.Count > 0) {
                while ((line = reader.ReadLine()) != null) {
                    foreach (string key in parameters.Keys) {
                        string keyString = $"{{?:{key}}}";
                        if (line.IndexOf(keyString) != -1) {
                            object value = parameters[key];
                            string strValue;
                            if (value is IPageComponentModel component)
                                strValue = component.Render();
                            else if (value is IEnumerable<IPageComponentModel> componentCollection)
                                strValue = string.Join('\n', componentCollection.Select(component => component.Render()));
                            else strValue = value?.ToString() ?? string.Empty;
                            line = line.Replace(keyString, strValue);
                        }
                    }
                    result.Append(line);
                }
            }
            reader.Close();
            if (MimeTypeMap.GetMimeType(path).ToLower().Contains("text"))
                return Process(result.ToString());

            return result.ToString();
        }

        /* Unused Function
        public static async Task<string> GetAsync(string path, Dictionary<string, string> parameters, bool keepParameterNamesIfKeyNotFound = false) {
            StreamReader reader = GetStream(path);
            if (reader == null)
                return string.Empty;

            StringBuilder result = new StringBuilder();
            // Read line by line  
            string line;
            while ((line = await reader.ReadLineAsync()) != null) {
                foreach (string key in parameters.Keys) {
                    string keyString = $"{{?:{key}}}";
                    if (line.IndexOf(keyString) != -1)
                        line = line.Replace(keyString, parameters[keyString]);
                }
                if (!keepParameterNamesIfKeyNotFound) {
                    int startIndex;
                    while (!string.IsNullOrEmpty(line) && (startIndex = line.IndexOf("{?:")) != -1) {
                        int endIndex = line.IndexOf('}', startIndex) + 1;
                        line = line.Substring(0, startIndex) + line.Substring(endIndex);
                    }
                }
                result.Append(line);
            }
            return result.ToString();
        }
        */

        public static string Process(string fileContents, bool keepModelTagsIfModelNotFound = false) {
            // Find all model tags
            string startTag = "<model",
                   endTag = "</model>",
                   attrName = "src=\"";
            int startIndex = 0;
            
            while ((startIndex = fileContents.IndexOf(startTag, startIndex)) != -1) {
                // Essentially replaces all tags of type <model src=""></model> with a model, without parameters
                int attributeStartIndex = startIndex, // |<model...
                    endIndex = fileContents.IndexOf(endTag, startIndex) + endTag.Length, // ...</model>|
                    srcAttributeStartIndex = fileContents.IndexOf(attrName, attributeStartIndex, endIndex - startIndex) + attrName.Length, // <model ... src="| ... </model>
                    srcAttributeEndIndex = fileContents.IndexOf("\"", srcAttributeStartIndex, endIndex - srcAttributeStartIndex); // <model ... src=" ... |" ... </model>

                string modelPath = fileContents.Substring(srcAttributeStartIndex, srcAttributeEndIndex - srcAttributeStartIndex);
                if (TryGet(modelPath, out string modelContent))
                    fileContents = fileContents.Substring(0, startIndex) + modelContent + fileContents.Substring(endIndex);
                else if (!keepModelTagsIfModelNotFound)
                    fileContents = fileContents.Substring(0, startIndex) + fileContents.Substring(endIndex);
                else startIndex = endIndex;
            }
            return fileContents;
        }
    }
}
