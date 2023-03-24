using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signapse.Data;
using Signapse.ServerTests.JsonDatabase;
using System.Net;
using UserDB = Signapse.Services.JsonDatabase<Signapse.Data.User>;

namespace Signapse.Server.JsonDatabase.AuthorizedAdmin
{
    [TestClass]
    public class DeleteTests : JsonDatabaseTest
    {
        [TestMethod]
        public async Task Delete_Self_Succeeds()
        {
            // Prepare the DB
            var otherUserGU = Guid.NewGuid();
            var db = this.Server.WebApp.Services.GetRequiredService<UserDB>();
            db.Items.Add(CreateValidUser(otherUserGU));

            // Make the request
            await Login(AdministratorFlag.Full);
            using var res = await HttpClient.SendRequest(HttpMethod.Delete, $"/api/v1/user/{UserGU}");

            Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
        }

        [TestMethod]
        public async Task Admin_Delete_Other_User_Succeeds()
        {
            // Prepare the DB
            var otherUserGU = Guid.NewGuid();
            var db = this.Server.WebApp.Services.GetRequiredService<UserDB>();
            db.Items.Add(CreateValidUser(otherUserGU));

            // Make the request
            await Login(AdministratorFlag.Full);
            using var res = await HttpClient.SendRequest(HttpMethod.Delete, $"/api/v1/user/{otherUserGU}");

            Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
        }

        [TestMethod]
        public async Task Delete_Unknown_User_Returns_NotFound()
        {
            // Prepare the DB
            var otherUserGU = Guid.NewGuid();
            var db = this.Server.WebApp.Services.GetRequiredService<UserDB>();
            db.Items.Add(CreateValidUser(otherUserGU));

            // Make the request
            await Login(AdministratorFlag.Full);
            using var res = await HttpClient.SendRequest(HttpMethod.Delete, $"/api/v1/user/{Guid.NewGuid()}");

            Assert.AreEqual(HttpStatusCode.NotFound, res.StatusCode);
        }
    }
}