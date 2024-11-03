
namespace System.Collections.Generic {
    public static class IEnumerableExtensions {
        public static IEnumerable<T> Omit<T>(this IEnumerable<T> source, params T[] omitValues)
            => source.Where(item => !omitValues.Contains(item));

        public static IEnumerable<T> Include<T>(this IEnumerable<T> source, params T[] includeValues)
            => source.Concat(includeValues);

        
    }
}