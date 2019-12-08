using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XmpSharpScrobbler.Misc
{
    public static class InteropHelper
    {
        public static string GetStringFromNativeBuffer(byte[] source, Encoding encoding)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (encoding == null) throw new ArgumentNullException(nameof(encoding));

            // GetString() includes null characters, but we don't want them.
            // We can't just trim at the byte level before, because it might f-up the encoding...
            var decoded = encoding.GetString(source);
            // Also trim unicode replacement characters (�) that can appear
            // when the buffer is too small to encode the last null character
            // (that can take more than one byte in some encodings)
            return decoded.TrimEnd('\0', '�');
        }

        /// <summary>
        /// Writes <paramref name="value"/> to <paramref name="backingField"/>, using the specified <paramref name="encoding"/>.
        /// If <paramref name="throwOnBufferTooSmall"/> is false and the buffer is too small for <paramref name="value"/>,
        /// <paramref name="backingField"/> is cleared.
        /// </summary>
        public static void SetNativeString(byte[] backingField, Encoding encoding, string value, bool throwOnBufferTooSmall = false)
        {
            if (backingField == null) throw new ArgumentNullException(nameof(backingField));
            if (encoding == null) throw new ArgumentNullException(nameof(encoding));

            // By design, we can't change the buffer length or set it to null
            if (value == null)
            {
                Array.Clear(backingField, 0, backingField.Length);
            }
            else
            {
                int requiredSize = encoding.GetByteCount(value);
                if (backingField.Length < requiredSize)
                {
                    if (throwOnBufferTooSmall)
                    {
                        throw new ArgumentException(
                            $"Buffer too small ({backingField.Length}) to fit the required {requiredSize} byte(s).",
                            nameof(backingField));
                    }
                    else
                    {
                        // If throwOnBufferTooSmall is false, values too large result in an empty string.
                        Array.Clear(backingField, 0, backingField.Length);
                    }
                }
                else // buffer large enough
                {
                    int written = encoding.GetBytes(value, 0, value.Length, backingField, 0);
                    Array.Clear(backingField, written, backingField.Length - written);
                }
            }
        }
    }
}
