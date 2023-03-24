using AffiliateSim.Services;
using Microsoft.Extensions.DependencyInjection;
using Signapse.BlockChain;
using System.ComponentModel;
using System.Security.Cryptography;

namespace AffiliateSim
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var services = new ServiceCollection();
            services.AddSingleton<Simulator>();
            services.AddSingleton<LedgerManager>();
            services.AddTransient<FormFactory>();

            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            
            var formFactory = scope.ServiceProvider.GetRequiredService<FormFactory>();
            Application.Run(formFactory.Create<MainForm>());
        }
    }

    static class RNGExtensions
    {
        static RandomNumberGenerator rng = RandomNumberGenerator.Create();

        static public IEnumerable<T> Randomize<T>(this IEnumerable<T> coll)
            where T : class
        {
            Dictionary<T, UInt64> randomValues = new Dictionary<T, UInt64>();
            
            byte[] data = new byte[8];
            UInt64 getRandomValue(T item)
            {
                if (false == randomValues.TryGetValue(item, out var val))
                {
                    rng.GetBytes(data);
                    
                    val = BitConverter.ToUInt64(data, 0);
                    randomValues.Add(item, val);
                }
                return val;
            }

            return coll.OrderBy(getRandomValue);
        }
    }
}