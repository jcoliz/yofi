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
using YoFi.Core;

namespace YoFi.Tests.Attributes
{
    [TestClass]
    public class ValidateStorageAttributeTest
    {
        private ValidateStorageAvailableAttribute attribute;

        private ActionExecutionDelegate Next => NextHandler;

        private int next_called;

        private Task<ActionExecutedContext> NextHandler()
        {
            ++next_called;

            return Task.FromResult<ActionExecutedContext>(null);
        }

        ActionExecutingContext GivenExecutingContextWithActionArguments(bool hasstorage = false, bool haskey = false)
        {
            // https://www.dotnetnakama.com/blog/creating-and-testing-asp-dotnet-core-filter-attributes/

            var httpcontext = new DefaultHttpContext();

            var services = new Mock<IServiceProvider>();

            if (hasstorage)
            {
                var storage = new Mock<IStorageService>();
                services.Setup(x => x.GetService(It.Is<Type>(t=> t== typeof(IStorageService)))).Returns(storage.Object);

                var config = new Mock<IConfiguration>();

                if (haskey)
                {
                    config.Setup(x => x["Storage:BlobContainerName"]).Returns("Ok");
                }

                services.Setup(x => x.GetService(It.Is<Type>(t => t == typeof(IConfiguration)))).Returns(config.Object);
            }
            else
            {
                services.Setup(x => x.GetService(It.IsAny<Type>())).Returns(null);
            }

            httpcontext.RequestServices = services.Object;

            var actionContext = new ActionContext()
            {
                HttpContext = httpcontext,
                RouteData = new RouteData(),
                ActionDescriptor = new ActionDescriptor()
            };

            // Create the filter input parameters (depending on our case-scenario)

            var resultExecutingContext = new ActionExecutingContext(
                actionContext,
                    new List<IFilterMetadata>(),
                    new Dictionary<string, object>(),
                    null //Mock.Of<Controller>()
                );

            return resultExecutingContext;
        }

        private void ThenBadRequest(ActionExecutingContext context, string code)
        {
            // Then: bad request object result
            var result = Assert.That.IsOfType<BadRequestObjectResult>(context.Result);
            var message = Assert.That.IsOfType<string>(result.Value);

            // And: Message contains supplied {code}
            Assert.IsTrue(message.Contains(code));

            // And: Next never called
            Assert.AreEqual(0, next_called);
        }

        [TestInitialize]
        public void SetUp()
        {
            //next_called = 0;
            attribute = new ValidateStorageAvailableAttribute();
        }

        [TestMethod]
        public void Empty()
        {
            // When: Creating a new object
            // (in setup)

            // Then: It's valid
            Assert.IsNotNull(attribute);
        }

        [TestMethod]
        public async Task NoStorage()
        {
            // Given: A result executing context with nothing special
            var resultExecutingContext = GivenExecutingContextWithActionArguments();

            // When: Executing the filter
            await attribute.OnActionExecutionAsync(context: resultExecutingContext, next: Next);

            // Then: Bad request result
            // And: MEssage contains code "E1"
            // And: Next never called
            ThenBadRequest(resultExecutingContext,"E1");
        }

        [TestMethod]
        public async Task NoConfigKey()
        {
            // Given: A result executing context with storage only, no config key
            var resultExecutingContext = GivenExecutingContextWithActionArguments(hasstorage:true);

            // When: Executing the filter
            await attribute.OnActionExecutionAsync(context: resultExecutingContext, next: Next);

            // Then: Bad request result
            // And: MEssage contains code "E2"
            // And: Next never called
            ThenBadRequest(resultExecutingContext, "E2");
        }

        [TestMethod]
        public async Task Ok()
        {
            // Given: A result executing context with storage and config key
            var resultExecutingContext = GivenExecutingContextWithActionArguments(hasstorage: true, haskey: true);

            // When: Executing the filter
            await attribute.OnActionExecutionAsync(context: resultExecutingContext, next: Next);

            // Then: No result set
            Assert.IsNull(resultExecutingContext.Result);

            // And: Next called once
            Assert.AreEqual(1, next_called);
        }
    }

}
