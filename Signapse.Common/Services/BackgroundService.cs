using Microsoft.Extensions.DependencyInjection;

namespace Signapse.Services
{
    abstract public class BackgroundService : IDisposable
    {
        Thread? thread = null;
        CancellationTokenSource ctSource = new CancellationTokenSource();

        public BackgroundService()
        {
        }

        /// <summary>
        /// Perform the inner loop
        /// </summary>
        abstract protected Task DoWork(CancellationToken token);

        public void Start()
        {
            Stop();

            ctSource = new CancellationTokenSource();
            thread = new Thread(MainLoop);
            thread.Start(ctSource.Token);
        }

        public void Stop()
        {
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