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

        [TestMethod]
        public async Task NoFilesKey()
        {
            // Given: A result executing context with NO action arguments

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
                    new Dictionary<string,object>(),
                    null //Mock.Of<Controller>()
                );

            // When: Executing the filter
            int next_called = 0;
            await attribute.OnActionExecutionAsync(context: resultExecutingContext, next:() => { ++next_called; return null; } ) ;

            // Then: Bad request result
            Assert.That.IsOfType<BadRequestResult>(resultExecutingContext.Result);

            // And: Next never called
            Assert.AreEqual(0, next_called);
        }

    }
}
