using System;
using System.Threading;

namespace Signapse.Services
{
    public class SemaphorSlimLock : IDisposable
    {
        readonly SemaphoreSlim semaphore;
        public SemaphorSlimLock(SemaphoreSlim semaphore)
        {
            this.semaphore = semaphore;
            semaphore.Wait();
        }

        public void Dispose()
        {
            semaphore.Release();
        }
    }
}
