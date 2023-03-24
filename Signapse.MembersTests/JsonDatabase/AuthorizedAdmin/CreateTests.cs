using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signapse.Data;
using Signapse.ServerTests.JsonDatabase;
using System.Net;

namespace Signapse.Server.JsonDatabase.AuthorizedAdmin
{
    [TestClass]
    public class CreateTests : JsonDatabaseTest
    {
        [TestMethod]
        public async Task Insert_Valid_User_Succeeds()
        {
            var user = await Login(AdministratorFlag.Full);
            using var res = await HttpClient.SendRequest(HttpMethod.Put, "/api/v1/user", CreateValidUser(Guid.Empty));

            Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
        }

        [TestMethod]
        public async Task Insert_Invalid_User_Returns_BadRequest()
        {
            var user = await Login(AdministratorFlag.Full);
            using var res = await HttpClient.SendRequest(HttpMethod.Put, "/api/v1/user", new User());

            Assert.AreEqual(HttpStatusCode.BadRequest, res.StatusCode);
        }
    }
}