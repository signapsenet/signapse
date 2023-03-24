using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Mustache;
using Signapse.Data;
using Signapse.Exceptions;
using Signapse.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Signapse.Server.Middleware
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class DataForAttribute : Attribute
    {
        public string Path { get; }

        public DataForAttribute(string path) => this.Path = path;
    }

    public class MustacheData
    {
        [JsonIgnore] public string AppData => this.Serialize();

#if DEBUG
        public bool IsDebug => true;
#else
        public bool IsDebug => false;
#endif

        public MustacheData() { }

        public object Translate(string name)
        {
            return name;
        }
    }

    public class MustacheOptions
    {
        public string[] ExtensionsToProcess { get; set; } = { "js", "html", "css" };
        public Type BaseDataType { get; set; } = typeof(MustacheData);
    }

    static public class MustacheExtensions
    {
        class MustacheDescriptor
        {
            readonly public Template template;
            readonly public Type dataType;
            readonly public string contentType;

            public MustacheDescriptor(Template template, Type dataType, string contentType)
                => (this.template, this.dataType, this.contentType) = (template, dataType, contentType);
        }

        static public IApplicationBuilder UseMustacheTemplates(this IApplicationBuilder app)
            => app.UseMustacheTemplates(options => { });

        static public IApplicationBuilder UseMustacheTemplates(this IApplicationBuilder app, Action<MustacheOptions> config)
        {
            Dictionary<string, MustacheDescriptor> templates = new Dictionary<string, MustacheDescriptor>();

            MustacheOptions options = new MustacheOptions();
            config(options);

            return app.Use(async (ctx, next) =>
            {
                if (Path.GetExtension(ctx.Request.Path)?.TrimStart('.') is string ext
                    && options.ExtensionsToProcess.Contains(ext))
                {
                    try
                    {
                        await processTemplate(ctx, next);
                    }
                    catch (HttpException ex)
                    {
                        ex.WriteTo(ctx.Response);
                    }
                }
                else
                {
                    await next(ctx);
                }
            });

            async Task processTemplate(HttpContext context, RequestDelegate next)
            {
                if (templates.TryGetValue(context.Request.Path, out var desc) == false)
                {
                    var origBody = context.Response.Body;
                    var newBody = new MemoryStream();

                    // We set the response body to our stream so we can read after the chain of middlewares have been called.
                    context.Response.Body = newBody;
#if DEBUG
                    // give a little time for the file to close before attempting to reload it
                    await Task.Delay(200);
#endif
                    await next(context);
                    newBody.Seek(0, SeekOrigin.Begin);
                    context.Response.Body = origBody;

                    if (context.Response.StatusCode == (int)HttpStatusCode.OK)
                    {
                        var newContent = new StreamReader(newBody).ReadToEnd();
                        var dataType = options.BaseDataType
                            .Assembly
                            .GetTypes()
                            .Concat(Assembly.GetEntryAssembly()?.GetTypes() ?? new Type[0])
                            .Distinct()
                            .Where(t => options.BaseDataType.IsAssignableFrom(t))
                            .Where(t => t.GetCustomAttribute<DataForAttribute>()?.Path == context.Request.Path)
                            .FirstOrDefault() ?? options.BaseDataType;

                        try
                        {
                            desc = new MustacheDescriptor(Template.Compile(newContent), dataType, context.Response.ContentType);
#if RELEASE
                            templates[context.Request.Path] = desc;
#endif
                        }
                        catch
                        {
                            context.Response.ContentLength = newContent.Length;
                            await context.Response.WriteAsync(newContent);
                        }
                    }
                    else
                    {
                        await newBody.CopyToAsync(context.Response.Body);
                    }
                }

                if (desc != null)
                {
                    if (Path.GetExtension(context.Request.Path) == ".html")
                    {
                        context.Response.Headers[HeaderNames.CacheControl] = "no-cache";
                    }

                    var data = ActivatorUtilities.CreateInstance(context.RequestServices, desc.dataType);
                    var content = desc.template.Render(data);

                    context.Response.ContentType = desc.contentType;
                    context.Response.ContentLength = content.Length;
                    await context.Response.WriteAsync(content);
                }
            }
        }
    }
}