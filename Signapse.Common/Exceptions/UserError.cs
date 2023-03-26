using System;

namespace Signapse.Exceptions
{
    public class UserError<T> : Exception
        where T : Exception
    {
        public UserError(string message)
            : base(message, Activator.CreateInstance(typeof(T), message) as T)
        {
        }
    }
}
