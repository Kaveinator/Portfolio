using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Linq {
    public static class LinqExt {
        public static IEnumerable<TTarget> GetAll<TTarget>(this IEnumerable<object> source)
            => source.Where(i => i is TTarget).Cast<TTarget>();


        /// <summary>Lets you do a foreach loop but without </summary>
        /// <typeparam name="T">The type that the IEnumerable contains</typeparam>
        /// <param name="source">IEnumerable instance</param>
        /// <param name="action">Method to call for each item, must return true to continue iteration, false to break</param>
        public static void ForEach<T>(this IEnumerable<T> source, Func<T, bool> action) {
            if (source == null) return;
            foreach (T item in source) if (action(item)) break;
        }

        /// <summary>Lets you do a foreach loop but easily</summary>
        /// <typeparam name="T">The type that the IEnumerable contains</typeparam>
        /// <param name="source">IEnumerable instance</param>
        /// <param name="action">Method to call for each item</param>
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action) {
            if (source == null) return;
            foreach (T item in source) action(item);
        }

        /// <summary>Alias of IEnumerable<T>.ForEach<T>(Func<T, bool>)</summary>
        /// <typeparam name="T">The type that the IEnumerable contains</typeparam>
        /// <param name="source">IEnumerable instance</param>
        /// <param name="action">Method to call for each item, must return true to continue iteration, false to break</param>
        public static void Do<T>(this IEnumerable<T> source, Func<T, bool> action) => ForEach<T>(source, action);
        /// <summary>Alias of IEnumerable<T>.ForEach<T>(Action<T>)</summary>
        /// <typeparam name="T">The type that the IEnumerable contains</typeparam>
        /// <param name="source">IEnumerable instance</param>
        /// <param name="action">Method to call for each itemk</param>
        public static void Do<T>(this IEnumerable<T> source, Action<T> action) => ForEach<T>(source, action);

        public static void Do<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source, Action<TKey, TValue> action)
            => source.ForEach(kvp => action(kvp.Key, kvp.Value));
    }
}
