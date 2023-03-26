using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Signapse.Tests
{
    public abstract class DITestClass
    {
        private ServiceCollection services;
        private ServiceProvider provider;
        protected IServiceScope scope;

        protected DITestClass()
        {
            services = new ServiceCollection();
            provider = services.BuildServiceProvider();
            scope = provider.CreateScope();
        }

        [TestInitialize]
        public void Init()
        {
            services = new ServiceCollection();
            InitServices(services);

            provider = services.BuildServiceProvider();
            scope = provider.CreateScope();
        }

        [TestCleanup]
        public void Cleanup()
        {
            scope.Dispose();
            provider.Dispose();
        }

        public virtual void InitServices(ServiceCollection services) { }
    }
}