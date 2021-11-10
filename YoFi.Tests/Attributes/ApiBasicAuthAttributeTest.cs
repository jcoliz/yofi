using Common.AspNet;
using Common.DotNet.Test;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace YoFi.Tests.Attributes
{
    [TestClass]
    public class ApiBasicAuthAttributeTest
    {
        ApiBasicAuthorizationAttribute attribute;

        private void ThenBadRequest(AuthorizationFilterContext context)
        {
            // Then: Bad request or bad request object result
            Assert.IsNotNull(context);

            if (!(context.Result is BadRequestObjectResult))
                Assert.That.IsOfType<BadRequestResult>(context.Result);
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
            // Given: Http context with no configuration parameters
            var httpcontext = new DefaultHttpContext();
            var config = new Mock<IConfiguration>();
            var services = new Mock<IServiceProvider>();
            services.Setup(x => x.GetService(typeof(IConfiguration))).Returns(config.Object);
            httpcontext.RequestServices = services.Object;

            var actionContext = new ActionContext()
            {
                HttpContext = httpcontext,
                RouteData = new RouteData(),
                ActionDescriptor = new ActionDescriptor()
            };

            // When: Executing the filter
            AuthorizationFilterContext context = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
            await attribute.OnAuthorizationAsync(context);

            // Then: Result is bad request
            ThenBadRequest(context);
        }
    }
}
