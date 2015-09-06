using System;
using System.Collections.Generic;
using System.Text;

namespace Scrobbling
{
    public static class Util
    {
        /// <summary>
        /// Append a single element to the <see cref="IEnumerable{T}"/>.
        /// </summary>
        public static IEnumerable<T> Append<T>(this IEnumerable<T> collection, T element)
        {
            foreach (var item in collection)
            {
                yield return item;
            }
            yield return element;
        }

        public static bool ParseApiBool(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (value.Trim() == "1") return true;
            if (value.Trim() == "0") return false;
            return bool.Parse(value);
        }
    }
}
