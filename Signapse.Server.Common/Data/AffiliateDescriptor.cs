using System;
using System.Net;

namespace Signapse.Data
{
    public interface IAffiliateDescriptor : IDatabaseEntry
    {
        Guid ID { get; }
        string Name { get; }
        string Network { get; }
        Uri Uri { get; }
        RSAParametersSerializable RSAPublicKey { get; }
    }

    public class SignapseServerDescriptor : IDatabaseEntry, IAffiliateDescriptor
    {
        public Guid ID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Network { get; set; } = string.Empty;
        public Uri WebServerUri { get; set; }
        public Uri AffiliateServerUri { get; set; }
        public RSAParametersSerializable RSAPublicKey { get; set; } = new RSAParametersSerializable();

        public Uri Uri
        {
            get => this.WebServerUri;
            set => this.WebServerUri = value;
        }

        public SignapseServerDescriptor()
            => (this.ID, this.WebServerUri, this.AffiliateServerUri) = (Guid.NewGuid(), new Uri($"http://{IPAddress.Loopback}"), new Uri($"http://{IPAddress.Loopback}"));

        public SignapseServerDescriptor(Guid id, String name, Uri signapseServerUri, Uri affiliateUri)
            => (this.ID, this.Name, this.AffiliateServerUri, this.WebServerUri) = (id, name, signapseServerUri, affiliateUri);
    }
}
