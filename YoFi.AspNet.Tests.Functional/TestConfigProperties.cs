using Microsoft.Extensions.Configuration;
using System;
using System.Reflection;

namespace YoFi.AspNet.Tests.Functional
{
    public class TestConfigProperties
    {
        public string Url { get; private set; }
        public string AdminUserEmail { get; private set; }
        public string AdminUserPassword { get; private set; }
        public string ApiKey { get; private set; }
        public bool IsDevelopment { get; private set; }
        public bool IsDemo { get; private set; }
        public string ViewportSize { get; private set; }
        public string Host { get; private set; }

        public TestConfigProperties(System.Collections.IDictionary testproperties)
        {
            // Top priority is to get config out of the properties
            Url = testproperties["webAppUrl"] as string;
            AdminUserEmail = testproperties["email"] as string;
            AdminUserPassword = testproperties["password"] as string;
            ApiKey = testproperties["apikey"] as string;
            IsDevelopment = (testproperties["environment"] as string)?.Contains("Development") == true;
            IsDemo = testproperties["demo"] as string == "true";
            Host = testproperties["host"] as string;
            ViewportSize = testproperties["viewportSize"] as string;

            // Else get them from user secrets
            var config = new ConfigurationBuilder().AddUserSecrets(Assembly.GetAssembly(typeof(FunctionalUITest))).Build();
            if (AdminUserEmail == null)
                AdminUserEmail = config["AdminUser:Email"];
            if (AdminUserPassword == null)
                AdminUserPassword = config["AdminUser:Password"];
            if (ApiKey == null)
                ApiKey = config["Api:Key"];
        }
    }
}
