using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signapse.RequestData
{
    /// <summary>
    /// Indicates that a type can be used in an affiliate request
    /// </summary>
    public interface IAffiliateRequest { }

    /// <summary>
    /// This wraps a request between Signapse affiliate servers
    /// </summary>
    sealed public class AffiliateRequest<T>
        where T : IAffiliateRequest
    {
        public Guid ID { get; set; } = Guid.NewGuid();
        public T? Data { get; set; }
    }
}
