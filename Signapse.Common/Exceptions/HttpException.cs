using System.Net;

namespace Signapse.Exceptions
{
    abstract public class HttpException : Exception
    {
        readonly public HttpStatusCode StatusCode;

        protected HttpException(HttpStatusCode status)
        {
            this.StatusCode = status;
        }

        protected HttpException(HttpStatusCode status, string msg) : base(msg)
        {
            this.StatusCode = status;
        }
    }

    public class HttpNotFound : HttpException
    {
        public HttpNotFound() : base(HttpStatusCode.NotFound) { }
    }

    public class HttpBadRequest : HttpException
    {
        public HttpBadRequest(string msg) : base(HttpStatusCode.BadRequest, msg) { }
    }

    public class HttpForbidden : HttpException
    {
        public HttpForbidden() : base(HttpStatusCode.Forbidden) { }
    }

    public class HttpUnauthorized : HttpException
    {
        public HttpUnauthorized() : base(HttpStatusCode.Unauthorized) { }
    }

    public class HttpRedirect : HttpException
    {
        public string Url { get; }

        public HttpRedirect(string url) : base(HttpStatusCode.Redirect)
        {
            this.Url = url;
        }
    }
}