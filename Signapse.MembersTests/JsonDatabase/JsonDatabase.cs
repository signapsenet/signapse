using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signapse.Data;
using Signapse.Server;
using Signapse.Server.Tests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UserDB = Signapse.Services.JsonDatabase<Signapse.Data.User>;

namespace Signapse.ServerTests.JsonDatabase
{
    abstract public class JsonDatabaseTest
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        CancellationTokenSource ctSource;
        protected SignapseWebClient HttpClient { get; private set; }
        protected TestAffiliateServer Server { get; private set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [TestInitialize]
        public void Initialize()
        {
            ctSource = new CancellationTokenSource();

            Server = new TestAffiliateServer();
            Server.Run(ctSource.Token);

            HttpClient = new SignapseWebClient(Server.ServerUri);
        }

        [TestCleanup]
        public void Cleanup()
        {
            HttpClient.Dispose();

            ctSource.Cancel();
            ctSource.Dispose();

            Server.Dispose();
        }

        protected Guid UserGU = Guid.NewGuid();
        protected async Task<Data.User> Login(AdministratorFlag adminFlags)
        {
            // Prepare the DB
            var user = CreateValidUser(UserGU);
            user.AdminFlags = adminFlags;

            // Add this user to the db
            {
                var db = this.Server.WebApp.Services.GetRequiredService<UserDB>();
                var hasher = this.Server.WebApp.Services.GetRequiredService<PasswordHasher<Data.User>>();
                var dbUser = CreateValidUser(UserGU);
                dbUser.AdminFlags = adminFlags;
                dbUser.Password = hasher.HashPassword(dbUser, dbUser.Password);

                db.Items.Add(dbUser);
            }

            var res = await HttpClient.SendRequest(HttpMethod.Post, "/api/v1/login", new
            {
                Data = new { user.Email, user.Password }
            });
            Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);

            return user;
        }

        public Data.User CreateValidUser() => CreateValidUser(Guid.NewGuid());
        public Data.User CreateValidUser(Guid guid) => new User()
        {
            ID = guid,
            Name = "New User",
            Email = $"{guid}@email.com",
            Password = "password123",
            AdminFlags = AdministratorFlag.Full,
        };
    }
}
