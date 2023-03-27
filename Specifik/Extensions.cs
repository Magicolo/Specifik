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

    public static void Shuffle<T>(this IList<T> list, Random? random = null)
    {
        random ??= Random.Shared;
        for (int i = 0; i < list.Count; i++)
        {
            var index = random.Next(0, list.Count);
            (list[i], list[index]) = (list[index], list[i]);
        }
    }
}