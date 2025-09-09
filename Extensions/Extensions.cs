using System.Diagnostics.CodeAnalysis;

namespace CWF.Extensions;

internal static class Extensions {
    internal static bool IsNullOrEmpty([NotNullWhen(false)] this string? str) {
        return string.IsNullOrEmpty(str);
    }

    internal static bool IsNullOrEmpty<T>([NotNullWhen(false)] this ICollection<T>? collection) {
        return collection == null || collection.Count == 0;
    }
}