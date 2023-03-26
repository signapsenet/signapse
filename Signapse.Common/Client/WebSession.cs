using Signapse.Services;

namespace Signapse.Client
{
    /// <summary>
    /// Communication endpoints from clients to the site's Webserver
    /// </summary>
    public partial class WebSession: HttpSession
    {
        public WebSession(JsonSerializerFactory jsonFactory, Uri signapseUri)
            : base(jsonFactory, signapseUri)
        {

        }
    }
}
