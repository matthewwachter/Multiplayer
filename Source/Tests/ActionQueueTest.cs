using Multiplayer.Common;

namespace Tests;

public class ActionQueueTest
{
    [Test]
    public void ActionsExecuteInOrder()
    {
        var queue = new ActionQueue();
        var results = new List<int>();

        queue.Enqueue(() => results.Add(1));
        queue.Enqueue(() => results.Add(2));
        queue.Enqueue(() => results.Add(3));

        queue.RunQueue(_ => { });

        Assert.That(results, Is.EqualTo(new List<int> { 1, 2, 3 }));
    }

    [Test]
    public void ExceptionDoesNotStopRemainingActions()
    {
        var queue = new ActionQueue();
        var results = new List<int>();

        queue.Enqueue(() => results.Add(1));
        queue.Enqueue(() => throw new InvalidOperationException("test error"));
        queue.Enqueue(() => results.Add(3));

        queue.RunQueue(_ => { });

        Assert.That(results, Is.EqualTo(new List<int> { 1, 3 }));
    }

    [Test]
    public void MultipleExceptionsAllLogged()
    {
        var queue = new ActionQueue();
        var errors = new List<string>();

        queue.Enqueue(() => throw new InvalidOperationException("error1"));
        queue.Enqueue(() => throw new ArgumentException("error2"));

        queue.RunQueue(msg => errors.Add(msg));

        Assert.That(errors, Has.Count.EqualTo(2));
        Assert.That(errors[0], Does.Contain("error1"));
        Assert.That(errors[1], Does.Contain("error2"));
    }

    [Test]
    public void NoStaleActionsAfterRunQueue()
    {
        var queue = new ActionQueue();
        var results = new List<int>();

        queue.Enqueue(() => results.Add(1));
        queue.RunQueue(_ => { });

        results.Clear();
        queue.RunQueue(_ => { });

        Assert.That(results, Is.Empty);
    }

    [Test]
    public void ThreadSafeEnqueue()
    {
        var queue = new ActionQueue();
        var count = 0;
        var countLock = new object();

        var threads = Enumerable.Range(0, 10).Select(_ => new Thread(() =>
        {
            for (int i = 0; i < 100; i++)
                queue.Enqueue(() => { lock (countLock) count++; });
        })).ToList();

        foreach (var t in threads) t.Start();
        foreach (var t in threads) t.Join();

        queue.RunQueue(_ => { });

        Assert.That(count, Is.EqualTo(1000));
    }

    [Test]
    public void ActionsEnqueuedDuringExecutionRunOnNextCycle()
    {
        var queue = new ActionQueue();
        var results = new List<int>();

        queue.Enqueue(() =>
        {
            results.Add(1);
            queue.Enqueue(() => results.Add(2));
        });

        queue.RunQueue(_ => { });
        Assert.That(results, Is.EqualTo(new List<int> { 1 }));

        queue.RunQueue(_ => { });
        Assert.That(results, Is.EqualTo(new List<int> { 1, 2 }));
    }
}
