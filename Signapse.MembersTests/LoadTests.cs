using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

namespace Signapse.Tests
{
    [TestClass]
    public class LoadTests
    {
        [TestMethod]
        public async Task Semaphore_Vs_Lock()
        {
            const int ITERATIONS = 1000;
            const int THREADS = 12;

            {
                Stopwatch sw = Stopwatch.StartNew();
                List<int> items = new List<int>();
                for (int i = 0; i < ITERATIONS; i++)
                {
                    List<Task> tasks = new List<Task>();
                    for (int j = 0; j < THREADS; j++)
                    {
                        tasks.Add(Task.Run(() =>
                        {
                            lock (items)
                            {
                                items.Add(i * j);
                            }
                        }));
                    }
                    Task.WaitAll(tasks.ToArray());
                }
                Console.WriteLine($"Lock Ticks: {sw.ElapsedTicks}");
            }

            {
                using SemaphoreSlim s = new SemaphoreSlim(1, 1);

                Stopwatch sw = Stopwatch.StartNew();
                List<int> items = new List<int>();
                for (int i = 0; i < ITERATIONS; i++)
                {
                    List<Task> tasks = new List<Task>();
                    for (int j = 0; j < THREADS; j++)
                    {
                        tasks.Add(Task.Run(() =>
                        {
                            s.Wait();
                            items.Add(i * j);
                            s.Release();
                        }));
                    }
                    Task.WaitAll(tasks.ToArray());
                }
                Console.WriteLine($"Semaphore Ticks: {sw.ElapsedTicks}");
            }

            {
                using SemaphoreSlim s = new SemaphoreSlim(1, 1);

                Stopwatch sw = Stopwatch.StartNew();
                List<int> items = new List<int>();
                for (int i = 0; i < ITERATIONS; i++)
                {
                    List<Task> tasks = new List<Task>();
                    for (int j = 0; j < THREADS; j++)
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            await s.WaitAsync();
                            items.Add(i * j);
                            s.Release();
                        }));
                    }
                    Task.WaitAll(tasks.ToArray());
                }
                Console.WriteLine($"Async Semaphore Ticks: {sw.ElapsedTicks}");
            }

            await Task.CompletedTask;
        }
    }
}