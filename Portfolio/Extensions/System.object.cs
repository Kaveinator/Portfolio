using System.Runtime.Serialization;

namespace System {
    public static class ObjectExt {
        public static bool IsOfAny<T>(this T obj, params T[] array) {
            foreach (var item in array)
                if (item?.Equals(obj) ?? false) return true;
            return false;
        }

        public static bool HasAnyFlags<T>(this T obj, params T[] array) where T : Enum
            => array.Any(flag => obj.HasFlag(flag));
    }
}
