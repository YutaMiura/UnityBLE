using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using UnityEngine;

namespace UnityBLE.Android
{
    /// <summary>
    /// Marshals Android JNI calls onto the Unity main thread.
    ///
    /// Unity's <c>AndroidJavaObject</c> / <c>AndroidJavaClass</c> calls require a
    /// JVM-attached thread, and Unity attaches only its main thread. Invoked from a
    /// .NET thread-pool thread (e.g. the continuation of an
    /// <c>await ....ConfigureAwait(false)</c>), the JNI call silently returns the
    /// default value WITHOUT executing the Java method and WITHOUT throwing — so BLE
    /// operations (subscribe, write, scan…) fail invisibly and the code believes they
    /// succeeded. Every outgoing JNI call in this plugin must therefore be issued on
    /// the main thread; this dispatcher guarantees that.
    /// </summary>
    internal static class UnityBleMainThread
    {
        // Safety-net cap for the synchronous wait below (see Run). Generous: real
        // main-thread dispatch completes within a frame; this only prevents a
        // permanent hang if the player loop has stopped pumping during shutdown.
        private const int MainThreadWaitTimeoutMs = 10000;

        private static SynchronizationContext _context;
        private static int _mainThreadId = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Capture()
        {
            // Runs on the Unity main thread, which carries the UnitySynchronizationContext.
            _context = SynchronizationContext.Current;
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        // True when calling directly is safe: either we are already on the captured
        // main thread, or no context was captured (e.g. running outside the player
        // loop, such as Edit-mode tests) in which case we preserve prior behavior.
        private static bool CanRunInline =>
            _context == null || Thread.CurrentThread.ManagedThreadId == _mainThreadId;

        /// <summary>
        /// Runs <paramref name="action"/> on the Unity main thread, blocking the
        /// caller until it completes. Exceptions are re-thrown on the caller.
        /// </summary>
        public static void Run(Action action)
        {
            if (action == null) return;

            if (CanRunInline)
            {
                action();
                return;
            }

            // Post (always supported by UnitySynchronizationContext; Send's
            // cross-thread behavior is version-dependent) and block until the main
            // thread has executed it, re-throwing any exception on the caller.
            ExceptionDispatchInfo captured = null;
            using var done = new ManualResetEventSlim(false);
            _context.Post(_ =>
            {
                try { action(); }
                catch (Exception e) { captured = ExceptionDispatchInfo.Capture(e); }
                finally { done.Set(); }
            }, null);
            // Bounded wait: the main thread normally drains the posted callback within
            // a frame. The timeout is a safety net so a shutdown where the main loop
            // has stopped pumping (e.g. a background-thread Dispose) cannot hang forever.
            if (!done.Wait(MainThreadWaitTimeoutMs))
            {
                Debug.LogWarning("[UnityBleMainThread] Timed out waiting for the main thread to run a JNI call (is the player loop still pumping?).");
                return;
            }
            captured?.Throw();
        }

        /// <summary>
        /// Runs <paramref name="func"/> on the Unity main thread and returns its
        /// result, blocking the caller until it completes.
        /// </summary>
        public static T Run<T>(Func<T> func)
        {
            T result = default;
            Run(() => { result = func(); });
            return result;
        }
    }
}
