using Signapse.BlockChain;
using Signapse.Data;
using Signapse.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Signapse.Server.Common.BackgroundServices
{
    public class ProcessMemberFees : BackgroundService
    {
        private readonly PaymentProcessor payments;
        private readonly JsonDatabase<Member> dbMembers;
        private readonly SignapseLedger ledger;

        public ProcessMemberFees(SignapseLedger ledger, PaymentProcessor payments, JsonDatabase<Member> dbMembers)
        {
            this.ledger = ledger;
            this.payments = payments;
            this.dbMembers = dbMembers;
        }

        protected override async Task DoWork(CancellationToken token)
        {
            // TODO: Process Member Fees

            await Process(token);
            await Task.Delay(TimeSpan.FromHours(8));
        }

        protected async Task Process(CancellationToken token)
        {
            // foreach member with a payment due
            {
                // Collect the member's payment

                // Once the payment is complete, disperse portions to each affiliate
                await DisperseFunds();
            }
        }

        private async Task DisperseFunds()
        {
            // gather the apportionment of time from the member's prior payment until now,
            // for each affiliate

            // for each apportion, send the funds to the affiliate and log it in the ledger
            {

            }

            await Task.CompletedTask;
        }
    }
}
