using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Owin.WebSocket.Extensions;

namespace UnitTests
{
    [TestClass]
    public class TaskQueueTests
    {
        [TestMethod]
        public void TaskQueueSizeTest()
        {
            var depth = 10;
            var taskQueue = new TaskQueue();
            var wait = true;
            for (var i = 0; i < depth; i++)
            {
                taskQueue.Enqueue(t => Task.Run(() =>
                {
                    while(wait)
                        Thread.Sleep(5);
                }), wait);
            }

            taskQueue.Size.Should().Be(depth);
            wait = false;
            Thread.Sleep(25);
            taskQueue.Size.Should().Be(0);
        }

        [TestMethod]
        public void TaskQueueMaxSizeTest()
        {
            var depth = 10;
            var taskQueue = new TaskQueue();
            taskQueue.SetMaxQueueSize(depth);
            var wait = true;
            for (var i = 0; i < depth; i++)
            {
                taskQueue.Enqueue(t => Task.Run(() =>
                {
                    while (wait)
                        Thread.Sleep(5);
                }), wait);
            }

            taskQueue.Size.Should().Be(depth);
            var newItem = taskQueue.Enqueue(t => Task.FromResult(0), wait);
            newItem.Should().BeNull("This should exceed the max and return a null task since it was not enqueued");
            taskQueue.Size.Should().Be(depth);
            
            wait = false;
            Thread.Sleep(25);
            newItem = taskQueue.Enqueue(t => Task.FromResult(0), wait);
            newItem.Should().NotBeNull("Released the queue and it should have space again");
        }

        [TestMethod]
        public void TaskQueueDrainTest()
        {
            var depth = 10;
            var taskQueue = new TaskQueue();
            taskQueue.SetMaxQueueSize(depth);
            var wait = true;
            for (var i = 0; i < depth; i++)
            {
                taskQueue.Enqueue(t => Task.Run(() =>
                {
                    while (wait)
                        Thread.Sleep(10);
                }), wait);
            }

            wait = false;
            taskQueue.Drain();
            taskQueue.Size.Should().Be(0);

            var newItem = taskQueue.Enqueue(t => Task.FromResult(0), wait);
            newItem.Should().NotBeNull();
        }
    }
}
