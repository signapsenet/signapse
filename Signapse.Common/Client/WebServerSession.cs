using Signapse.Services;

namespace Signapse.Client
{
    /// <summary>
    /// Communication paths from web servers to Signapse endpoints
    /// </summary>
    public class WebServerSession : HttpSession
    {
        public WebServerSession(JsonSerializerFactory jsonFactory, Uri signapseUri)
            : base(jsonFactory, signapseUri)
        {

        }
    }
}
