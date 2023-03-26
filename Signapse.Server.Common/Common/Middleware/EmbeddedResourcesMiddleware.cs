using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Signapse.Server.Common.Services;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Signapse.Server.Middleware
{
    /// <summary>
    /// Use embedded resources for streaming content.
    /// </summary>
    internal class EmbeddedResourcesMiddleware
    {
        private readonly EmbeddedResourceLoader resLoader;
        private readonly RequestDelegate next;
        private readonly Assembly asm;

        public EmbeddedResourcesMiddleware(RequestDelegate next, Assembly asm, IWebHostEnvironment env)
        {
            resLoader = new EmbeddedResourceLoader(asm, env);
            this.next = next;
            this.asm = asm;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            string path = httpContext.Request.Path;

            // Find the closest matching resource name
            var truncatedPath = TruncatePath(path);
            var resName = asm.GetManifestResourceNames()
                .Where(r => TruncatePath(r).EndsWith(truncatedPath, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            // Fetch the content
            if (resName != null
                && resLoader.LoadStream(resName, out var contentType) is Stream stream)
            {
                httpContext.Response.RegisterForDispose(stream);

                httpContext.Response.ContentType = contentType;
                httpContext.Response.ContentLength = stream.Length;
                await stream.CopyToAsync(httpContext.Response.Body);
            }
            else
            {
                await next(httpContext);
            }
        }

        private string TruncatePath(string str)
            => str.Replace("\\", "").Replace("/", "").Replace(".", "");
    }

    public static class EmbeddedResourcesExtensions
    {
        public static IApplicationBuilder UseEmbeddedResources(this IApplicationBuilder builder, Assembly asm)
        {
            return builder.UseMiddleware<EmbeddedResourcesMiddleware>(asm);
        }
    }
}
