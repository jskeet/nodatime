using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace NodaTime.Benchmarks.Custom;

internal abstract class ConcurrencyCommandBase
{
    protected const int ChunkSize = 1000;
    private readonly int threadCount;
    private readonly Duration executionTime;

    protected long iterations;

    protected ConcurrencyCommandBase(int threadCount, Duration executionTime)
    {
        this.threadCount = threadCount;
        this.executionTime = executionTime;
    }

    protected void Execute()
    {
        List<Thread> threads = Enumerable.Range(0, threadCount).Select(_ => new Thread(DoWork)).ToList();
        Instant start = SystemClock.Instance.GetCurrentInstant();
        Instant end = start + executionTime;
        long lastIterations = 0;
        long lastTimestamp = Stopwatch.GetTimestamp();
        threads.ForEach(t => t.Start());
        while (SystemClock.Instance.GetCurrentInstant() < end)
        {
            Thread.Sleep(1000);
            long currentTimestamp = Stopwatch.GetTimestamp();

            long currentIterations = Interlocked.Read(ref iterations);
            long diffIterations = currentIterations - lastIterations;
            long diffTimestamp = currentTimestamp - lastTimestamp;
            long iterationsPerSecond = diffIterations * Stopwatch.Frequency / diffTimestamp;
            Console.WriteLine($"{DateTime.UtcNow:HH:mm:ss}: {iterationsPerSecond:N0}");

            lastTimestamp = currentTimestamp;
            lastIterations = currentIterations;
        }

        // Use a negative number of iterations to signal to threads that they should stop.
        Interlocked.Exchange(ref iterations, long.MinValue);

        threads.ForEach(t => t.Join());
    }

    protected abstract void DoWork();
}
