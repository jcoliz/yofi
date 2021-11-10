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

namespace YoFi.Tests.Attributers
{
    [TestClass]
    public class ValidateFilesProvidedAttributeTest
    {
        ValidateFilesProvidedAttribute attribute;

        [TestInitialize]
        public void SetUp()
        {   
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
            int next_called = 0;
            await attribute.OnActionExecutionAsync(context: resultExecutingContext, next:() => { ++next_called; return null; } ) ;

            // Then: Bad request result
            Assert.That.IsOfType<BadRequestResult>(resultExecutingContext.Result);

            // And: Next never called
            Assert.AreEqual(0, next_called);
        }

        [TestMethod]
        public async Task FilesKeyNull()
        {
            // Given: A result executing context with action arguments: files = null
            var resultExecutingContext = GivenExecutingContextWithActionArguments(new Dictionary<string, object>() { { "files",null } });

            // When: Executing the filter
            int next_called = 0;
            await attribute.OnActionExecutionAsync(context: resultExecutingContext, next: () => { ++next_called; return null; });

            // Then: Bad request result
            Assert.That.IsOfType<BadRequestResult>(resultExecutingContext.Result);

            // And: Next never called
            Assert.AreEqual(0, next_called);
        }

        [TestMethod]
        public async Task FilesKeyWrongType()
        {
            // Given: A result executing context with action arguments: files = {string}
            var resultExecutingContext = GivenExecutingContextWithActionArguments(new Dictionary<string, object>() { { "files", "string" } });

            // When: Executing the filter
            int next_called = 0;
            await attribute.OnActionExecutionAsync(context: resultExecutingContext, next: () => { ++next_called; return null; });

            // Then: Bad request result
            Assert.That.IsOfType<BadRequestResult>(resultExecutingContext.Result);

            // And: Next never called
            Assert.AreEqual(0, next_called);
        }
        [TestMethod]
        public async Task FilesKeyEmptyList()
        {
            // Given: A result executing context with action arguments: files = {string}
            var resultExecutingContext = GivenExecutingContextWithActionArguments(new Dictionary<string, object>() { { "files", new List<IFormFile>() } });

            // When: Executing the filter
            int next_called = 0;
            await attribute.OnActionExecutionAsync(context: resultExecutingContext, next: () => { ++next_called; return null; });

            // Then: Bad request result
            Assert.That.IsOfType<BadRequestObjectResult>(resultExecutingContext.Result);

            // And: Next never called
            Assert.AreEqual(0, next_called);
        }

        public class MockFile : IFormFile
        {
            string IFormFile.ContentDisposition => throw new System.NotImplementedException();

            string IFormFile.ContentType => throw new System.NotImplementedException();

            string IFormFile.FileName => throw new System.NotImplementedException();

            IHeaderDictionary IFormFile.Headers => throw new System.NotImplementedException();

            long IFormFile.Length => throw new System.NotImplementedException();

            string IFormFile.Name => throw new System.NotImplementedException();

            void IFormFile.CopyTo(Stream target)
            {
                throw new System.NotImplementedException();
            }

            Task IFormFile.CopyToAsync(Stream target, CancellationToken cancellationToken)
            {
                throw new System.NotImplementedException();
            }

            Stream IFormFile.OpenReadStream()
            {
                throw new System.NotImplementedException();
            }
        }

    }
}
