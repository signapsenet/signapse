using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Signapse.Server.Middleware
{
    class DefaultPathMiddleware
    {
        readonly RequestDelegate next;
        readonly string defaultPath;

        public DefaultPathMiddleware(RequestDelegate next, string defaultPath = "index.html")
            => (this.next, this.defaultPath) = (next, defaultPath);

        async public Task Invoke(HttpContext context)
        {
            string path = context.Request.Path;
            if (path == "" || path.EndsWith("/"))
            {
                // HACK: We need a more elegant solution here
                path = path.TrimEnd('/') + '/' + defaultPath.TrimStart('/');
                context.Response.Redirect(path);
            }
            else
            {
                await next(context);
            }
        }
    }

    public static class SignapseMiddleware
    {
        /// <summary>
        /// This is a hack to workaround not being able to make default routes in the mustache middleware
        /// work with index.html
        /// 
        /// There's probably a more elegant solution to this
        /// </summary>
        /// <param name="app"></param>
        /// <param name="defaultPath"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseDefaultPath(this IApplicationBuilder app, string defaultPath = "index.html")
        {
            return app.UseMiddleware<DefaultPathMiddleware>(defaultPath);
        }
    }
}
