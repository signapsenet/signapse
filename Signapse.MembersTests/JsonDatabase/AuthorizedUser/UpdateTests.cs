using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signapse.Data;
using Signapse.ServerTests.JsonDatabase;
using System.Net;
using UserDB = Signapse.Services.JsonDatabase<Signapse.Data.User>;

namespace Signapse.Server.JsonDatabase.AuthorizedUser
{
    [TestClass]
    public class UpdateTests : JsonDatabaseTest
    {
        [TestMethod]
        public async Task Update_Self_Succeeds()
        {
            var user = await Login(AdministratorFlag.User);

            // Make the request
            using var res = await HttpClient.SendRequest(HttpMethod.Put, "/api/v1/user", user);

            Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
        }

        [TestMethod]
        public async Task Update_Self_AdminFlags_Returns_BadRequest()
        {
            var user = await Login(AdministratorFlag.User);

            // Make the request
            user.AdminFlags = AdministratorFlag.Full;
            using var res = await HttpClient.SendRequest(HttpMethod.Put, "/api/v1/user", user);

            Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
        }

        [TestMethod]
        public async Task Update_Other_User_Returns_Unauthorized()
        {
            var user = await Login(AdministratorFlag.None);

            // Prepare the DB
            user = CreateValidUser(Guid.NewGuid());
            var db = this.Server.WebApp.Services.GetRequiredService<UserDB>();
            db.Items.Add(user);

            // Make the request
            using var res = await HttpClient.SendRequest(HttpMethod.Put, "/api/v1/user", user);

            Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
        }

        [TestMethod]
        public async Task Admin_Update_Other_User_Succeeds()
        {
            var user = await Login(AdministratorFlag.User);

            // Prepare the DB
            user = CreateValidUser(Guid.NewGuid());
            var db = this.Server.WebApp.Services.GetRequiredService<UserDB>();
            db.Items.Add(user);

            // Make the request
            using var res = await HttpClient.SendRequest(HttpMethod.Put, "/api/v1/user", user);

            Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
        }

        [TestMethod]
        public async Task Update_Unknown_User_Returns_NotFound()
        {
            var user = await Login(AdministratorFlag.User);

            // Make the request
            user = CreateValidUser(Guid.NewGuid());
            using var res = await HttpClient.SendRequest(HttpMethod.Put, "/api/v1/user", user);

            Assert.AreEqual(HttpStatusCode.NotFound, res.StatusCode);
        }
    }
}