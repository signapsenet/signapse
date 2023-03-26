using Signapse.Services;
using System;

namespace Signapse.Server.Web.Services
{
    public class WebServerConfig
    {
        private readonly IAppDataStorage storage;
        private readonly JsonSerializerFactory JSON;

        public Uri? SignapseServerUri { get; set; }
        public string? SignapseServerAPIKey { get; set; }

        public WebServerConfig(IAppDataStorage storage, JsonSerializerFactory jsonFactory)
        {
            this.storage = storage;
            JSON = jsonFactory;
        }

        public void Load()
        {
            var json = storage.SecureReadFile("websettings.json").Result;
            if (JSON.Deserialize<AppConfig>(json) is AppConfig copy)
            {
                copy.CopyPropertiesTo(this);
            }
        }

        public void Save()
        {
            storage.SecureWriteFile("websettings.json", JSON.Serialize(this));
        }
    }
}
