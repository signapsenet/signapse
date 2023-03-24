using Signapse.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Signapse.Server.Common.BackgroundServices
{
    public class AuditAffiliates : BackgroundService
    {
        protected override async Task DoWork(CancellationToken token)
        {
            // TODO: Audit affiliates for suspicious activity

            // foreach affiliate
            {
                // TODO: Determine some use cases for suspicious activity from an affiliate

                // foreach of the affiliates members
                {
                    // Check if the member views more of our content than the affiliate's content (urgent)

                    // Check if the member views more remote content than the affiliate's content

                    // Verify the percentage of content that is streamed simultaneously
                }
            }

            await Task.Delay(TimeSpan.FromHours(24));
        }
    }
}
