﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signapse.Data;
using Signapse.ServerTests.JsonDatabase;
using System.Net;

namespace Signapse.Server.JsonDatabase.Unauthorized
{
    [TestClass]
    public class CreateTests : JsonDatabaseTest
    {
        [TestMethod]
        public async Task Insert_Valid_User_Fails()
        {
            using var res = await HttpClient.SendRequest(HttpMethod.Put, "/api/v1/user", CreateValidUser());

            Assert.AreEqual(HttpStatusCode.Forbidden, res.StatusCode);
        }

        [TestMethod]
        public async Task Insert_Invalid_User_Returns_Forbidden()
        {
            using var res = await HttpClient.SendRequest(HttpMethod.Put, "/api/v1/user", new User());

            Assert.AreEqual(HttpStatusCode.Forbidden, res.StatusCode);
        }

        // This is not possible.  Inserting an existing user is just an update request
        //[TestMethod]
        //public async Task Insert_Existing_User_Returns_Forbidden()
        //{
        //    using var res = await HttpClient.SendRequest(HttpMethod.Put, "/api/v1/user", new User());

        //    Assert.AreEqual(HttpStatusCode.Forbidden, res.StatusCode);
        //}
    }
}