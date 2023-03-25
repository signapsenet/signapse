using System.IO;
using System;
using System.Reflection;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Signapse.Server.Common.Services;

namespace Signapse.Server.Middleware
{
    /// <summary>
    /// Use embedded resources for streaming content.
    /// </summary>
    class EmbeddedResourcesMiddleware
    {
        readonly EmbeddedResourceLoader resLoader;
        readonly RequestDelegate next;
        readonly Assembly asm;

        public EmbeddedResourcesMiddleware(RequestDelegate next, Assembly asm, IWebHostEnvironment env)
        {
            resLoader = new EmbeddedResourceLoader(asm, env);
            this.next = next;
            this.asm = asm;
        }

        async public Task Invoke(HttpContext httpContext)
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

        string TruncatePath(string str)
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
