using Microsoft.Extensions.DependencyInjection;

namespace AffiliateSim
{
    public class FormFactory
    {
        readonly IServiceProvider provider;
        public FormFactory(IServiceProvider provider)
        {
            this.provider = provider;
        }

        public T Create<T>(params object[] args)
            where T : Form
        {
            return ActivatorUtilities.CreateInstance<T>(provider, args);
        }
    }
}