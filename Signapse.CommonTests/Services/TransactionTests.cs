using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signapse.Data;
using Signapse.Test;
using Signapse.Tests;

namespace Signapse.Services.Tests
{
    [TestClass]
    public class TransactionTests : DITestClass
    {
        public override void InitServices(ServiceCollection services)
        {
            services.AddSingleton<ISecureStorage, MockStorage>()
                .AddSingleton<IAppDataStorage, MockStorage>()
                .AddSingleton(typeof(JsonDatabase<>))
                .AddScoped(typeof(Transaction<>))
                .AddTransient<JsonSerializerFactory>();

            base.InitServices(services);
        }

        [TestMethod]
        public async Task Commit_Only_Inserts_Once()
        {
            var db = this.scope.ServiceProvider.GetRequiredService<JsonDatabase<TestData>>();

            using (var transaction = new Transaction<TestData>(db))
            {
                transaction.Insert(new TestData());
                await transaction.Commit();
                await transaction.Commit();
            }

            Assert.AreEqual(1, db.Items.Count);
        }

        [TestMethod]
        public void Rollback_Does_Not_Commit_Transaction()
        {
            var db = this.scope.ServiceProvider.GetRequiredService<JsonDatabase<TestData>>();

            using (var transaction = new Transaction<TestData>(db))
            {
                transaction.Insert(new TestData());
                transaction.Rollback();
            }

            Assert.AreEqual(0, db.Items.Count);
        }

        [TestMethod]
        public void Dispose_Commits_Transaction()
        {
            var db = this.scope.ServiceProvider.GetRequiredService<JsonDatabase<TestData>>();

            using (var transaction = new Transaction<TestData>(db))
            {
                transaction.Insert(new TestData());
            }

            Assert.AreEqual(1, db.Items.Count);
        }

        [TestMethod]
        public async Task Does_Not_Affect_DB_Without_Commit()
        {
            var db = this.scope.ServiceProvider.GetRequiredService<JsonDatabase<TestData>>();

            using (var transaction = new Transaction<TestData>(db))
            {
                transaction.Insert(new TestData());

                Assert.AreEqual(0, db.Items.Count);
                await transaction.Commit();
                Assert.AreEqual(1, db.Items.Count);
            }
        }

        [TestMethod]
        public async Task Finds_Items_From_Other_Committed_Transactions()
        {
            var db = this.scope.ServiceProvider.GetRequiredService<JsonDatabase<TestData>>();

            using (var transaction = new Transaction<TestData>(db))
            {
                transaction.Insert(new TestData() { ID = Guid.NewGuid() });
                using (var t2 = new Transaction<TestData>(db))
                {
                    t2.Insert(new TestData());
                    await t2.Commit();
                }

                Assert.AreEqual(1, db.Items.Count);
                Assert.IsNotNull(transaction[Guid.Empty]);
                Assert.AreEqual(2, transaction.Count());
            }
        }

        [TestMethod]
        public void Retrieves_Uncommitted_Items()
        {
            var db = this.scope.ServiceProvider.GetRequiredService<JsonDatabase<TestData>>();

            using (var transaction = new Transaction<TestData>(db))
            {
                transaction.Insert(new TestData());

                Assert.IsNotNull(transaction[Guid.Empty]);
                Assert.AreEqual(1, transaction.Count());
            }
        }

        [TestMethod]
        public void Cannot_Retrieve_Deleted_Items()
        {
            var db = this.scope.ServiceProvider.GetRequiredService<JsonDatabase<TestData>>();

            using (var transaction = new Transaction<TestData>(db))
            {
                transaction.Insert(new TestData());
                transaction.Delete(Guid.Empty);

                Assert.IsNull(transaction[Guid.Empty]);
                Assert.AreEqual(0, transaction.Count());
            }
        }

        [TestMethod]
        public void Editing_Specific_Item_Causes_Update()
        {
            var db = this.scope.ServiceProvider.GetRequiredService<JsonDatabase<TestData>>();
            db.Items.Add(new TestData());

            using (var transaction = new Transaction<TestData>(db))
            {
                if (transaction[Guid.Empty] is TestData item)
                {
                    item.Value = 1;
                }
            }

            Assert.AreEqual(1, db.Items[0].Value);
        }

        private class TestData : IDatabaseEntry
        {
            public Guid ID { get; set; } = Guid.Empty;
            public int Value { get; set; } = 0;
        }
    }
}