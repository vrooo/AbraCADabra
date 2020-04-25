using System.Collections.Generic;

namespace AbraCADabra
{
    public static class ListExtender
    {
        public static void AddMany<T>(this List<T> list, params T[] items)
        {
            list.AddRange(items);
        }
    }
}
