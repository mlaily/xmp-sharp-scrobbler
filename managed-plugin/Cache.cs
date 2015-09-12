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
    public interface ICompletableAction
    {
        bool TrySetException(Exception exception);
    }
    public abstract class CacheAction<TResult> : ICompletableAction
    {
        public TaskCompletionSource<TResult> CompletionSource { get; protected set; }

        public CacheAction()
        {
            CompletionSource = new TaskCompletionSource<TResult>();
        }

        bool ICompletableAction.TrySetException(Exception exception)
        => CompletionSource.TrySetException(exception);
    }
    public class StoreAction : CacheAction<Unit>
    {
        public Scrobble Scrobble { get; }
        public StoreAction(Scrobble scrobble)
        {
            Scrobble = scrobble;
        }
    }
    public class RetrieveAllAction : CacheAction<IReadOnlyCollection<Scrobble>> { }
    public class RemoveScrobblesAction : CacheAction<Unit>
    {
        public IReadOnlyCollection<Scrobble> Scrobbles { get; }
        public RemoveScrobblesAction(IReadOnlyCollection<Scrobble> scrobbles)
        {
            Scrobbles = scrobbles;
        }
    }

    /// <summary>
    /// Implements synchronized read/write access to a cache file
    /// to persist scrobbles across plugin restarts.
    /// </summary>
    /// <remarks>
    /// Everything is done so that a scrobble is never lost.
    /// In the worst case, a scrobble could be scrobbled twice
    /// (Last.fm ignore double scrobbles based on the timestamp, so this is ok)
    /// but it should not be lost.
    /// </remarks>
    public class Cache
    {
        public const string DefaultFileName = "SharpScrobbler.cache";
        public const string HeaderSignature = "XSSCache";
        public const int CurrentVersion = 1;

        private static readonly TimeSpan HandleQueueRetryDelay = TimeSpan.FromSeconds(1);

        private ConcurrentBag<ICompletableAction> actionQueue;
        private object locker = new object();
        private bool isTaskRunning = false;
        /// <summary>
        /// Indicate whether the task should be rerun immediately once it ends.
        /// </summary>
        private bool mustRerun = false;

        public string Location { get; }

        public Cache(string location = null)
        {
            Location = location ?? GetDefaultPath();
            actionQueue = new ConcurrentBag<ICompletableAction>();
        }

        public Task StoreAsync(Scrobble scrobble)
        {
            var action = new StoreAction(scrobble);
            return EnqueueAction(action);
        }

        public Task<IReadOnlyCollection<Scrobble>> RetrieveAsync()
        {
            var action = new RetrieveAllAction();
            return EnqueueAction(action);

            //// TODO: we have to remove the parts we read from the files to avoid scrobbling it multiple times.
            //// apparently the only way to remove parts of a file is to rewrite it entirely. 
            //// so we are going to do it the easy way: put it in memory, then delete the file.
            //// to avoid losing data, we could rename the file temporarily ("xxx~"),
            //// and maybe always hold a handle on both the real file and the temporary copy instead of recreating one each time we need to access it?

            //using (var fs = new FileStream(Location, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            //using (var reader = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: false))
            //{
            //    await EnsureCorrectHeaderAndGetFileVersion(fs, reader);

            //    // The header was successfully parsed. We can start reading the data.
            //    while (!reader.EndOfStream)
            //    {
            //        var line = await reader.ReadLineAsync();
            //        var scrobble = ScrobbleSerializer.Deserialize(line);
            //    }

            //    var serializedScrobble = ScrobbleSerializer.Serialize(scrobble);
            //    byte[] buffer = Encoding.UTF8.GetBytes(serializedScrobble);
            //    byte[] newLineBuffer = Encoding.UTF8.GetBytes("\n");
            //    await fs.WriteAsync(newLineBuffer, 0, newLineBuffer.Length);
            //    await fs.WriteAsync(buffer, 0, buffer.Length);
            //}

        }

        public Task RemoveScrobblesAsync(IReadOnlyCollection<Scrobble> scrobbles)
        {
            var action = new RemoveScrobblesAction(scrobbles);
            return EnqueueAction(action);

            //// TODO: we have to remove the parts we read from the files to avoid scrobbling it multiple times.
            //// apparently the only way to remove parts of a file is to rewrite it entirely. 
            //// so we are going to do it the easy way: put it in memory, then delete the file.
            //// to avoid losing data, we could rename the file temporarily ("xxx~"),
            //// and maybe always hold a handle on both the real file and the temporary copy instead of recreating one each time we need to access it?

            //using (var fs = new FileStream(Location, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            //using (var reader = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: false))
            //{
            //    await EnsureCorrectHeaderAndGetFileVersion(fs, reader);

            //    // The header was successfully parsed. We can start reading the data.
            //    while (!reader.EndOfStream)
            //    {
            //        var line = await reader.ReadLineAsync();
            //        var scrobble = ScrobbleSerializer.Deserialize(line);
            //    }

            //    var serializedScrobble = ScrobbleSerializer.Serialize(scrobble);
            //    byte[] buffer = Encoding.UTF8.GetBytes(serializedScrobble);
            //    byte[] newLineBuffer = Encoding.UTF8.GetBytes("\n");
            //    await fs.WriteAsync(newLineBuffer, 0, newLineBuffer.Length);
            //    await fs.WriteAsync(buffer, 0, buffer.Length);
            //}

        }

        private Task<T> EnqueueAction<T>(CacheAction<T> action)
        {
            lock (locker)
            {
                actionQueue.Add(action);
            }
            StartQueueHandlingTask();
            return action.CompletionSource.Task;
        }

        private void StartQueueHandlingTask(bool continuation = false)
        {
            lock (locker)
            {
                // This is a continuation callback, meaning the task just ended.
                if (continuation) isTaskRunning = false;

                if (isTaskRunning)
                {
                    // If the task is already running, we only set a value indicating new data where enqueued
                    // so the task will have to be rerun once it ends.
                    mustRerun = true;
                }
                else
                {
                    // If this is not a continuation, whe start a new task in all cases.
                    // If this is a continuation, mustRerun must be set.
                    if (!continuation || (continuation && mustRerun))
                    {
                        // Start a new task.
                        isTaskRunning = true;
                        try
                        {
                            Task.Run(HandleActionQueueAsync)
                                // Continue recursively with the current method, to handle a possible rerun.
                                .ContinueWith(t => StartQueueHandlingTask(continuation: true),
                                TaskContinuationOptions.RunContinuationsAsynchronously);
                            mustRerun = false;
                        }
                        catch (Exception)
                        {
                            isTaskRunning = false;
                            throw;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// This method runs on a new thread.
        /// It's started when an action is added to the queue.
        /// </summary>
        private async Task HandleActionQueueAsync()
        {
            // First, try to acquire an exclusive lock on the file.
            FileStream fs;
            try
            {
                fs = new FileStream(Location, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (Exception)
            {
                // TODO: log exceptions not caused by the file being locked, since they are unlikely to resolve themselves.
                // TODO: and release the waiting tasks in the queue by failing them.

                // Wait a little, hoping the file will be released...
                await Task.Delay(HandleQueueRetryDelay);
                lock (locker)
                {
                    // And indicate we should retry.
                    mustRerun = true;
                    return;
                }
            }

            // We have our file lock!
            // We can now handle the queued actions...

            // (But before, let's check the file header)
            try
            {
                await EnsureCorrectHeaderAndGetFileVersionAsync(fs);
            }
            catch (Exception ex)
            {
                // TODO: log wrong header exceptions.

                // We cannot cache, so to avoid blocking scrobbling, we release all the waiting actions
                lock (locker)
                {
                    foreach (var item in actionQueue)
                    {
                        item.TrySetException(ex);
                    }
                }
            }

            // First, store all the queued scrobbles:

            // Note about the file content:
            // The file is line separated.
            // Instead of writing the data followed by a new line, we do the opposite:
            // We always start by writing a new line, followed by the data.
            // This way, we don't risk continuing a corrupted line without a proper line ending.
            // Unreadable lines can then be discarded, and we lose only one record instead of two.

            using (var reader = GetReader(fs))
            {
                // Skip to the end of the file.
                fs.Seek(0, SeekOrigin.End);

                // Now we can start writing the actual data.
                foreach (var storeAction in actionQueue.OfType<StoreAction>())
                {
                    var serializedScrobble = ScrobbleSerializer.Serialize(storeAction.Scrobble);
                    byte[] buffer = Encoding.UTF8.GetBytes(serializedScrobble);
                    byte[] newLineBuffer = Encoding.UTF8.GetBytes("\n");
                    await fs.WriteAsync(newLineBuffer, 0, newLineBuffer.Length);
                    await fs.WriteAsync(buffer, 0, buffer.Length);
                }
            }
        }

        private async Task<int> EnsureCorrectHeaderAndGetFileVersionAsync(FileStream fs)
        {
            using (var reader = GetReader(fs))
            {
                // Read the header (the first line of the file, which is supposed to contain the header signature followed by the file version)
                var header = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(header))
                {
                    // This is a new file, so we write the header now...
                    var headerBuffer = Encoding.UTF8.GetBytes($"{HeaderSignature}{CurrentVersion}");
                    fs.Write(headerBuffer, 0, headerBuffer.Length);
                }
                if (header.StartsWith(HeaderSignature) == false)
                {
                    throw new Exception("The cache file header does not match the expected signature!");
                }
                // Parse the version.
                int version;
                if (int.TryParse(header.Substring(startIndex: HeaderSignature.Length), out version) == false)
                {
                    throw new Exception("Could not read the cache file version!");
                }
                // We only try to handle files with a known version.
                if (version < 1 && version > CurrentVersion)
                {
                    throw new Exception("Unexpected cache file version!");
                }
                return version;
            }
        }

        private static StreamReader GetReader(FileStream fs)
            => new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);

        public static string GetDefaultPath()
        {
            var currentDirectory = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            return Path.Combine(currentDirectory, DefaultFileName);
        }
    }
}
