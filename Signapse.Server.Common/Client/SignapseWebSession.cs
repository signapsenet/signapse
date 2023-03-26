using Signapse.Services;
using System;

namespace Signapse.Client
{
    /// <summary>
    /// Communication paths from clients to Signapse endpoints
    /// </summary>
    public partial class SignapseWebSession : HttpSession
    {
        public SignapseWebSession(JsonSerializerFactory jsonFactory, Uri signapseUri)
            : base(jsonFactory, signapseUri)
        {

        }
    }
}
