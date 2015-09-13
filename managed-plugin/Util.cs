using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xmp_sharp_scrobbler_managed
{
    public delegate void ShowInfoBubbleHandler(string text, int displayTimeMs);

    public static class Util
    {
        private static ShowInfoBubbleHandler _ShowInfoBubble;
        public static void InitializeShowBubbleInfo(ShowInfoBubbleHandler showInfoBubble)
            => _ShowInfoBubble = showInfoBubble;
        public static void ShowInfoBubble(string text, TimeSpan? displayTime = null)
            => _ShowInfoBubble?.Invoke(text, displayTime == null ? 0 : (int)displayTime.Value.TotalMilliseconds);

        /// <summary>
        /// Lazily return the source <see cref="IEnumerable{T}"/>, partitionned by <paramref name="partitionSize"/>.
        /// </summary>
        public static IEnumerable<IEnumerable<TElement>> Partition<TElement>(this IEnumerable<TElement> source, int partitionSize)
        {
            int lastPartitionMaxIndex = int.MaxValue;
            for (int partitionNumber = 0; lastPartitionMaxIndex >= partitionSize; partitionNumber++)
            {
                yield return source
                   .Skip(partitionNumber * partitionSize)
                   .TakeWhile((x, i) =>
                   {
                       lastPartitionMaxIndex = i;
                       return i < partitionSize;
                   })
                   // Force eager iteration inside partitions to avoid infinite loops if the partitions are not iterated themselves.
                   .ToList();
            }
        }

    }
}
