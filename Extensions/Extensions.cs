using System.Diagnostics.CodeAnalysis;

namespace CWF.Extensions;

public static class Extensions {
    public static bool IsNullOrEmpty([NotNullWhen(false)] this string? str) {
        return string.IsNullOrEmpty(str);
    }

    public static bool IsNullOrEmpty<T>([NotNullWhen(false)] this ICollection<T>? collection) {
        return collection == null || collection.Count == 0;
    }
}