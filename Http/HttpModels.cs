using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace WebServer.Http {
    public static class HttpTemplates {
        static EventLogger Logger = EventLogger.GetOrCreate(typeof(HttpTemplates));
        public static DirectoryInfo PrivatePath = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, "PrivateTemplates"));
        public static DirectoryInfo PublicPath = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, "PublicTemplates"));
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

        public static bool TryGet(string path, out string result, Dictionary<string, object> parameters = null) {
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
                    //if (result.IndexOf(keyString) != -1)
                        result = result.Replace(keyString, parameters[key].ToString());
                }
            }
            if (MimeTypeMap.GetMimeType(path).ToLower().Contains("text"))
                result = Process(result);
            return true;
        }

        public static string Format(string content, Dictionary<string, object> parameters) {
            foreach (string key in parameters.Keys) {
                string keyString = $"{{?:{key}}}";
                //if (result.IndexOf(keyString) != -1)
                content = content.Replace(keyString, parameters[key].ToString());
            }
            return content;
        }

        public static string Get(string path, Dictionary<string, string> parameters = null, bool keepParameterNamesIfKeyNotFound = false) {
            StreamReader reader = GetStream(path);
            if (reader == null)
                return string.Empty;

            StringBuilder result = new StringBuilder();
            // Read line by line  
            string line;
            if (parameters != null && parameters.Count > 0) {
                while ((line = reader.ReadLine()) != null) {
                    foreach (string key in parameters.Keys) {
                        string keyString = $"{{?:{key}}}";
                        if (line.IndexOf(keyString) != -1)
                            line = line.Replace(keyString, parameters[key]);
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

        public static string Process(string file, bool keepModelTagsIfModelNotFound = false) {
            // Find all model tags
            string startTag = "<model",
                   endTag = "</model>",
                   attrName = "src=\"";
            int startIndex = 0;
            
            while ((startIndex = file.IndexOf(startTag, startIndex)) != -1) {
                // Essentially replaces all tags of type <model src=""></model> with a model, without parameters
                int attributeStartIndex = startIndex, // |<model...
                    endIndex = file.IndexOf(endTag, startIndex) + endTag.Length, // ...</model>|
                    srcAttributeStartIndex = file.IndexOf(attrName, attributeStartIndex, endIndex - startIndex) + attrName.Length, // <model ... src="| ... </model>
                    srcAttributeEndIndex = file.IndexOf("\"", srcAttributeStartIndex, endIndex - srcAttributeStartIndex); // <model ... src=" ... |" ... </model>

                string modelPath = file.Substring(srcAttributeStartIndex, srcAttributeEndIndex - srcAttributeStartIndex);
                if (TryGet(modelPath, out string modelContent))
                    file = file.Substring(0, startIndex) + modelContent + file.Substring(endIndex);
                else if (!keepModelTagsIfModelNotFound)
                    file = file.Substring(0, startIndex) + file.Substring(endIndex);
                else startIndex = endIndex;
            }
            return file;
        }
    }
}
