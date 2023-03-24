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
using static System.Net.Mime.MediaTypeNames;

namespace Signapse.Server
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
}