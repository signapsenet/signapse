using Signapse.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Signapse.Server.Common.BackgroundServices
{
    /// <summary>
    /// Check for completed requests and initialize the secret handshake
    /// </summary>
    public class ProcessAffiliateRequests : BackgroundService
    {
        protected override async Task DoWork(CancellationToken token)
        {
            // TODO: Process Affiliate Requests

            // foreach request we've made
            {
                // ask the target server if it was accepted (target server will validate consensus)

                // if accepted
                {
                    // send a join request transaction to the target server

                    // for each affiliate that's returned
                    {
                        // send a join request

                        // add the affiliate to our list
                    }
                }
            }

            await Task.Delay(TimeSpan.FromHours(1));
        }
    }
}
