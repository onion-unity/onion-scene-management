using System.Collections.Generic;

namespace Onion.SceneManagement.Utility {
    internal static class IEnumerableExtensions {
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable) {
            if (enumerable == null) return true;
            if (enumerable is ICollection<T> coll) return coll.Count == 0;
            
            using var e = enumerable.GetEnumerator();
            return !e.MoveNext();
        }
    }
}