// Copyright(c) 2015-2019 Melvyn Laïly
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
// of the Software, and to permit persons to whom the Software is furnished to do
// so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Scrobbling;

namespace XmpSharpScrobbler
{
    /// <summary>
    /// Implements synchronized read/write access to a cache file
    /// to persist scrobbles across plugin restarts.
    /// The class implements <see cref="IDisposable"/>
    /// and locks the underlying file until <see cref="Dispose"/> is called
    /// or the instance is garbage collected.
    /// </summary>
    /// <remarks>
    /// Everything is done so that a scrobble is never lost.
    /// In the worst case, a scrobble might be scrobbled twice
    /// (Last.fm ignore double scrobbles based on the timestamp, so this is ok)
    /// but it should never be lost.
    /// </remarks>
    internal class Cache : IDisposable
    {
        public const string DefaultFileName = "SharpScrobbler.cache";
        private const string HeaderSignature = "XSSCache";
        private const int CurrentVersion = 1;

        /// <summary>
        /// Once <see cref="Dispose"/> is called, this instance cannot be re-used.
        /// </summary>
        private bool _instanceDisposed = false;
        /// <summary>
        /// Used to lock around the FileStream creation and underlying file access.
        /// </summary>
        private readonly object _accquireFileLockLocker = new object();
        /// <summary>
        /// Asynchronous lock to prevent concurrent access to a public cache operation.
        /// </summary>
        private readonly AsyncLock _fileOperationAsyncLock = new AsyncLock();
        /// <summary>
        /// Set to true once the FileStream is successfuly created, to avoid trying to re-create it.
        /// </summary>
        private bool _fileLockAcquired = false;
        /// <summary>
        /// Global FileStream for the cache file.
        /// The underlying handle prevents write access to the file from other processes.
        /// The lock is maintained for the life of the current instance.
        /// </summary>
        private FileStream _fileStream = null;

        /// <summary>
        /// Path to the underlying cache file.
        /// </summary>
        public string Location { get; }


        /// <summary>
        /// Creates an instance of <see cref="Cache"/> and try to acquire an exclusive lock on the underlying file.
        /// If the file cannot be locked, no exception is thrown in the ctor, but the instance will be virtually useless until the file can be locked.
        /// All the <see cref="Cache"/> public operations will try to reacquire the lock when called, and will throw if it then fails.
        /// </summary>
        /// <param name="location">Full path to the desired cache file, or null to use the default location.</param>
        public Cache(string location = null)
        {
            Location = location ?? GetDefaultPath();
            try
            {
                // Try to acquire a file lock as soon as possible.
                EnsureFileLockIsAcquired();
            }
            catch
            {
                // Throwing in the constructor is not a really nice thing to do :)
            }
        }

        /// <summary>
        /// Asynchronously stores a scrobble in the underlying cache file.
        /// Will throw an exception if the cache file cannot be accessed.
        /// This method is thread safe.
        /// </summary>
        public async Task StoreAsync(Scrobble scrobble)
        {
            EnsureFileLockIsAcquired();

            using (await _fileOperationAsyncLock.LockAsync())
            {
                // Note about the file content:
                // The file is line separated.
                // Instead of writing the data followed by a new line, we do the opposite:
                // We always start by writing a new line, followed by the data.
                // This way, we don't risk continuing a corrupted line without a proper line ending.
                // Unreadable lines can then be discarded, and we lose only one record instead of two.

                await EnsureCorrectHeaderAndGetFileVersionAsync(_fileStream);

                // Skip to the end of the file to append a new scrobble.
                _fileStream.Seek(0, SeekOrigin.End);

                var serializedScrobble = ScrobbleSerializer.Serialize(scrobble);
                byte[] buffer = Encoding.UTF8.GetBytes(serializedScrobble);
                byte[] newLineBuffer = Encoding.UTF8.GetBytes("\n");
                await _fileStream.WriteAsync(newLineBuffer, 0, newLineBuffer.Length);
                await _fileStream.WriteAsync(buffer, 0, buffer.Length);
            }
        }

