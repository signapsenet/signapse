using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Signapse.Exceptions;
using Signapse.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace Signapse
{
    static public class HttpExtensions
    {
        static public T? Read<T>(this HttpContext context)
        {
            return context.Read<T>(out var propertiesRead);
        }

        static public T? Read<T>(this HttpContext context, out HashSet<string> propertiesRead)
        {
            var req = context.Request;

            propertiesRead = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var properties = typeof(T)
                .GetProperties()
                .Where(pi => pi.SetMethod != null)
                .ToDictionary(pi => pi.Name, pi => pi, StringComparer.OrdinalIgnoreCase);

            if (req.HasFormContentType && Activator.CreateInstance(typeof(T)) is T res)
            {
                foreach (var key in req.Form.Keys)
                {
                    setProperty(res, key, req.Form[key], propertiesRead);
                }

                return applyQueryKeys(res, propertiesRead);
            }
            else if (req.HasJsonContentType())
            {
                using (var reader = new StreamReader(req.Body))
                {
                    var json = reader.ReadToEndAsync().Result;

                    if (string.IsNullOrEmpty(json) == false)
                    {
                        var jsonFactory = context.RequestServices.GetRequiredService<JsonSerializerFactory>();
                        if (jsonFactory.Deserialize<T>(json) is T jRes)
                        {
                            var propDict = jsonFactory.Deserialize<Dictionary<string, object?>>(json);
                            if (propDict != null)
                            {
                                foreach (var key in propDict.Keys)
                                {
                                    propertiesRead.Add(key);
                                }
                            }

                            return applyQueryKeys(jRes, propertiesRead);
                        }
                    }
                }
            }
            else if ((req.Query.Count > 0 || req.RouteValues.Count > 0)
                && Activator.CreateInstance(typeof(T)) is T qRes)
            {
                return applyQueryKeys(qRes, propertiesRead);
            }

            void setProperty(T obj, string propName, object? value, HashSet<string> propertiesRead)
            {
                PropertyInfo? pi;
                if (properties.TryGetValue(propName, out pi)
                    || properties.TryGetValue(propName.Replace("_", ""), out pi))
                {
                    if (value != null)
                    {
                        object? convertedValue = value.SafeTypeConvert(pi.PropertyType);

                        propertiesRead.Add(pi.Name);
                        pi.SetValue(obj, convertedValue);
                    }
                }
            }

            T applyQueryKeys(T obj, HashSet<string> propertiesRead)
            {
                foreach (string key in req.Query.Keys)
                {
                    setProperty(obj, key, req.Query[key].FirstOrDefault(), propertiesRead);
                }

                foreach (string key in req.RouteValues.Keys)
                {
                    setProperty(obj, key, req.RouteValues[key], propertiesRead);
                }

                return obj;
            }

            return default(T);
        }

        static public void WriteTo(this Exception ex, HttpResponse response)
        {
            if (ex is HttpException httpEx)
            {
                response.StatusCode = (int)httpEx.StatusCode;
            }
            else
            {
                response.StatusCode = StatusCodes.Status500InternalServerError;
            }

            if (ex is HttpRedirect httpRedirect)
            {
                response.Redirect(httpRedirect.Url);
            }
            else
            {
                response.ContentType = "application/json";
                response.WriteAsJsonAsync(new
                {
                    error = ex.Message
                });
            }
        }
    }

    static public class ClaimsPrincipalExtensions
    {
        static public T ClaimValue<T>(this ClaimsPrincipal user, string claimType)
            where T : struct
        {
            return user.Claims
                .Where(c => c.Type == claimType)
                .Select(c => c.Value)
                .Select(c => Enum.TryParse<T>(c, out var res) ? res : default(T))
                .FirstOrDefault();
        }

        static public string? ClaimValue(this ClaimsPrincipal user, string claimType)
        {
            return user.Claims
                .Where(c => c.Type == claimType)
                .Select(c => c.Value)
                .FirstOrDefault();
        }

        static public Guid SignapseUserID(this ClaimsPrincipal user)
        {
            if (user.Claims.FirstOrDefault(c => c.Type == Claims.UserID)?.Value is string id
                && Guid.TryParse(id, out var guid))
            {
                return guid;
            }
            else
            {
                return Guid.Empty;
            }
        }
    }

    static public class ServerStringExtensions
    {
        readonly static public JsonSerializerOptions JsonOptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new BlockChain.BlockConverter(),
                new BlockChain.TransactionConverter()
            }
        };

        static public string Serialize<T>(this T obj)
        {
            try
            {
                return JsonSerializer.Serialize(obj, obj!.GetType(), JsonOptions);
            }
            catch { }

            return string.Empty;
        }
    }
}