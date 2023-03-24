namespace Signapse.RequestData
{
    /// <summary>
    /// Indicates that a type can be used in a web request
    /// </summary>
    public interface IWebRequest { }

    /// <summary>
    /// This wraps a RESTish request from a web client to either an affiliate web server or the
    /// Signapse web server
    /// </summary>
    sealed public class WebRequest<T>
        where T : IWebRequest
    {
        public Guid ID { get; set; } = Guid.NewGuid();
        public T? Data { get; set; }
    }
}
