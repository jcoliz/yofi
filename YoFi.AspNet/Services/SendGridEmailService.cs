using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
 
namespace YoFi.Services
{
    // https://www.ryadel.com/en/asp-net-core-send-email-messages-sendgrid-api/

    [ExcludeFromCodeCoverage]
    public class SendGridEmailService : IEmailSender
    {
        public SendGridEmailService(IOptions<SendGridEmailOptions> options)
        {
            _options = options.Value;
        }

        private readonly SendGridEmailOptions _options;

        public async Task SendEmailAsync(String email, String subject, String message)
        {
            var client = new SendGridClient(_options.Key);
            var msg = new SendGridMessage()
            {
                From = new EmailAddress(_options.Email, _options.Sender),
                Subject = subject,
                PlainTextContent = message,
                HtmlContent = message
            };
            msg.AddTo(new EmailAddress(email));

            // disable tracking settings
            // ref.: https://sendgrid.com/docs/User_Guide/Settings/tracking.html
            msg.SetClickTracking(false, false);
            msg.SetOpenTracking(false);
            msg.SetGoogleAnalytics(false);
            msg.SetSubscriptionTracking(false);

            await client.SendEmailAsync(msg);
        }
    }
}