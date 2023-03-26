using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Signapse.Data;
using Signapse.Middleware;
using Signapse.Server;
using Signapse.Services;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;

namespace Signapse
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var server = new LocalAffiliateServer(args);

            server.Run(CancellationToken.None);
            server.WaitForShutdown();
        }
    }
}