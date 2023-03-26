using Signapse.RequestData;
using Signapse.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Signapse.Data
{
    public interface IDatabaseEntry : IWebRequest
    {
        Guid ID { get; set; }
    }

    /// <summary>
    /// Indicates which policies are allowed to view a property via web requests
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class PolicyAccessAttribute : Attribute
    {
        public string[] Policies { get; }

        public PolicyAccessAttribute(params string[] policies)
            => this.Policies = policies;
    }

    /// <summary>
    /// Indicates a property that should always be excluded from web requests
    /// </summary>
    public class NoPolicyAccessAttribute : PolicyAccessAttribute
    {
        public NoPolicyAccessAttribute() : base("NoAccess") { }
    }

    static public class DatabaseEntryExtensions
    {
        static Dictionary<Type, IReadOnlyDictionary<string, string[]>> PolicyAttributes
            = new Dictionary<Type, IReadOnlyDictionary<string, string[]>>();

        static IReadOnlyDictionary<string, string[]> GetPolicyAttributes<T>()
        {
            if (PolicyAttributes.TryGetValue(typeof(T), out var res) == false)
            {
                res = typeof(T)
                    .GetProperties()
                    .Select(p => new
                    {
                        attr = p.GetCustomAttribute<PolicyAccessAttribute>(),
                        name = p.Name
                    })
                    .Where(o => o.attr != null)
                    .ToDictionary(o => o.name, o => o.attr!.Policies);

                PolicyAttributes[typeof(T)] = res;
            }

            return res;
        }

        static public TEntry ApplyPolicyAccess<TEntry>(this TEntry item, IAuthResults authResults)
            where TEntry: IDatabaseEntry
        {
            var privateAttributes = GetPolicyAttributes<TEntry>();
            var res = Activator.CreateInstance<TEntry>();

            foreach (var prop in typeof(TEntry).GetProperties())
            {
                if (privateAttributes.TryGetValue(prop.Name, out var policies) == false
                    || policiesMatch(policies))
                {
                    prop.SetValue(res, prop.GetValue(item));
                }
                else
                {
                    prop.SetValue(res, null);
                }
            }

            bool policiesMatch(string[] policies)
            {
                return policies.Length == 0
                    ? authResults.IsAuthorized
                    : policies
                        .Where(p => p switch
                        {
                            Policies.User => authResults.IsUser,
                            Policies.Administrator => authResults.IsAdmin,
                            Policies.UsersAdministrator => authResults.IsUsersAdmin,
                            Policies.AffiliatesAdministrator => authResults.IsAffiliatesAdmin,
                            _ => false
                        })
                        .Any();
            }

            return res;
        }

        static public bool Matches<T>(this T entry, T cmp, params string[] properties)
            where T : IDatabaseEntry
        {
            var props = new HashSet<string>(properties, StringComparer.OrdinalIgnoreCase);
            bool res = false;

            foreach (var prop in typeof(T).GetProperties())
            {
                if (props.Contains(prop.Name)
                    && prop.GetValue(entry)?.Equals(prop.GetValue(cmp)) == true)
                {
                    res = true;
                    break;
                }
            }

            return res;
        }

        static public void CopyPropertiesFrom<T>(this T to, T from, Func<string, bool> includeProperty)
            where T : IDatabaseEntry
        {
            foreach (var prop in typeof(T).GetProperties())
            {
                if (includeProperty(prop.Name))
                    //|| System.Nullable.GetUnderlyingType(prop.PropertyType) != null
                    //|| typeof(string).IsAssignableFrom(prop.PropertyType))
                {
                    if (prop.GetValue(from) is object val)
                    {
                        prop.SetValue(to, val);
                    }
                }
            }
        }

        static public T Clone<T>(this T item)
            where T : IDatabaseEntry
        {
            var res = Activator.CreateInstance<T>() ?? throw new Exception($"Cannot create {typeof(T).Name}");

            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (var fi in res.GetType().GetFields(flags))
            {
                fi.SetValue(res, fi.GetValue(item));
            }

            return res;
        }
    }
}
