using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YoFi.Services
{
    // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-6.0

    public class SendGridEmailOptions
    {
        public const string Section = "SendGrid";

        public string Key { get; set; }
        public string Email { get; set; }
        public string Sender { get; set; }
    }
}
