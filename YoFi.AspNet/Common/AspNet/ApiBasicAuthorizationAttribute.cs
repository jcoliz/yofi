using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Common.DotNet;

namespace Common.AspNet
{
    public class ApiBasicAuthorizationAttribute: ActionFilterAttribute, IAsyncAuthorizationFilter
    {
        public Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var httpcontext = context.HttpContext;
            try
            {
                var config = httpcontext.RequestServices.GetService(typeof(IOptions<ApiConfig>)) as IOptions<ApiConfig>;
                var expectedpassword = config.Value.Key;

                if (string.IsNullOrEmpty(expectedpassword))
                    throw new ApplicationException("Unexpected configuration [E6]");

                var Headers = context.HttpContext.Request.Headers;

                if (!Headers.ContainsKey("Authorization"))
                    throw new UnauthorizedAccessException("Authorization header required [E2]");

                var authorization = Headers["Authorization"].Single();
                if (!authorization.StartsWith("Basic "))
                    throw new UnauthorizedAccessException("Authorization supports basic authentication only [E3]");

                var base64 = authorization[6..];
                var credentialBytes = new Span<byte>(new byte[32+expectedpassword.Length]);
                var ok = Convert.TryFromBase64String(base64, credentialBytes,out int length);

                if (!ok)
                    throw new UnauthorizedAccessException("Credentials mis-coded [E5]");

                var credentials = Encoding.UTF8.GetString(credentialBytes[..length] ).Split(':', 2);
                var password = credentials[1];

                if (expectedpassword != password)
                    throw new UnauthorizedAccessException("Credentials invalid [E4]");
            }
            catch (ApplicationException)
            {
                context.Result = new BadRequestObjectResult("Application not configured for API password access. [E1]");
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
