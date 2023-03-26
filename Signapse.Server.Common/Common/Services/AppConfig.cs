namespace Signapse.Services
{
    /// <summary>
    /// Public app settings
    /// </summary>
    public class AppConfig
    {
        private readonly IAppDataStorage storage;
        private readonly JsonSerializerFactory JSON;

        public string APIKey { get; set; } = string.Empty;
        public string SiteName { get; set; } = string.Empty;
        public string NetworkName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public SMTPOptions SMTP { get; set; } = new SMTPOptions();

        public string[] Affiliates { get; set; } = { };

        public bool IsInstalled() => !(string.IsNullOrWhiteSpace(this.NetworkName)
            || string.IsNullOrWhiteSpace(SiteName) || string.IsNullOrWhiteSpace(Email)
            || string.IsNullOrWhiteSpace(SMTP.Address) || string.IsNullOrWhiteSpace(SMTP.User)
            || string.IsNullOrWhiteSpace(SMTP.Password) || string.IsNullOrEmpty(SMTP.ReplyTo));

        public AppConfig(IAppDataStorage storage, JsonSerializerFactory jsonFactory)
        {
            this.storage = storage;
            this.JSON = jsonFactory;
        }

        public void Load()
        {
            var json = storage.SecureReadFile("appsettings.json").Result;
            if (JSON.Deserialize<AppConfig>(json) is AppConfig copy)
            {
                copy.CopyPropertiesTo(this);
            }
        }

        public void Save()
        {
            storage.SecureWriteFile("appsettings.json", JSON.Serialize(this));
        }

        public class SMTPOptions
        {
            public string Address { get; set; } = string.Empty;
            public string User { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string ReplyTo { get; set; } = string.Empty;
        }
    }
}