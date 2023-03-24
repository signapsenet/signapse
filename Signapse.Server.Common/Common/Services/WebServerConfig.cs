using Signapse.Services;
using System;

namespace Signapse.Server.Common.Services
{
    public class WebServerConfig
    {
        readonly IAppDataStorage storage;
        readonly JsonSerializerFactory JSON;

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
