using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signapse.Data;
using Signapse.ServerTests.JsonDatabase;
using Signapse.Test;
using System.Net;

namespace Signapse.Server.JsonDatabase.AuthorizedAdmin
{
    [TestClass]
    public class ReadTests : JsonDatabaseTest
    {
        [TestMethod]
        public async Task Read_Existing_User_Succeeds()
        {
            var user = await Login(AdministratorFlag.Full);

            // Make the request
            using var res = await HttpClient.SendRequest(HttpMethod.Get, $"/api/v1/user/{UserGU}");

            Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
        }

        [TestMethod]
        public async Task Read_Unknown_User_Returns_NotFound()
        {
            var user = await Login(AdministratorFlag.Full);

            // Make the request
            using var res = await HttpClient.SendRequest(HttpMethod.Get, $"/api/v1/user/{Guid.NewGuid()}");

            Assert.AreEqual(HttpStatusCode.NotFound, res.StatusCode);
        }

        [TestMethod]
        public async Task Read_User_Matches_Source()
        {
            var user = await Login(AdministratorFlag.Full);

            var resUser = await HttpClient.SendRequest<Data.User>(HttpMethod.Get, $"/api/v1/user/{UserGU}")
                ?? throw new Exception("User Not Found");

            var authResults = new TestAuthResults(true, AdministratorFlag.Full);
            var origUserJson = user.ApplyPolicyAccess(authResults).Serialize();
            var resUserJson = resUser.Serialize();
            Assert.AreEqual(origUserJson, resUserJson);
        }
    }
}