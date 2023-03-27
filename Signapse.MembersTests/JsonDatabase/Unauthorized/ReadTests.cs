using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signapse.Data;
using Signapse.ServerTests.JsonDatabase;
using Signapse.Test;
using System.Net;
using UserDB = Signapse.Services.JsonDatabase<Signapse.Data.User>;

namespace Signapse.Server.JsonDatabase.Unauthorized
{
    [TestClass]
    public class ReadTests : JsonDatabaseTest
    {
        [TestMethod]
        public async Task Read_Existing_User_Succeeds()
        {
            await Login(AdministratorFlag.Full);

            // Prepare the DB
            var db = this.Server.WebApp.Services.GetRequiredService<UserDB>();
            db.Items.Add(CreateValidUser(UserGU));

            // Make the request
            using var res = await HttpClient.SendRequest(HttpMethod.Get, $"/api/v1/user/{UserGU}");

            Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
        }

        [TestMethod]
        public async Task Read_Unknown_User_Returns_NotFound()
        {
            await Login(AdministratorFlag.Full);

            // Make the request
            using var res = await HttpClient.SendRequest(HttpMethod.Get, $"/api/v1/user/{Guid.NewGuid()}");

            Assert.AreEqual(HttpStatusCode.NotFound, res.StatusCode);
        }

        [TestMethod]
        public async Task Read_User_Matches_Source()
        {
            await Login(AdministratorFlag.Full);

            // Prepare the DB
            var user = CreateValidUser(UserGU);
            var db = this.Server.WebApp.Services.GetRequiredService<UserDB>();
            db.Items.Add(user);

            // Make the request
            var resUser = await HttpClient.SendRequest<Data.User>(HttpMethod.Get, $"/api/v1/user/{UserGU}")
                ?? throw new Exception("User Not Found");

            var authResults = new TestAuthResults(true, AdministratorFlag.Full);
            var origUserJson = user.ApplyPolicyAccess(authResults).Serialize();
            var resUserJson = resUser.Serialize();
            Assert.AreEqual(origUserJson, resUserJson);
        }

    }
}
