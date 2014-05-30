using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Owin.WebSocket.Extensions
{
    // Allows serial queuing of Task instances
    // The tasks are not called on the current synchronization context
    public sealed class TaskQueue
    {
        private readonly object mLockObj = new object();
        private Task mLastQueuedTask;
        private volatile bool mDrained;
        private readonly int? mMaxSize;
        private long mSize;

        public long Size { get { return mSize;} }

        public TaskQueue()
            : this(TaskAsyncHelper.Empty)
        {
        }

        public TaskQueue(Task initialTask)
        {
            mLastQueuedTask = initialTask;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is shared code")]
        public Task Enqueue<T>(Func<T, Task> taskFunc, T state)
        {
            // Lock the object for as short amount of time as possible
            lock (mLockObj)
            {
                if (mDrained)
                {
                    return mLastQueuedTask;
                }

                if (mMaxSize != null)
                {
                    // Increment the size if the queue
                    if (Interlocked.Increment(ref mSize) > mMaxSize)
                    {
                        Interlocked.Decrement(ref mSize);

                        // We failed to enqueue because the size limit was reached
                        return null;
                    }
                }

                Task newTask = mLastQueuedTask.Then((next, nextState) =>
                {
                    return next(nextState).Finally(s =>
                    {
                        var queue = (TaskQueue)s;
                        if (queue.mMaxSize != null)
                        {
                            // Decrement the number of items left in the queue
                            Interlocked.Decrement(ref queue.mSize);
                        }
                    },
                    this);
                },
                taskFunc, state);

                mLastQueuedTask = newTask;
                return newTask;
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is shared code")]
        public Task Drain()
        {
            lock (mLockObj)
            {
                mDrained = true;

                return mLastQueuedTask;
            }
        }
    }
}