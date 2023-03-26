using System;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Signapse.Services
{
    public enum SendEmailResult
    {
        Failed,
        Succeeded
    }

    public class EmailProvider : IDisposable
    {
        private readonly AppConfig appConfig;
        private readonly CancellationTokenSource ctSource = new CancellationTokenSource();

        public EmailProvider(AppConfig appConfig)
        {
            this.appConfig = appConfig;
        }

        public async Task<SendEmailResult> SendTo(string emailAddress, string subject, string body)
        {
            using SmtpClient smtp = new SmtpClient(appConfig.SMTP.Address);
            smtp.Credentials = new NetworkCredential(appConfig.SMTP.User, appConfig.SMTP.Password);

            var message = new MailMessage(appConfig.SMTP.ReplyTo, emailAddress)
            {
                Subject = subject,
                SubjectEncoding = Encoding.UTF8,
                Body = body,
                BodyEncoding = Encoding.UTF8,
                IsBodyHtml = body.StartsWith('<')
            };

            try
            {
                await smtp.SendMailAsync(message, ctSource.Token);
                return SendEmailResult.Succeeded;
            }
            catch
            {
                return SendEmailResult.Failed;
            }
        }

        public async Task<SendEmailResult> SendVerification(Data.User user)
        {
            return await SendTo(user.Email, "Verify Account", "<div>Please <a href=\"#\">verify</a> your account.");
        }

        void IDisposable.Dispose()
        {
            ctSource.Cancel();
        }
    }
}
