using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signapse.Tests;

namespace Signapse.Services.Tests
{
    [TestClass]
    public class JsonSerializerFactoryTests : DITestClass
    {
        public override void InitServices(ServiceCollection services)
        {
            services.AddTransient<JsonSerializerFactory>();
            services.AddTransient<TestService>();
        }

        [TestMethod]
        public void JsonSerializerFactoryTest()
        {
            var factory = scope.ServiceProvider.GetRequiredService<JsonSerializerFactory>();

            var data = ActivatorUtilities.CreateInstance<TestDI>(scope.ServiceProvider);
            var json = factory.Serialize(data);
            var data2 = factory.Deserialize<TestDI>(json);
            var json2 = factory.Serialize(data2);

            Assert.AreEqual(json, json2);
        }

        private class TestService { }

        private class TestDI
        {
            public TestDI(TestService svc) { }

            public string TestProp { get; set; } = "Test Property";
            public int TestInt { get; set; } = 30;
        }
    }
}