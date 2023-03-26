using System;

namespace Signapse.RequestData
{
    /// <summary>
    /// Indicates that a type can be used in a web server request
    /// </summary>
    public interface IWebServerRequest { }

    /// <summary>
    /// This wraps a request from an affiliate web server to its
    /// Signapse server.
    /// </summary>
    sealed public class WebServerRequest<T>
        where T : IWebServerRequest
    {
        public Guid ID { get; set; } = Guid.NewGuid();
        public T? Data { get; set; }
    }
}
