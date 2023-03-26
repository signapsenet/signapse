using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signapse.BlockChain;
using Signapse.BlockChain.Transactions;
using Signapse.Data;
using Signapse.Test;
using Signapse.Tests;
using System.Security.Cryptography;

namespace Signapse.Services.Tests
{
    [TestClass]
    public class RSASignerTests : DITestClass
    {
        public override void InitServices(ServiceCollection services)
        {
            services.AddTransient<ISecureStorage, MockStorage>();
            services.AddTransient<IAppDataStorage, MockStorage>();
            services.AddTransient<JsonSerializerFactory>();
            services.AddTransient<RSASigner>();
        }

        [TestMethod]
        public void Verify_Valid_Signature_Succeeds()
        {
            RSASigner signer = scope.ServiceProvider.GetRequiredService<RSASigner>();

            var block = new Block()
            {
                Transaction = new ContentTransaction()
            };
            var signature = signer.Sign(block);

            Assert.AreEqual(true, signer.Verify(signer.PublicKey, block, signature));
        }

        [TestMethod]
        public void Verify_Serialized_Signature_Succeeds()
        {
            var json = scope.ServiceProvider.GetRequiredService<JsonSerializerFactory>();
            var signer = scope.ServiceProvider.GetRequiredService<RSASigner>();

            var block = new Block()
            {
                Transaction = new ContentTransaction()
            };

            var blockToSign = json.Deserialize<Block>(json.Serialize(block))
                ?? throw new Exception("Unable to deserialize Block");

            var signature = signer.Sign(blockToSign);
            var key = json.Deserialize<RSAParametersSerializable>(
                    json.Serialize(new RSAParametersSerializable(signer.PublicKey))
                )
                ?.RSAParameters ?? new RSAParameters();

            Assert.AreEqual(true, signer.Verify(key, block, signature));
        }

        [TestMethod]
        public void Verify_Serialized_Block_Succeeds()
        {
            var json = scope.ServiceProvider.GetRequiredService<JsonSerializerFactory>();
            var signer = scope.ServiceProvider.GetRequiredService<RSASigner>();

            var block = new Block()
            {
                Transaction = new ContentTransaction()
            };
            block.Signatures = new[]
            {
                new Signature()
                {
                    Data = signer.Sign(block)
                }
            };
            block = json.Deserialize<Block>(json.Serialize(block));

            var key = json.Deserialize<RSAParametersSerializable>(
                    json.Serialize(new RSAParametersSerializable(signer.PublicKey))
                )
                ?.RSAParameters ?? new RSAParameters();

            Assert.AreEqual(true, signer.Verify(key, block!, block!.Signatures[0].Data));
        }
    }
}