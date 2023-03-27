using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Mustache;
using Signapse.Server.Common.Services;
using Signapse.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Signapse.Server.Middleware
{
    public static class MustacheEndpointExtensions
    {
        /// <summary>
        /// Specifies the options for WebApplication.MapMustacheEndpoint
        /// </summary>
        public class MustacheOptions
        {
            /// <summary>
            /// The assembly that contains the embedded resource for the mustache template
            /// </summary>
            public Assembly ResourceAssembly { get; set; } = Assembly.GetExecutingAssembly();
        }

        /// <summary>
        /// Map an embedded resource to a mustache template
        /// </summary>
        /// <typeparam name="TData">The data type to use in the mustache template and to handle callbacks</typeparam>
        /// <param name="app"></param>
        /// <param name="path">The web path</param>
        /// <returns></returns>
        public static IEndpointConventionBuilder MapMustacheEndpoint<TData>(this WebApplication app, string path)
            => app.MapMustacheEndpoint<TData>(path, config => { });

        /// <summary>
        /// Map an embedded resource to a mustache template
        /// </summary>
        /// <typeparam name="TData"></typeparam>
        /// <typeparam name="TData">The data type to use in the mustache template and to handle callbacks</typeparam>
        /// <param name="app"></param>
        /// <param name="path">The web path</param>
        /// <param name="config">Configuration callback</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static IEndpointConventionBuilder MapMustacheEndpoint<TData>(this WebApplication app, string path, Action<MustacheOptions> config)
        {
            MustacheOptions options = new MustacheOptions()
            {
                ResourceAssembly = typeof(TData).Assembly
            };
            config(options);

            string contentType = string.Empty;
            Template? template = null;

            return app.MapMethods(path, new[] { "GET", "POST" }, handleRequest);

            Task handleRequest(HttpContext context, IWebHostEnvironment env)
            {
                switch (context.Request.Method)
                {
                    case "POST": return handleApiRequest(context);
                    default: return handleTemplateCallback(context, env);
                }
            }

            async Task handleTemplateCallback(HttpContext context, IWebHostEnvironment env)
            {
                if (template == null)
                {
                    EmbeddedResourceLoader resLoader = new EmbeddedResourceLoader(new EmbeddedResourceOptions()
                    {
                        ResourcePath = "",
                        Assembly = options.ResourceAssembly
                    });

                    using var stream = resLoader.LoadStream(path, out contentType)
                        ?? throw new Exception($"Unable to locate resource {options.ResourceAssembly.FullName}: {path}");

                    using var reader = new StreamReader(stream);
                    template = Template.Compile(reader.ReadToEnd());
                }

                context.Response.Headers[HeaderNames.CacheControl] = "no-cache";
                context.Response.ContentType = contentType;

                var data = ActivatorUtilities.CreateInstance<TData>(context.RequestServices);

                var content = template.Render(data);
                await context.Response.WriteAsync(content);

#if DEBUG
                // Unset the template in debug mode to make refreshing content easier
                template = null;
#endif
            }

            async Task handleApiRequest(HttpContext context)
            {
                var data = ActivatorUtilities.CreateInstance<TData>(context.RequestServices);

                if (ReadParameter(context, "method") is string method
                    && FindMethod<TData>(method) is MethodInfo mi)
                {
                    var parameters = mi.GetParameters()
                        .Select(pi => ConvertParameter(context, pi.Name, pi.ParameterType))
                        .ToArray();

                    var result = mi.Invoke(data, parameters);
                    if (result is Task task)
                    {
                        await task;
                        if (task.GetType().GetProperty("Result") is PropertyInfo pi)
                        {
                            result = pi.GetValue(task);
                        }
                        else
                        {
                            result = null;
                        }
                    }

                    if (result != null)
                    {
                        await context.Response.WriteAsJsonAsync(result);
                    }
                }
            }
        }

        private static readonly Dictionary<Type, Dictionary<string, MethodInfo>> TypeMethods = new Dictionary<Type, Dictionary<string, MethodInfo>>();

        private static MethodInfo? FindMethod<T>(string methodName)
        {
            if (TypeMethods.TryGetValue(typeof(T), out var methods) == false)
            {
                try
                {
                    methods = typeof(T)
                        .GetMethods()
                        //.DistinctBy(mi => mi.Name) // we want an exception here, to avoid frustration
                        .ToDictionary(mi => mi.Name, mi => mi, StringComparer.OrdinalIgnoreCase);
                }
                catch (KeyNotFoundException ex)
                {
                    throw new KeyNotFoundException($"Multiple methods with the same name: {typeof(T).Name}.{methodName}", ex);
                }

                TypeMethods[typeof(T)] = methods;
            }


            return methods.TryGetValue(methodName, out var methodInfo)
                ? methodInfo
                : null;
        }

        private static string? ReadParameter(HttpContext context, string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }
            else if (context.Request.HasFormContentType
                && context.Request.Form.TryGetValue(name, out var formValue))
            {
                return formValue.First();
            }
            else if (context.Request.Query.TryGetValue(name, out var queryValue))
            {
                return queryValue.First();
            }
            else
            {
                return null;
            }
        }

        private static object? ConvertParameter(HttpContext context, string? name, Type type)
        {
            if (ReadParameter(context, name) is string value)
            {
                var jsonFactory = context.RequestServices.GetRequiredService<JsonSerializerFactory>();

                try
                {
                    return jsonFactory.Deserialize(type, value) ?? value;
                }
                catch
                {
                    return value;
                }
            }

            return null;
        }
    }
}