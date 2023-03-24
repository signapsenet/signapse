using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signapse.Services;
using Signapse.Test;
using Signapse.Tests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signapse.Services.Tests
{
    [TestClass()]
    public class CryptographyTests : DITestClass
    {
        const string DATA_STR = "There is an oak in the middle of the road.  Will someone rid me of this loathsome oak?";

        public override void InitServices(ServiceCollection services)
        {
            services.AddSingleton<Cryptography>()
                .AddTransient<JsonSerializerFactory>()
                .AddTransient<ISecureStorage, MockStorage>()
                .AddTransient<IAppDataStorage, MockStorage>();
        }

        [TestMethod()]
        public void Can_Encrypt_And_Decrypt()
        {
            var crypto = scope.ServiceProvider.GetRequiredService<Cryptography>();

            var data = string.Join(' ', Enumerable.Range(0, 20).Select(i => DATA_STR));
            var encrypted = crypto.Encrypt(data);
            var decrypted = crypto.Decrypt(encrypted);

            Assert.AreNotEqual(data, encrypted);
            Assert.AreEqual(data, decrypted);
        }
    }
}