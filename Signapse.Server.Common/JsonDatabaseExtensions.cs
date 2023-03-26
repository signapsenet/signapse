using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Signapse.Data;
using Signapse.Exceptions;
using Signapse.Server.Common.Services;
using Signapse.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Signapse.Server
{
    public static class JsonDatabaseExtensions
    {
        private static readonly HashSet<string> DB_METHODS = new HashSet<string>() { "GET", "PUT", "DELETE" };

        public static IEndpointRouteBuilder MapDatabaseEndpoint<T>(this IEndpointRouteBuilder app, string path)
            where T : class, IDatabaseEntry
        {
            return app.MapDatabaseEndpoint<T, DatabaseEntryValidator<T>>(path);
        }

        public static IEndpointRouteBuilder MapDatabaseEndpoint<T, TDBValidator>(this IEndpointRouteBuilder app, string path)
            where T : class, IDatabaseEntry
            where TDBValidator : IDatabaseEntryValidator
        {
            if (path.StartsWith('/') == false)
            {
                path = $"/api/v1/{path}";
            }

            app.Map($"{path}/{{id?}}", (ctx) => ProcessDatabaseRequest<T, TDBValidator>(ctx));
            return app;
        }

        private static async Task ProcessDatabaseRequest<TEntry, TDBValidator>(HttpContext context)
            where TEntry : class, IDatabaseEntry
            where TDBValidator : IDatabaseEntryValidator
        {
            try
            {
                // Load all the DI services
                var db = context.RequestServices.GetRequiredService<JsonDatabase<TEntry>>();
                var authFactory = context.RequestServices.GetRequiredService<AuthorizationFactory>();

                var authResults = await authFactory.Create(context.User);
                var validator = ActivatorUtilities.CreateInstance<TDBValidator>(context.RequestServices, context.User, authResults);

                var request = context.Read<TEntry>(out var readProperties)
                    ?? Activator.CreateInstance<TEntry>();

                // For form submissions, allow overriding the POST method by injecting a _method field
                var method = context.Request.Method;
                if (context.Request.HasFormContentType
                    && context.Request.Form["_method"].FirstOrDefault() is string m
                    && DB_METHODS.Contains(m))
                {
                    method = m;
                }

                object? res = null;

                using var _ = db.Lock();
                switch (method)
                {
                    case "GET":
                        {
                            if (request.ID != Guid.Empty && db[request.ID] is TEntry entry)
                            {
                                res = entry.ApplyPolicyAccess(authResults);
                            }
                            else
                            {
                                // Don't allow users to query/modify properties they don't have access to
                                request = request.ApplyPolicyAccess(authResults);

                                var items = db.Items
                                    .Where(it => it.Matches(request))
                                    .Select(it => it.ApplyPolicyAccess(authResults))
                                    .ToArray();

                                if (items.Length == 0)
                                {
                                    throw new HttpNotFound();
                                }

                                res = items;
                            }
                        }
                        break;

                    case "PUT":
                        if (authResults.IsAuthorized == false)
                            throw new HttpForbidden();

                        // Attempting to update
                        if (request.ID == Guid.Empty)
                        {
                            request.ID = Guid.NewGuid();
                            if (validator.ValidateInsert(request))
                            {
                                db.Items.Add(request);
                                await db.Save();
                            }
                            else
                                throw new HttpBadRequest("Invalid Object");
                        }
                        else if (db[request.ID] is TEntry match
                            && readProperties != null)
                        {
                            // Don't allow users to query/modify properties they don't have access to
                            request = request.ApplyPolicyAccess(authResults);

                            request.CopyPropertiesFrom(match, (propName) => !readProperties.Contains(propName));
                            if (validator.ValidateUpdate(request))
                            {
                                match.CopyPropertiesFrom(request, (propName) => readProperties.Contains(propName));
                                await db.Save();
                            }
                            else
                                throw new HttpBadRequest("Invalid Object");
                        }
                        else
                        {
                            throw new HttpNotFound();
                        }

                        res = request.ApplyPolicyAccess(authResults);
                        break;

                    case "DELETE":
                        if (authResults.IsAuthorized == false)
                        {
                            throw new HttpUnauthorized();
                        }
                        else
                        {
                            if (db[request.ID] is TEntry item)
                            {
                                if (validator.ValidateDelete(item))
                                {
                                    db.Items.Remove(item);
                                    await db.Save();
                                }
                                else throw new HttpForbidden();
                            }
                            else throw new HttpNotFound();
                        }

                        break;

                    default:
                        throw new HttpBadRequest("Invalid Method");
                }

                if (res != null)
                {
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    context.Response.ContentType = "application/json";

                    await context.Response.WriteAsJsonAsync(new
                    {
                        Result = res
                    });
                }
            }
            catch (Exception ex) { ex.WriteTo(context.Response); }
        }
    }
}