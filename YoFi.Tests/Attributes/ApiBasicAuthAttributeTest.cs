using Common.AspNet;
using Common.DotNet;
using Common.DotNet.Test;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace YoFi.Tests.Attributes
{
    [TestClass]
    public class ApiBasicAuthAttributeTest
    {
        ApiBasicAuthorizationAttribute attribute;

        ActionContext GivenActionContextWith(string password, string auth)
        {
            // Given: Http context with basic auth header, but not base64 password
            var httpcontext = new DefaultHttpContext();
            var config = new Mock<IOptions<ApiConfig>>();

            if (password != null)
                config.Setup(x => x.Value).Returns(new ApiConfig() { Key = password });
            else
                config.Setup(x => x.Value).Returns(new ApiConfig());

            var services = new Mock<IServiceProvider>();
            services.Setup(x => x.GetService(typeof(IOptions<ApiConfig>))).Returns(config.Object);
            httpcontext.RequestServices = services.Object;

            if (auth != null)
                httpcontext.Request.Headers.Add("Authorization", auth);

            return new ActionContext()
            {
                HttpContext = httpcontext,
                RouteData = new RouteData(),
                ActionDescriptor = new ActionDescriptor()
            };
        }

        private async Task<AuthorizationFilterContext> WhenExecutingTheFilter(ActionContext actionContext)
        {
            // When: Executing the filter
            AuthorizationFilterContext context = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
            await attribute.OnAuthorizationAsync(context);

            return context;
        }

        private void ThenIsResult<T>(IActionResult actionresult, string code) where T: ObjectResult
        {
            // Then: Result is bad request
            Assert.IsNotNull(actionresult);
            var result = Assert.That.IsOfType<T>(actionresult);

            // And: Message is {code}
            var message = Assert.That.IsOfType<string>(result.Value);
            Assert.IsTrue(message.Contains(code));
        }

        [TestInitialize]
        public void SetUp()
        {
            attribute = new ApiBasicAuthorizationAttribute();
        }

        [TestMethod]
        public void Empty()
        {
            Assert.IsNotNull(attribute);
        }

        [TestMethod]
        public async Task NoPassword()
        {
            // Given: Action context with no configuration parameters
            var actionContext = GivenActionContextWith(password: null, auth: null);

            // When: Executing the filter
            var context = await WhenExecutingTheFilter(actionContext);

            // Then: Result is bad request #E1
            ThenIsResult<BadRequestObjectResult>(context.Result,"E1");
        }

        [TestMethod]
        public async Task NoAuthHeader()
        {
            // Given: Action context with proper Api Key but no auth header
            var actionContext = GivenActionContextWith(password: "Password", auth: null);

            // When: Executing the filter
            var context = await WhenExecutingTheFilter(actionContext);

            // Then: Result is UnauthorizedObjectResult #E2
            ThenIsResult<UnauthorizedObjectResult>(context.Result, "E2");
        }

        [TestMethod]
        public async Task AuthHeaderNotBasic()
        {
            // Given: Http context with auth header, but not "basic" auth
            var actionContext = GivenActionContextWith(password: "Password", auth: "Wrong");

            // When: Executing the filter
            var context = await WhenExecutingTheFilter(actionContext);

            // Then: Result is UnauthorizedObjectResult #E3
            ThenIsResult<UnauthorizedObjectResult>(context.Result, "E3");
        }

        [TestMethod]
        public async Task AuthHeaderMiscodedPassword()
        {
            // Given: Http context with basic auth header, but not base64 password
            var actionContext = GivenActionContextWith(password: "Password", auth: "Basic Wrong");

            // When: Executing the filter
            var context = await WhenExecutingTheFilter(actionContext);

            // Then: Result is UnauthorizedObjectResult #E5
            ThenIsResult<UnauthorizedObjectResult>(context.Result, "E5");
        }

        [TestMethod]
        public async Task AuthHeaderWrongPassword()
        {
            // Given: Http context with basic auth header, but wrong password, but correctly encoded
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"user:Wrong"));
            var actionContext = GivenActionContextWith(password: "Password", auth: $"Basic {base64}");

            // When: Executing the filter
            var context = await WhenExecutingTheFilter(actionContext);

            // Then: Result is UnauthorizedObjectResult #E4
            ThenIsResult<UnauthorizedObjectResult>(context.Result, "E4");
        }

        [TestMethod]
        public async Task AuthHeaderOk()
        {
            // Given: Http context with basic auth header, and correct password
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"user:Password"));
            var actionContext = GivenActionContextWith(password: "Password", auth: $"Basic {base64}");

            // When: Executing the filter
            var context = await WhenExecutingTheFilter(actionContext);

            // Then: Result is not set
            Assert.IsNull(context.Result);
        }
    }
}
