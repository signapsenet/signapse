using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Signapse.Services
{
    public abstract class BackgroundService : IDisposable
    {
        private Thread? thread = null;
        private CancellationTokenSource ctSource = new CancellationTokenSource();
        private ManualResetEvent startupCompleted = new ManualResetEvent(false);

        public BackgroundService()
        {
        }

        /// <summary>
        /// Perform the inner loop
        /// </summary>
        protected abstract Task DoWork(CancellationToken token);

        public void Start()
        {
            Stop();

            startupCompleted = new ManualResetEvent(false);

            ctSource = new CancellationTokenSource();
            thread = new Thread(MainLoop);
            thread.Start(ctSource.Token);

            startupCompleted.WaitOne();
        }

        public void Stop()
        {
            startupCompleted?.Dispose();

            ctSource.Cancel();
            thread?.Join(1000);
            ctSource.Dispose();
        }

        void IDisposable.Dispose()
        {
            Stop();
        }

        private async void MainLoop(object? obj)
        {
            if (obj is CancellationToken token)
            {
                startupCompleted.Set();

                while (!token.IsCancellationRequested)
                {
                    await DoWork(token);
                }
            }
        }
    }

    public static class BackgroundServiceExtensions
    {
        public static IServiceCollection AddBackgroundService<T>(this IServiceCollection services)
            where T : BackgroundService
        {
            return services;
        }
    }
}