using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.Formats.Asn1.AsnWriter;

namespace Signapse.Tests
{
    abstract public class DITestClass
    {
        ServiceCollection services;
        ServiceProvider provider;
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

        virtual public void InitServices(ServiceCollection services) { }
    }
}