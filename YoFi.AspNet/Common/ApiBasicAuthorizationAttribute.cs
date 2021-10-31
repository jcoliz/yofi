using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;

namespace YoFi.AspNet.Common
{
    public class ApiBasicAuthorizationAttribute: ActionFilterAttribute, IAsyncAuthorizationFilter
    {
        public Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var httpcontext = context.HttpContext;
            try
            {
                IConfiguration config = httpcontext.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration;
                var expectedpassword = config["Api:Key"];

                if (string.IsNullOrEmpty(expectedpassword))
                    throw new ApplicationException();

                var Headers = context.HttpContext.Request.Headers;

                if (!Headers.ContainsKey("Authorization"))
                    throw new UnauthorizedAccessException("Authorization header required");

                var authorization = Headers["Authorization"].Single();
                if (!authorization.StartsWith("Basic "))
                    throw new UnauthorizedAccessException("Authorization supports basic authentication only");

                var base64 = authorization[6..];
                var credentialBytes = Convert.FromBase64String(base64);
                var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':', 2);
                var password = credentials[1];

                if (expectedpassword != password)
                    throw new UnauthorizedAccessException("Credentials invalid");
            }
            catch (ApplicationException)
            {
                context.Result = new BadRequestObjectResult("Application not configured for API password access.");
            }
            catch (UnauthorizedAccessException ex)
            {
                context.HttpContext.Response.StatusCode = 401;
                var host = httpcontext.Request.Host;
                context.HttpContext.Response.Headers.Add("WWW-Authenticate", string.Format("Basic realm=\"{0}\"", host));
                context.Result = new UnauthorizedObjectResult(ex.Message);
            }

            return Task.CompletedTask;
        }
    }
}
