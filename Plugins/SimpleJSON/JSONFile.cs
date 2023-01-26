using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleJSON {
    public partial class JSONNode {
        public virtual JSONFile AsFile => new JSONFile($"{Guid.NewGuid()}.json").Append(this);
        public bool IsEmpty => Children.Count() == 0;
    }
    public class JSONFile : JSONObject {
        /// <summary>Encoding used when reading/writing</summary>
        public Encoding Encoding = Encoding.UTF8;

        public readonly FileInfo Info;

        public JSONFile(string path) {
            Info = new FileInfo(path);
        }

        /// <summary>Load the File</summary>
        /// <returns>Current instance</returns>
        /// <exception cref="FormatException">Thrown when the file failed to parse</exception>
        public JSONFile Load() {
            Info.Refresh(); // Reload file info

            if (!Info.Exists) return Clear();

            if (!JSON.TryParse(File.ReadAllText(Info.FullName, Encoding), out JSONNode node))
                throw new FormatException($"JSONFile Parse Error! [Location: '{Info.FullName}']");

            return Clear().Append(node);
        }

        /// <summary>Loads the file asynchronously</summary>
        /// <param name="bufferSize">The buffer size, keep at default if you don't know what this means</param>
        /// <returns>Current instance</returns>
        /// <exception cref="FormatException">Thrown when the file failed to parse</exception>
        public async Task<JSONFile> LoadAsync(int bufferSize = 0x1000) {
            Info.Refresh();

            if (!Info.Exists) return Clear();

            using (FileStream sourceStream = Info.OpenRead()) {
                StringBuilder builder = new StringBuilder();

                byte[] buffer = new byte[bufferSize];
                int numRead; // Ammount of bytes read per cycle
                while ((numRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                    builder.Append(Encoding.GetString(buffer, 0, numRead));

                if (!JSON.TryParse(builder.ToString(), out JSONNode node))
                    throw new FormatException($"JSONFile Parse Error! [Location: '{Info.FullName}']");

                return Clear().Append(node);
            }
        }

        /// <summary>Save the file</summary>
        /// <param name="formatted">Should the output be pretty printed?</param>
        /// <returns>Current instance</returns>
        public JSONFile Save(int? aIndent = null) {
            Info.Refresh();
            if (!Info.Directory.Exists)
                Info.Directory.Create();
            File.WriteAllText(Info.FullName, aIndent.HasValue ? ToString(aIndent.Value) : ToString());
            return this;
        }

        /// <summary>Saves current data asynchronously</summary>
        /// <param name="formatted">Should the file be pretty printed?</param>
        /// <param name="bufferSize">The buffer size, keep at default if you don't know what this means</param>
        /// <returns>Current instance</returns>
        public async Task<JSONFile> SaveAsync(int? aIndent = null) {
            if (!Info.Directory.Exists)
                Info.Directory.Create();

            byte[] buffer = Encoding.GetBytes(aIndent.HasValue ? ToString(aIndent.Value) : ToString());
            using (FileStream sourceStream = Info.OpenWrite()) {
                await sourceStream.WriteAsync(buffer, 0, buffer.Length);
            };
            return this;
        }

        /// <summary>Append nodes to the file</summary>
        /// <param name="node">The JSONObject to append</param>
        /// <returns>Current instance</returns>
        public JSONFile Append(JSONNode node) {
            foreach (KeyValuePair<string, JSONNode> pair in node)
                Add(pair.Key, pair.Value);
            return this;
        }

        /// <summary>Clears the data in the file</summary>
        /// <returns>Current instance</returns>
        public new JSONFile Clear() {
            base.Clear();
            return this;
        }
    }
}
