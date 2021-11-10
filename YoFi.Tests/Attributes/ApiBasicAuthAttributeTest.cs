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
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace YoFi.Tests.Attributes
{
    [TestClass]
    public class ApiBasicAuthAttributeTest
    {
        ApiBasicAuthorizationAttribute attribute;

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
            Assert.IsNotNull(context);
            var result = Assert.That.IsOfType<BadRequestObjectResult>(context.Result);

            // And: Message is {E1}
            var message = Assert.That.IsOfType<string>(result.Value);
            Assert.IsTrue(message.Contains("E1"));
        }
        [TestMethod]
        public async Task NoAuthHeader()
        {
            // Given: Http context with proper Api Key but no auth header
            var httpcontext = new DefaultHttpContext();
            var config = new Mock<IConfiguration>();
            config.Setup(x => x["Api:Key"]).Returns("Password");
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

            // Then: Result is UnauthorizedObjectResult
            Assert.IsNotNull(context);
            var result = Assert.That.IsOfType<UnauthorizedObjectResult>(context.Result);

            // And: Message is {E2}
            var message = Assert.That.IsOfType<string>(result.Value);
            Assert.IsTrue(message.Contains("E2"));
        }

        [TestMethod]
        public async Task AuthHeaderNotBasic()
        {
            // Given: Http context with auth header, but not "basic" auth
            var httpcontext = new DefaultHttpContext();
            var config = new Mock<IConfiguration>();
            config.Setup(x => x["Api:Key"]).Returns("Password");
            var services = new Mock<IServiceProvider>();
            services.Setup(x => x.GetService(typeof(IConfiguration))).Returns(config.Object);
            httpcontext.RequestServices = services.Object;
            httpcontext.Request.Headers.Add("Authorization", "Wrong");

            var actionContext = new ActionContext()
            {
                HttpContext = httpcontext,
                RouteData = new RouteData(),
                ActionDescriptor = new ActionDescriptor()
            };

            // When: Executing the filter
            AuthorizationFilterContext context = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
            await attribute.OnAuthorizationAsync(context);

            // Then: Result is UnauthorizedObjectResult
            Assert.IsNotNull(context);
            var result = Assert.That.IsOfType<UnauthorizedObjectResult>(context.Result);

            // And: Message is {E3}
            var message = Assert.That.IsOfType<string>(result.Value);
            Assert.IsTrue(message.Contains("E3"));
        }

        [TestMethod]
        public async Task AuthHeaderMiscodedPassword()
        {
            // Given: Http context with basic auth header, but not base64 password
            var httpcontext = new DefaultHttpContext();
            var config = new Mock<IConfiguration>();
            config.Setup(x => x["Api:Key"]).Returns("Password");
            var services = new Mock<IServiceProvider>();
            services.Setup(x => x.GetService(typeof(IConfiguration))).Returns(config.Object);
            httpcontext.RequestServices = services.Object;
            httpcontext.Request.Headers.Add("Authorization", "Basic Wrong");

            var actionContext = new ActionContext()
            {
                HttpContext = httpcontext,
                RouteData = new RouteData(),
                ActionDescriptor = new ActionDescriptor()
            };

            // When: Executing the filter
            AuthorizationFilterContext context = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
            await attribute.OnAuthorizationAsync(context);

            // Then: Result is UnauthorizedObjectResult
            Assert.IsNotNull(context);
            var result = Assert.That.IsOfType<UnauthorizedObjectResult>(context.Result);

            // And: Message is {E4}
            var message = Assert.That.IsOfType<string>(result.Value);
            Assert.IsTrue(message.Contains("E5"));
        }

        [TestMethod]
        public async Task AuthHeaderWrongPassword()
        {
            // Given: Http context with basic auth header, but wrong password, but correctly encoded
            var httpcontext = new DefaultHttpContext();
            var config = new Mock<IConfiguration>();
            config.Setup(x => x["Api:Key"]).Returns("Password");
            var services = new Mock<IServiceProvider>();
            services.Setup(x => x.GetService(typeof(IConfiguration))).Returns(config.Object);
            httpcontext.RequestServices = services.Object;

            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"user:Wrong"));
            httpcontext.Request.Headers.Add("Authorization", $"Basic {base64}");
            var actionContext = new ActionContext()
            {
                HttpContext = httpcontext,
                RouteData = new RouteData(),
                ActionDescriptor = new ActionDescriptor()
            };

            // When: Executing the filter
            AuthorizationFilterContext context = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
            await attribute.OnAuthorizationAsync(context);

            // Then: Result is UnauthorizedObjectResult
            Assert.IsNotNull(context.Result);
            var result = Assert.That.IsOfType<UnauthorizedObjectResult>(context.Result);

            // And: Message is {E4}
            var message = Assert.That.IsOfType<string>(result.Value);
            Assert.IsTrue(message.Contains("E4"));
        }

        [TestMethod]
        public async Task AuthHeaderOk()
        {
            // Given: Http context with basic auth header, and correct password
            var httpcontext = new DefaultHttpContext();
            var config = new Mock<IConfiguration>();
            var password = "Password";
            config.Setup(x => x["Api:Key"]).Returns(password);
            var services = new Mock<IServiceProvider>();
            services.Setup(x => x.GetService(typeof(IConfiguration))).Returns(config.Object);
            httpcontext.RequestServices = services.Object;

            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"user:{password}"));
            httpcontext.Request.Headers.Add("Authorization", $"Basic {base64}");
            var actionContext = new ActionContext()
            {
                HttpContext = httpcontext,
                RouteData = new RouteData(),
                ActionDescriptor = new ActionDescriptor()
            };

            // When: Executing the filter
            AuthorizationFilterContext context = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
            await attribute.OnAuthorizationAsync(context);

            // Then: Result is not set
            Assert.IsNull(context.Result);
        }
    }
}
