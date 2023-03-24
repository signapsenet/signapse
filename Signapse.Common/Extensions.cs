using Signapse.Exceptions;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text;
using Newtonsoft.Json.Linq;
using System.ComponentModel;

namespace Signapse
{
    static public class Extensions
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

    static public class StringExtensions
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

        static public string ToMD5_2(this string? str)
        {
            using var md5 = MD5.Create();

            try
            {
                if (str != null)
                {
                    var buffer = Encoding.UTF8.GetBytes(str);
                    var bytes = md5.ComputeHash(md5.ComputeHash(buffer));

                    return Convert.ToHexString(bytes);
                }
            }
            catch { }

            return string.Empty;
        }

        static public void CopyPropertiesTo<T, U>(this T from, U to, params string[] properties)
        {
            if (properties.Length == 0)
            {
                properties = typeof(T)
                    .GetProperties()
                    .Select(p => p.Name)
                    .ToArray();
            }

            foreach (var prop in properties)
            {
                if (from?.GetType().GetProperty(prop) is PropertyInfo fromProp
                    && to?.GetType().GetProperty(prop) is PropertyInfo toProp
                    && fromProp.PropertyType == toProp.PropertyType)
                {
                    toProp.SetValue(to, fromProp.GetValue(from));
                }
            }
        }
    }

    static public class ConverterExtensions
    {
        static public TEnum ToEnum<TEnum>(this string str)
            where TEnum : struct
        {
            return str.ToEnum<TEnum>(default(TEnum));
        }

        static public TEnum ToEnum<TEnum>(this string str, TEnum defaultValue)
            where TEnum : struct
        {
            return Enum.TryParse<TEnum>(str, true, out var res)
                ? res
                : defaultValue;
        }

        static public object? SafeTypeConvert<T>(this object? obj)
            => obj.SafeTypeConvert(typeof(T));

        static public object? SafeTypeConvert(this object? value, Type type)
        {
            object? convertedValue = null;
            if (value != null)
            {
                try
                {
                    if (value is string str)
                    {
                        convertedValue = TypeDescriptor.GetConverter(type).ConvertFromInvariantString(str);
                    }
                    else
                    {
                        convertedValue = Convert.ChangeType(value, type);
                    }
                }
                catch
                {
                    try { convertedValue = Convert.ChangeType(value, type); }
#if DEBUG
                    catch { throw; }
#else
                    catch { }
#endif
                }
            }

            return convertedValue;
        }
    }
}