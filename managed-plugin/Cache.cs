using Scrobbling;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace xmp_sharp_scrobbler_managed
{
    /// <summary>
    /// Implements synchronized read/write access to a cache file
    /// to persist scrobbles across plugin restarts.
    /// </summary>
    /// <remarks>
    /// Everything is done so that a scrobble is never lost.
    /// In the worst case, a scrobble might be scrobbled twice
    /// (Last.fm ignore double scrobbles based on the timestamp, so this is ok)
    /// but it should never be lost.
    /// Also, if the file cannot be accessed, the cache will not work (obviously),
    /// but operations will succeed nonetheless to avoid blocking the scrobbling process.
    /// </remarks>
    public class Cache : IDisposable
    {
        public const string DefaultFileName = "SharpScrobbler.cache";
        private const string HeaderSignature = "XSSCache";
        private const int CurrentVersion = 1;

        /// <summary>
        /// Once <see cref="Dispose"/> is called, this instance cannot be re-used.
        /// </summary>
        private bool instanceDisposed = false;
        /// <summary>
        /// Used to lock around the FileStream creation and underlying file access.
        /// </summary>
        private object accquireFileLockLocker = new object();
        /// <summary>
        /// Asynchronous lock to prevent concurrent access to a public cache operation.
        /// </summary>
        private AsyncLock fileOperationAsyncLock = new AsyncLock();
        /// <summary>
        /// Set to true once the FileStream is successfuly created, to avoid trying to re-create it.
        /// </summary>
        private bool fileLockAcquired = false;
        /// <summary>
        /// Global FileStream for the cache file.
        /// The underlying handle prevents access to the file from other processes.
        /// The lock is maintained for the life of the current instance.
        /// </summary>
        private FileStream fileStream = null;

        /// <summary>
        /// Path to the underlying cache file.
        /// </summary>
        public string Location { get; }
        /// <summary>
        /// Is the cache file actually being used?
        /// If the file cannot be accessed, this will return false,
        /// but operations will still return without exceptions.
        /// Return true when everything works as expected.
        /// </summary>
        public bool IsOperational => fileLockAcquired;

        public Cache(string location = null)
        {
            Location = location ?? GetDefaultPath();
            TryAcquireFileLock();
        }

        public async Task StoreAsync(Scrobble scrobble)
        {
            if (!TryAcquireFileLock()) return;

            using (await fileOperationAsyncLock.LockAsync())
            {
                // Note about the file content:
                // The file is line separated.
                // Instead of writing the data followed by a new line, we do the opposite:
                // We always start by writing a new line, followed by the data.
                // This way, we don't risk continuing a corrupted line without a proper line ending.
                // Unreadable lines can then be discarded, and we lose only one record instead of two.

                await EnsureCorrectHeaderAndGetFileVersionAsync(fileStream);

                // Skip to the end of the file to append a new scrobble.
                fileStream.Seek(0, SeekOrigin.End);

                var serializedScrobble = ScrobbleSerializer.Serialize(scrobble);
                byte[] buffer = Encoding.UTF8.GetBytes(serializedScrobble);
                byte[] newLineBuffer = Encoding.UTF8.GetBytes("\n");
                await fileStream.WriteAsync(newLineBuffer, 0, newLineBuffer.Length);
                await fileStream.WriteAsync(buffer, 0, buffer.Length);
            }
        }

        public async Task<IReadOnlyCollection<Scrobble>> RetrieveAsync()
        {
            if (!TryAcquireFileLock()) return new Scrobble[0];

            using (await fileOperationAsyncLock.LockAsync())
            {
                await EnsureCorrectHeaderAndGetFileVersionAsync(fileStream);

                return await RetrieveInternalAsync(fileStream);
            }
        }

        /// <summary>
        /// Read lines from the current position to the end of the file.
        /// </summary>
        private static async Task<IReadOnlyCollection<Scrobble>> RetrieveInternalAsync(FileStream fs)
        {
            List<Scrobble> result = new List<Scrobble>();
            using (var reader = GetReader(fs))
            {
                while (!reader.EndOfStream)
                {
                    // Try to parse each line to a scrobble, ignoring failures.
                    var line = await reader.ReadLineAsync();
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        try
                        {
                            var scrobble = ScrobbleSerializer.Deserialize(line);
                            result.Add(scrobble);
                        }
                        catch (Exception)
                        {
                            continue;
                            // TODO: log
                        }
                    }
                }
            }
            return result;
        }

        public async Task RemoveScrobblesAsync(IReadOnlyCollection<Scrobble> scrobbles)
        {
            if (!TryAcquireFileLock()) return;

            using (await fileOperationAsyncLock.LockAsync())
            {
                await EnsureCorrectHeaderAndGetFileVersionAsync(fileStream);

                // Find the scrobbles in the file not picked for deletion, that we have to rewrite.
                var cachedScrobbles = await RetrieveInternalAsync(fileStream);
                var remainingScrobbles = cachedScrobbles.Except(scrobbles, new ScrobbleEqualityComparer());
                // Rewrite the cache file entirely.
                await OverwriteFileAsync(fileStream, remainingScrobbles);
            }
        }

        /// <summary>
        /// Erase the file, write a new header, then write the scrobbles.
        /// </summary>
        private static async Task OverwriteFileAsync(FileStream fs, IEnumerable<Scrobble> scrobbles)
        {
            // Truncate the file.
            fs.SetLength(0);
            // Write the header.
            await EnsureCorrectHeaderAndGetFileVersionAsync(fs);
            // Skip to the end of the file to append the scrobbles.
            fs.Seek(0, SeekOrigin.End);

            // To minimize file writes, we use a StringBuilder that will be written in the file
            // in as few buffer lengths as possible.
            StringBuilder sb = new StringBuilder();
            foreach (var scrobble in scrobbles)
            {
                var serialized = ScrobbleSerializer.Serialize(scrobble);
                sb.AppendLine();
                sb.Append(serialized);
            }

            byte[] buffer = Encoding.UTF8.GetBytes(sb.ToString());
            await fs.WriteAsync(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Reset the <see cref="FileStream"/> position to the beginning then parse the first line of the file.
        /// If the file is empty, the header is created.
        /// If the header is invalid or the version of the file is unknown, an exception is thrown.
        /// Before returning, the stream position is set to the end of the header.
        /// </summary>
        private static async Task<int> EnsureCorrectHeaderAndGetFileVersionAsync(FileStream fs)
        {
            fs.Seek(0, SeekOrigin.Begin);
            int headerLength = 0;
            int version;
            using (var reader = GetReader(fs))
            {
                // Read the header (the first line of the file, which is supposed to contain the header signature followed by the file version)
                var header = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(header))
                {
                    // This is a new file, so we write the header now...
                    header = $"{HeaderSignature}{CurrentVersion}";
                    var headerBuffer = Encoding.UTF8.GetBytes(header);
                    await fs.WriteAsync(headerBuffer, 0, headerBuffer.Length);
                    await fs.FlushAsync();
                }
                headerLength = header.Length;
                if (header.StartsWith(HeaderSignature) == false)
                {
                    throw new Exception("The cache file header does not match the expected signature!");
                }
                // Parse the version.
                if (int.TryParse(header.Substring(startIndex: HeaderSignature.Length), out version) == false)
                {
                    throw new Exception("Could not read the cache file version!");
                }
                // We only try to handle files with a known version.
                if (version < 1 && version > CurrentVersion)
                {
                    throw new Exception("Unexpected cache file version!");
                }
            }
            // Yeah I know character count is not necessarily the same as byte count,
            // but the header is supposed to only contain ASCII characters so this will do.
            fs.Seek(Math.Min(headerLength, fs.Length), SeekOrigin.Begin);
            return version;
        }

        /// <summary>
        /// Get a <see cref="StreamReader"/> for the specified <see cref="FileStream"/>.
        /// The encoding is set to <see cref="Encoding.UTF8"/> and the underlying <see cref="FileStream"/>
        /// is left open when the <see cref="StreamReader"/> is closed.
        /// </summary>
        private static StreamReader GetReader(FileStream fs)
            => new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);

        /// <summary>
        /// Try to (re)acquire a file lock on the cache file.
        /// If a file lock is already acquired, simply return true.
        /// This method does not throw.
        /// </summary>
        /// <returns></returns>
        private bool TryAcquireFileLock()
        {
            lock (accquireFileLockLocker)
            {
                if (instanceDisposed)
                {
                    throw new ObjectDisposedException(nameof(Cache));
                }

                if (fileLockAcquired)
                {
                    // We already have a file lock.
                    return true;
                }
                else
                {
                    try
                    {
                        // Try to acquire an exclusive lock on the file.
                        var fs = new FileStream(Location, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                        fileStream = fs;
                        fileLockAcquired = true;
                        return true;
                    }
                    catch (Exception)
                    {
                        // TODO: log
                        fileLockAcquired = false;
                        return false;
                    }
                }
            }
        }

        /// <summary>
        /// Returns the default cache file path based on the current process executable file location.
        /// </summary>
        public static string GetDefaultPath()
        {
            var currentDirectory = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            return Path.Combine(currentDirectory, DefaultFileName);
        }

        /// <summary>
        /// Dispose of the underlying file handle.
        /// </summary>
        public void Dispose()
        {
            // FIXME: this method is probably not thread safe regarding the asynchronous file operations.
            lock (accquireFileLockLocker)
            {
                if (fileLockAcquired)
                {
                    try
                    {
                        fileStream.Dispose();
                    }
                    catch { }
                    instanceDisposed = true;
                }
            }
        }

        ~Cache()
        {
            Dispose();
        }
    }
}
