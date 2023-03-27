using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signapse.Data;
using Signapse.ServerTests.JsonDatabase;
using System.Net;
using UserDB = Signapse.Services.JsonDatabase<Signapse.Data.User>;

namespace Signapse.Server.JsonDatabase.Unauthorized
{
    [TestClass]
    public class UpdateTests : JsonDatabaseTest
    {
        [TestMethod]
        public async Task Update_Existing_User_Returns_Unauthorized()
        {
            // Prepare the DB
            var db = this.Server.WebApp.Services.GetRequiredService<UserDB>();
            db.Items.Add(CreateValidUser(UserGU));

            // Make the request
            using var res = await HttpClient.SendRequest(HttpMethod.Put, "/api/v1/user", CreateValidUser(UserGU));

            Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
        }

        [TestMethod]
        public async Task Update_Invalid_User_Returns_Unauthorized()
        {
            using var res = await HttpClient.SendRequest(HttpMethod.Put, "/api/v1/user", new Data.User()
            {
                ID = UserGU
            });

            Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
        }

        [TestMethod]
        public async Task Update_Unknown_User_Returns_Unauthorized()
        {
            using var res = await HttpClient.SendRequest(HttpMethod.Put, "/api/v1/user", CreateValidUser());

            Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
        }
    }
}