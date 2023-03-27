using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Signapse.Server.Common.Services;
using System;
using System.Collections.Generic;
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
        private readonly EmbeddedResourceOptions opts;

        public EmbeddedResourcesMiddleware(RequestDelegate next, EmbeddedResourceOptions opts, IWebHostEnvironment env)
        {
            resLoader = new EmbeddedResourceLoader(opts);
            this.next = next;
            this.opts = opts;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            string path = httpContext.Request.Path;

            // Limit the request to the defined root path
            if (path.StartsWith(opts.VirtualPath) == false)
            {
                await next(httpContext);
                return;
            }

            // TODO: Add caching checks (based on asm/file dates)
            

            // Fetch the content
            if (resLoader.LoadStream(path, out var contentType) is Stream stream)
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

    /// <summary>
    /// Configuration for EmbeddedResourceMiddleware
    /// </summary>
    public class EmbeddedResourceOptions
    {
        /// <summary>
        /// The assembly that contains the embedded resources
        /// </summary>
        public Assembly Assembly { get; set; } = typeof(EmbeddedResourcesMiddleware).Assembly;

        /// <summary>
        /// The root path in the assembly
        /// </summary>
        public string ResourcePath { get; set; } = "wwwroot";

        /// <summary>
        /// The web root for the resources
        /// </summary>
        public string VirtualPath { get; set; } = "/";
    }

    public static class EmbeddedResourcesExtensions
    {
        public static IApplicationBuilder UseEmbeddedResources(this IApplicationBuilder builder, Action<EmbeddedResourceOptions> config)
        {
            EmbeddedResourceOptions options = new EmbeddedResourceOptions();
            config(options);

            options.VirtualPath = options.VirtualPath.TrimEnd('/');
            return builder.UseMiddleware<EmbeddedResourcesMiddleware>(options);
        }
    }
}
