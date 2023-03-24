using Signapse.Data;
using Signapse.Services;

namespace Signapse.Client
{
    /// <summary>
    /// Communication paths from clients to Signapse endpoints
    /// </summary>
    public partial class WebSession : HttpSession
    {
        public WebSession(JsonSerializerFactory jsonFactory, Uri signapseUri)
            : base(jsonFactory, signapseUri)
        {

        }
    }
}
