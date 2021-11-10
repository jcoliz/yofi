using Common.AspNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Common.DotNet.Test;
using System.IO;
using System.Threading;
using Moq;

namespace YoFi.Tests.Attributes
{
    [TestClass]
    public class ValidateFilesProvidedAttributeTest
    {
        ValidateFilesProvidedAttribute attribute;

        private ActionExecutionDelegate Next => NextHandler;

        private int next_called;

        private Task<ActionExecutedContext> NextHandler()
        {
            ++next_called;

            return Task.FromResult<ActionExecutedContext>(null);
        }

        private void ThenOk(ActionExecutingContext context)
        {
            // Then: Context result is not set
            Assert.IsNull(context.Result);

            // And: Next was called
            Assert.AreEqual(1, next_called);
        }

        private void ThenBadRequest(ActionExecutingContext context)
        {
            // Then: Bad request or bad request object result
            Assert.IsNotNull(context);

            if (!(context.Result is BadRequestObjectResult))
                Assert.That.IsOfType<BadRequestResult>(context.Result);

            // And: Next never called
            Assert.AreEqual(0, next_called);
        }

        [TestInitialize]
        public void SetUp()
        {
            next_called = 0;
            attribute = new ValidateFilesProvidedAttribute();
        }

        [TestMethod]
        public void Empty()
        {
            // When: Creating a new object
            // (in setup)

            // Then: It's valid
            Assert.IsNotNull(attribute);
        }

        ActionExecutingContext GivenExecutingContextWithActionArguments(IDictionary<string,object> args)
        {
            // https://www.dotnetnakama.com/blog/creating-and-testing-asp-dotnet-core-filter-attributes/

            var actionContext = new ActionContext()
            {
                HttpContext = new DefaultHttpContext(),
                RouteData = new RouteData(),
                ActionDescriptor = new ActionDescriptor()
            };

            // Create the filter input parameters (depending on our case-scenario)

            var resultExecutingContext = new ActionExecutingContext(
                actionContext,
                    new List<IFilterMetadata>(),
                    args,
                    null //Mock.Of<Controller>()
                );

            return resultExecutingContext;
        }

        [TestMethod]
        public async Task NoFilesKey()
        {
            // Given: A result executing context with NO action arguments
            var resultExecutingContext = GivenExecutingContextWithActionArguments(new Dictionary<string, object>());

            // When: Executing the filter
            await attribute.OnActionExecutionAsync(context: resultExecutingContext, next: Next);

            // Then: Bad request result
            // And: Next never called
            ThenBadRequest(resultExecutingContext);
        }

        [TestMethod]
        public async Task FilesKeyNull()
        {
            // Given: A result executing context with action arguments: files = null
            var resultExecutingContext = GivenExecutingContextWithActionArguments(new Dictionary<string, object>() { { "files",null } });

            // When: Executing the filter
            await attribute.OnActionExecutionAsync(context: resultExecutingContext, next: Next);

            // Then: Bad request result
            // And: Next never called
            ThenBadRequest(resultExecutingContext);
        }

        [TestMethod]
        public async Task FilesKeyWrongType()
        {
            // Given: A result executing context with action arguments: files = {string}
            var resultExecutingContext = GivenExecutingContextWithActionArguments(new Dictionary<string, object>() { { "files", "string" } });

            // When: Executing the filter
            await attribute.OnActionExecutionAsync(context: resultExecutingContext, next: Next);

            // Then: Bad request result
            // And: Next never called
            ThenBadRequest(resultExecutingContext);
        }
        [TestMethod]
        public async Task FilesKeyEmptyList()
        {
            // Given: A result executing context with action arguments: files = {string}
            var resultExecutingContext = GivenExecutingContextWithActionArguments(new Dictionary<string, object>() { { "files", new List<IFormFile>() } });

            // When: Executing the filter
            await attribute.OnActionExecutionAsync(context: resultExecutingContext, next: Next);

            // Then: Bad request result
            // And: Next never called
            ThenBadRequest(resultExecutingContext);
        }

        [TestMethod]
        public async Task FilesSingleOK()
        {
            // Given: A result executing context with action arguments: files = {one file}
            var file = new Mock<IFormFile>();
            var resultExecutingContext = GivenExecutingContextWithActionArguments(new Dictionary<string, object>() { { "files", new List<IFormFile>() { file.Object } } });

            // When: Executing the filter
            await attribute.OnActionExecutionAsync(context: resultExecutingContext, next: Next);

            // Then: Context result is not set
            // And: Next was called
            ThenOk(resultExecutingContext);
        }

        [TestMethod]
        public async Task FilesMultipleOK()
        {
            // Given: A result executing context with action arguments: files = {two files}
            var file = new Mock<IFormFile>();
            var resultExecutingContext = GivenExecutingContextWithActionArguments(new Dictionary<string, object>() { { "files", new List<IFormFile>() { file.Object, file.Object } } });

            // When: Executing the filter
            await attribute.OnActionExecutionAsync(context: resultExecutingContext, next: Next);

            // Then: Context result is not set
            // And: Next was called
            ThenOk(resultExecutingContext);
        }

        [TestMethod]
        public async Task FilesMultipleNotOK()
        {
            // Given: Object under test does not allow multiple files
            attribute = new ValidateFilesProvidedAttribute(multiplefilesok:false);

            // And: A result executing context with action arguments: files = {two files}
            var file = new Mock<IFormFile>();
            var resultExecutingContext = GivenExecutingContextWithActionArguments(new Dictionary<string, object>() { { "files", new List<IFormFile>() { file.Object, file.Object } } });

            // When: Executing the filter
            await attribute.OnActionExecutionAsync(context: resultExecutingContext, next: Next);

            // Then: Bad request result
            // And: Next never called
            ThenBadRequest(resultExecutingContext);
        }

    }
}
