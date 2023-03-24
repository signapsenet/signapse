using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signapse.Data;
using Signapse.ServerTests.JsonDatabase;
using System.Net;
using UserDB = Signapse.Services.JsonDatabase<Signapse.Data.User>;

namespace Signapse.Server.JsonDatabase.Unauthorized
{
    [TestClass]
    public class DeleteTests : JsonDatabaseTest
    {
        [TestMethod]
        public async Task Delete_Existing_User_Returns_Unauthorized()
        {
            // Prepare the DB
            var db = this.Server.WebApp.Services.GetRequiredService<UserDB>();
            db.Items.Add(CreateValidUser(UserGU));

            // Make the request
            using var res = await HttpClient.SendRequest(HttpMethod.Delete, $"/api/v1/user/{UserGU}");

            Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
        }

        [TestMethod]
        public async Task Delete_Unknown_User_Returns_Unauthorized()
        {
            // Make the request
            using var res = await HttpClient.SendRequest(HttpMethod.Delete, $"/api/v1/user/{UserGU}");

            Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
        }
    }
}