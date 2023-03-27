using System.Diagnostics.CodeAnalysis;

namespace Specifik;

static class Extensions
{
    public static bool TryAt<T>(this IList<T> list, Index index, [MaybeNullWhen(false)] out T item)
    {
        var at = index.IsFromEnd ? list.Count - index.Value - 1 : index.Value;
        if (at >= 0 && at < list.Count)
        {
            item = list[at];
            return true;
        }
        else
        {
            item = default;
            return false;
        }
    }
}