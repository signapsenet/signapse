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