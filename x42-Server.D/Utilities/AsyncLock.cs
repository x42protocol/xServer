using System;
using System.Threading;
using System.Threading.Tasks;

namespace X42.Utilities
{
    /// <summary>
    ///     An async synchronization primitive that allows the caller to await inside the critical section.
    ///     <para>
    ///         The lock is disposable, which allows the caller to use the convenient <c>using</c> statement
    ///         and avoid caring about releasing the lock.
    ///     </para>
    /// </summary>
    /// <example>
    ///     The lock can be used in async environment:
    ///     <code>
    /// private AsyncLock asyncLock = new AsyncLock();
    /// ...
    /// using (await asyncLock.LockAsync(cancellationToken))
    /// {
    ///     // Body of critical section.
    ///     ...
    ///     // Unlocking is automatic in Dispose method invoked by using statement.
    /// }
    /// </code>
    ///     <para>
    ///         or it can be used in non-async environment:
    ///     </para>
    ///     <code>
    /// using (asyncLock.Lock(cancellationToken))
    /// {
    ///     // Body of critical section.
    ///     ...
    ///     // Unlocking is again automatic in Dispose method invoked by using statement.
    /// }
    /// </code>
    /// </example>
    /// <remarks>Based on https://www.hanselman.com/blog/ComparingTwoTechniquesInNETAsynchronousCoordinationPrimitives.aspx .</remarks>
    public sealed class AsyncLock : IDisposable
    {
        /// <summary>
        ///     Helper object that allows implementation of disposable lock for convenient use with <c>using</c> statement.
        ///     <para>This releaser is used when the lock has been acquired and disposing it will release the lock.</para>
        /// </summary>
        /// <remarks>We use the disposable interfaced in a task here to avoid allocations on acquiring the lock when it is free.</remarks>
        private readonly Task<IDisposable> releaser;

        /// <summary>Internal synchronization primitive used as a mutex to only allow one thread to occupy the critical section.</summary>
        private readonly SemaphoreSlim semaphore;

        /// <summary>
        ///     Initializes an instance of the object.
        /// </summary>
        public AsyncLock()
        {
            semaphore = new SemaphoreSlim(1, 1);
            releaser = Task.FromResult<IDisposable>(new Releaser(this));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            semaphore.Dispose();
        }

        /// <summary>
        ///     Acquires the lock.
        /// </summary>
        /// <param name="cancel">Cancellation token that can be used to abort waiting for the lock.</param>
        /// <returns>Disposable interface to enable using construct. Disposing it releases the lock.</returns>
        /// <exception cref="OperationCanceledException">
        ///     Thrown when the <paramref name="cancel" /> is triggered and the lock is
        ///     not acquired.
        /// </exception>
        public Task<IDisposable> LockAsync(CancellationToken cancel = default)
        {
            var wait = semaphore.WaitAsync(cancel);

            // If the lock is available, quickly return.
            if (wait.IsCompleted)
            {
                // We only hold the lock if the task was completed successfully.
                // If the task was cancelled, we don't hold the lock and we need to throw.
                if (wait.IsCanceled) throw new OperationCanceledException();

                return releaser;
            }

            // If the lock is not available, we wait until it is available
            // or the wait is cancelled.
            return wait.ContinueWith((task, state) =>
            {
                // We only hold the lock if the task was completed successfully.
                // If the task was cancelled, we don't hold the lock and we need to throw.
                if (task.IsCanceled) throw new OperationCanceledException();

                return (IDisposable) state;
            }, releaser.Result, cancel, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        /// <summary>
        ///     Acquires the lock.
        /// </summary>
        /// <param name="cancel">Cancellation token that can be used to abort waiting for the lock.</param>
        /// <returns>Disposable interface to enable using construct. Disposing it releases the lock.</returns>
        /// <exception cref="OperationCanceledException">
        ///     Thrown when the <paramref name="cancel" /> is triggered and the lock is
        ///     not acquired.
        /// </exception>
        public IDisposable Lock(CancellationToken cancel = default)
        {
            semaphore.Wait(cancel);

            // We are holding the lock here, so we will want unlocking.
            return releaser.Result;
        }

        /// <summary>
        ///     Disposable mechanism that is attached to the parent lock and releases it when it is disposed.
        ///     This allows the user of the lock to use the convenient <c>using</c> statement and avoid
        ///     caring about manual releasing of the lock.
        /// </summary>
        private sealed class Releaser : IDisposable
        {
            /// <summary>
            ///     Parent lock to be released when this releaser is disposed, or <c>null</c> if no action should be taken on
            ///     disposing it.
            /// </summary>
            private readonly AsyncLock toRelease;

            /// <summary>
            ///     Connects the releaser with its parent lock.
            /// </summary>
            internal Releaser(AsyncLock toRelease)
            {
                this.toRelease = toRelease;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                toRelease.semaphore.Release();
            }
        }
    }
}