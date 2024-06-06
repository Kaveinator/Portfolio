using System;
using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace System.Collections.Generic {
    public static class IDictionaryExtensions {
        public static Dictionary<TKey, TValue> RenameKey<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey oldKey, TKey newKey) {
            if (!dictionary.TryGetValue(oldKey, out TValue? value))
                throw new KeyNotFoundException($"The key '{oldKey}' does not exist in the dictionary.");

            dictionary.Remove(oldKey);
            dictionary.Add(newKey, value);
            return dictionary;
        }

        public static Dictionary<TKey, TValue> Update<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, Func<TValue, TValue> updateFunc) {
            if (!dictionary.TryGetValue(key, out TValue? value))
                throw new KeyNotFoundException($"The key '{key}' does not exist in the dictionary.");
            dictionary[key] = updateFunc(value);
            return dictionary;
        }
    }
}
