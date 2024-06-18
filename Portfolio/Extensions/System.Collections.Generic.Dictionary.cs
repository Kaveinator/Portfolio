using System;
using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace System.Collections.Generic {
    public static class IDictionaryExtensions {
        public static Dictionary<TKey, TValue> RenameKey<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey oldKey, TKey newKey) where TKey : notnull {
            if (!dictionary.TryGetValue(oldKey, out TValue? value))
                throw new KeyNotFoundException($"The key '{oldKey}' does not exist in the dictionary.");

            dictionary.Remove(oldKey);
            dictionary.Add(newKey, value);
            return dictionary;
        }

        public static Dictionary<TKey, TValue> Update<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, Func<TValue, TValue> updateFunc) where TKey : notnull {
            if (!dictionary.TryGetValue(key, out TValue? value))
                throw new KeyNotFoundException($"The key '{key}' does not exist in the dictionary.");
            dictionary[key] = updateFunc(value);
            return dictionary;
        }

        public static Dictionary<TKey, TValue> UpdateKeys<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, Func<TKey, TKey> updateFunc) where TKey : notnull
            => dictionary.ToDictionary(pair => updateFunc(pair.Key), pair => pair.Value);

        public static Dictionary<TKey, TValue> MergeDictionaries<TKey, TValue>(
            this Dictionary<TKey, TValue> first,
            Dictionary<TKey, TValue> second,
            Func<TValue, TValue, TValue> conflictResolver
        ) where TKey : notnull {
            return first.Concat(second)
                .GroupBy(pair => pair.Key)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(pair => pair.Value).Aggregate(conflictResolver)
                );
        }
    }
}
