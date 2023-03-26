using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Signapse.RequestData;
using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace Signapse.Server.Middleware
{
    public static class RequestHandlerExtensions
    {
        public static IEndpointConventionBuilder MapAffiliateRequestHandler<THandler, TRequest>(
            this WebApplication app,
            HttpMethod method,
            string absPath,
            Func<THandler, AffiliateRequest<TRequest>, Task> process)
            where TRequest : IAffiliateRequest
        {
            return app.MapRequestHandler(method, absPath, process);
        }

        public static IEndpointConventionBuilder MapAffiliateRequestHandler<THandler, TRequest>(
            this WebApplication app,
            HttpMethod method,
            string absPath,
            Func<THandler, Task> process)
            where TRequest : IAffiliateRequest
        {
            return app.MapRequestHandler(method, absPath, process);
        }

        public static IEndpointConventionBuilder MapWebRequestHandler<THandler, TRequest>(
            this WebApplication app,
            HttpMethod method,
            string absPath,
            Func<THandler, WebRequest<TRequest>, Task> process)
            where TRequest : IWebRequest
        {
            return app.MapRequestHandler(method, absPath, process);
        }

        public static IEndpointConventionBuilder MapWebRequestHandler<THandler, TRequest>(
            this WebApplication app,
            HttpMethod method,
            string absPath,
            Func<THandler, Task> process)
            where TRequest : IWebRequest
        {
            return app.MapRequestHandler(method, absPath, process);
        }

        public static IEndpointConventionBuilder MapWebServerRequestHandler<THandler, TRequest>(
            this WebApplication app,
            HttpMethod method,
            string absPath,
            Func<THandler, WebServerRequest<TRequest>, Task> process)
            where TRequest : IWebServerRequest
        {
            return app.MapRequestHandler(method, absPath, process);
        }

        public static IEndpointConventionBuilder MapWebServerRequestHandler<THandler, TRequest>(
            this WebApplication app,
            HttpMethod method,
            string absPath,
            Func<THandler, Task> process)
            where TRequest : IWebServerRequest
        {
            return app.MapRequestHandler(method, absPath, process);
        }

        private static IEndpointConventionBuilder MapRequestHandler<THandler, TRequest>(this WebApplication app, HttpMethod method, string absPath, Func<THandler, TRequest, Task> process)
        {
            if (method.Equals(HttpMethod.Get))
                return app.MapGet(absPath, handleRequest);
            else if (method.Equals(HttpMethod.Put))
                return app.MapPut(absPath, handleRequest);
            else if (method.Equals(HttpMethod.Delete))
                return app.MapDelete(absPath, handleRequest);
            else
                return app.MapPost(absPath, handleRequest);

            async Task handleRequest(HttpContext ctx)
            {
                if (ctx.Read<TRequest>() is TRequest request)
                {
                    var handler = ActivatorUtilities.CreateInstance<THandler>(ctx.RequestServices);
                    var processTask = process(handler, request);
                    await processTask;

                    if (!ctx.Response.HasStarted)
                    {
                        if (processTask.GetType().GetProperty("Result") is PropertyInfo pi)
                        {
                            await ctx.Response.WriteAsJsonAsync(new
                            {
                                Result = pi.GetValue(processTask)
                            });
                        }
                        else
                        {
                            await ctx.Response.WriteAsJsonAsync(new { result = "success" });
                        }
                    }
                }
                else throw new Exceptions.HttpBadRequest("Invalid Request");
            };
        }

        private static IEndpointConventionBuilder MapRequestHandler<THandler>(this WebApplication app, HttpMethod method, string absPath, Func<THandler, Task> process)
        {
            if (method.Equals(HttpMethod.Get))
                return app.MapGet(absPath, handleRequest);
            else if (method.Equals(HttpMethod.Put))
                return app.MapPut(absPath, handleRequest);
            else if (method.Equals(HttpMethod.Delete))
                return app.MapDelete(absPath, handleRequest);
            else
                return app.MapPost(absPath, handleRequest);

            async Task handleRequest(HttpContext ctx)
            {
                var handler = ActivatorUtilities.CreateInstance<THandler>(ctx.RequestServices);
                var processTask = process(handler);
                await processTask;

                if (processTask.GetType().IsGenericType
                    && processTask.GetType().GetProperty("Result") is PropertyInfo pi)
                {
                    await ctx.Response.WriteAsJsonAsync(new
                    {
                        result = pi.GetValue(processTask)
                    });
                }
                else
                {
                    await ctx.Response.WriteAsJsonAsync(new { result = "success" });
                }
            };
        }

    }
}