        /// <summary>
        /// Asynchronously retrieves all the scrobbles stored in the underlying cache file.
        /// Will throw an exception if the cache file cannot be accessed.
        /// This method is thread safe.
        /// </summary>
        public async Task<CacheRetrievalResult> RetrieveAsync()
        {
            EnsureFileLockIsAcquired();

            using (await _fileOperationAsyncLock.LockAsync())
            {
                await EnsureCorrectHeaderAndGetFileVersionAsync(_fileStream);

                return await RetrieveInternalAsync(_fileStream);
            }
        }

        /// <summary>
        /// Asynchronously removes scrobbles from the underlying cache file.
        /// Will throw an exception if the cache file cannot be accessed.
        /// This method is thread safe.
        /// </summary>
        public async Task RemoveScrobblesAsync(IReadOnlyCollection<Scrobble> scrobbles)
        {
            EnsureFileLockIsAcquired();

            using (await _fileOperationAsyncLock.LockAsync())
            {
                await EnsureCorrectHeaderAndGetFileVersionAsync(_fileStream);

                // Find the scrobbles in the file not picked for deletion, that we have to rewrite.
                var retrievalResult = await RetrieveInternalAsync(_fileStream);
                var remainingScrobbles = retrievalResult.Scrobbles.Except(scrobbles, new ScrobbleEqualityComparer());
                // Rewrite the cache file entirely.
                await OverwriteFileAsync(_fileStream, remainingScrobbles);
            }
        }

        /// <summary>
        /// Reads lines from the current position to the end of the file.
        /// </summary>
        private static async Task<CacheRetrievalResult> RetrieveInternalAsync(FileStream fs)
        {
            List<Scrobble> result = new List<Scrobble>();
            List<string> failedLines = new List<string>();
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
                        catch
                        {
                            failedLines.Add(line);
                        }
                    }
                }
            }
            return new CacheRetrievalResult(result, failedLines);
        }

        /// <summary>
        /// Erases the file, write a new header, then write the scrobbles.
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
        /// Resets the <see cref="FileStream"/> position to the beginning then parse the first line of the file.
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
                if (header.StartsWith(HeaderSignature, StringComparison.Ordinal) == false)
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
        /// Gets a <see cref="StreamReader"/> for the specified <see cref="FileStream"/>.
        /// The encoding is set to <see cref="Encoding.UTF8"/> and the underlying <see cref="FileStream"/>
        /// is left open when the <see cref="StreamReader"/> is closed.
        /// </summary>
        private static StreamReader GetReader(FileStream fs)
            => new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);

        /// <summary>
        /// Returns the default cache file path based on the current process executable file location.
        /// </summary>
        public static string GetDefaultPath()
        {
            var currentDirectory = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            return Path.Combine(currentDirectory, DefaultFileName);
        }

        /// <summary>
        /// Tries to (re)acquire a file lock on the cache file.
        /// If a file lock is already acquired, does nothing.
        /// Throws an exception in case of failure.
        /// </summary>
        private void EnsureFileLockIsAcquired()
        {
            lock (_accquireFileLockLocker)
            {
                if (_instanceDisposed)
                {
                    throw new ObjectDisposedException(nameof(Cache));
                }

                if (_fileLockAcquired)
                {
                    // We already have a file lock.
                    return;
                }
                else
                {
                    // Try to acquire an exclusive lock on the file.
                    var fs = new FileStream(Location, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
                    _fileStream = fs;
                    _fileLockAcquired = true;
                }
            }
        }

        /// <summary>
        /// Disposes of the underlying file handle.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // FIXME: this method is probably not thread safe regarding the asynchronous file operations.
                lock (_accquireFileLockLocker)
                {
                    if (_fileLockAcquired)
                    {
                        try
                        {
                            _fileStream.Dispose();
                        }
                        catch { }
                        _instanceDisposed = true;
                    }
                }
            }
        }

        ~Cache() => Dispose(false);
    }

    public class CacheRetrievalResult
    {
        public IReadOnlyCollection<Scrobble> Scrobbles { get; }
        public IReadOnlyCollection<string> FailedLines { get; }
        public CacheRetrievalResult(IReadOnlyCollection<Scrobble> scrobbles, IReadOnlyCollection<string> failedLines)
        {
            Scrobbles = scrobbles;
            FailedLines = failedLines;
        }
    }
}
