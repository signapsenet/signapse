using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Signapse.Exceptions;
using Signapse.RequestData;
using Signapse.Server.Middleware;
using Signapse.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Signapse.Middleware
{
    [DataFor("/install.html")]
    public class InstallDataModel : AffiliateMustacheData, IWebRequest
    {
        public InstallDataModel(AppConfig appConfig, JsonDatabase<Data.User> db)
        {
            this.SMTP = appConfig.SMTP;
            this.SiteName = appConfig.SiteName;
            this.NetworkName = appConfig.NetworkName;

            if (db[Guid.Empty] is Data.User user)
            {
                this.Email = user.Email;
            }
        }

        [JsonConstructor]
        public InstallDataModel()
        {
            this.SMTP = new AppConfig.SMTPOptions();
        }

        public AppConfig.SMTPOptions SMTP { get; set; }

        public string SiteName { get; set; } = string.Empty;
        public string NetworkName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public bool SendSignapseRequest { get; set; } = true;
    }

    public class InstallPageHandler
    {
        readonly AppConfig appConfig;
        readonly JsonDatabase<Data.User> db;
        readonly PasswordHasher<Data.User> hasher;
        readonly EmailProvider emailProvider;
        readonly IHttpContextAccessor contextAccessor;
        readonly ISecureStorage storage;

        public InstallPageHandler(AppConfig appConfig, JsonDatabase<Data.User> db, PasswordHasher<Data.User> hasher, EmailProvider emailProvider, ISecureStorage storage, IHttpContextAccessor contextAccessor)
            => (this.appConfig, this.db, this.hasher, this.emailProvider, this.storage, this.contextAccessor) = (appConfig, db, hasher, emailProvider, storage, contextAccessor);

        public async Task ProcessInstallation(WebRequest<InstallDataModel> request)
        {
            var installData = request.Data ?? throw new HttpBadRequest("Invalid Request");

            if (appConfig.IsInstalled())
                throw new HttpBadRequest("Already Installed");

            installData.CopyPropertiesTo(appConfig,
                nameof(AppConfig.SiteName),
                nameof(AppConfig.NetworkName),
                nameof(AppConfig.Email));

            installData.SMTP.CopyPropertiesTo(appConfig.SMTP,
                nameof(AppConfig.SMTP.Address),
                nameof(AppConfig.SMTP.User),
                nameof(AppConfig.SMTP.Password),
                nameof(AppConfig.SMTP.ReplyTo));

            if (!string.IsNullOrWhiteSpace(installData.Password))
            {
                if (installData.Password != installData.ConfirmPassword)
                    throw new UserError<ArgumentException>("Passwords Must Match");
                if (string.IsNullOrEmpty(installData.Email))
                    throw new UserError<ArgumentException>("Invalid Admin Email");

                // Add the admin user
                if (false == db.Items.FirstOrDefault(it => it.ID.Equals(Guid.Empty)) is Data.User adminUser)
                {
                    adminUser = new Data.User()
                    {
                        ID = Data.User.PrimaryAdminGU,
                        Name = "Admin",
                        AdminFlags = Data.AdministratorFlag.Full
                    };
                    db.Items.Add(adminUser);
                }
                adminUser.Email = installData.Email;
                adminUser.Password = hasher.HashPassword(adminUser, installData.Password);
            }

            // Generate the necessary RSA key pairs
            {
                using RSACryptoServiceProvider RSA = new RSACryptoServiceProvider();
                string xml = RSA.ToXmlString(true);
                await storage.WriteFile("signature_keys.xml", xml);
            }

            await db.Save();
            appConfig.Save();

            // Log the user in immediately
            {
                if (appConfig.IsInstalled()
                    && contextAccessor.HttpContext != null
                    && db.Items.FirstOrDefault(it => it.ID.Equals(Data.User.PrimaryAdminGU)) is Data.User adminUser)
                {
                    _ = emailProvider.SendVerification(adminUser);
                    await contextAccessor.HttpContext.SignInAsync(Claims.CreatePrincipal(adminUser, "cookie"));
                }
            }
        }
    }
}
