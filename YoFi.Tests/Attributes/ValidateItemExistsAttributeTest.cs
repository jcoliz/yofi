using Common.DotNet.Test;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoFi.AspNet.Controllers;
using YoFi.Core.Models;
using YoFi.Core.Repositories;

namespace YoFi.Tests.Attributes
{
    [TestClass]
    public class ValidateItemExistsAttributeTest
    {
        private ValidateTransactionExistsAttribute txattribute;
        private IAsyncActionFilter filter;

        private ActionExecutionDelegate Next => NextHandler;

        private int next_called;

        private Task<ActionExecutedContext> NextHandler()
        {
            ++next_called;

            return Task.FromResult<ActionExecutedContext>(null);
        }

        ActionExecutingContext GivenExecutingContextWithActionArguments(IDictionary<string, object> args)
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

        private void ThenBadRequest(IActionResult result)
        {
            // Then: Bad request or bad request object result
            Assert.IsNotNull(result);

            if (!(result is BadRequestObjectResult))
                Assert.That.IsOfType<BadRequestResult>(result);

            // And: Next never called
            Assert.AreEqual(0, next_called);
        }

        [TestInitialize]
        public void SetUp()
        {
            txattribute = new ValidateTransactionExistsAttribute();

            var repository = new Mock<IRepository<Transaction>>();
            repository.Setup(x => x.TestExistsByIdAsync(1)).Returns(Task.FromResult(true));
            repository.Setup(x => x.TestExistsByIdAsync(2)).Returns(Task.FromResult(false));

            var services = new Mock<IServiceProvider>();
            services.Setup(x => x.GetService(typeof(IRepository<Transaction>))).Returns(repository.Object);

            var metadata = txattribute.CreateInstance(services.Object);

            filter = Assert.That.IsOfType<IAsyncActionFilter>(metadata);
        }

        [TestMethod]
        public void Empty()
        {
            Assert.IsNotNull(txattribute);
            Assert.IsNotNull(filter);
        }

        [TestMethod]
        public async Task NoIdKey()
        {
            // Given: A result executing context with NO action arguments
            var resultExecutingContext = GivenExecutingContextWithActionArguments(new Dictionary<string, object>());

            // When: Executing the filter
            await filter.OnActionExecutionAsync(context: resultExecutingContext, next: Next);

            // Then: Bad request result
            // And: Next never called
            ThenBadRequest(resultExecutingContext.Result);
        }

        [TestMethod]
        public async Task IdNotInt()
        {
            // Given: A result executing context with action arguments id = {string}
            var resultExecutingContext = GivenExecutingContextWithActionArguments(new Dictionary<string, object>() { { "id", "string" } });

            // When: Executing the filter
            await filter.OnActionExecutionAsync(context: resultExecutingContext, next: Next);

            // Then: Bad request result
            // And: Next never called
            ThenBadRequest(resultExecutingContext.Result);
        }

        [TestMethod]
        public async Task IdNull()
        {
            // Given: A result executing context with action arguments id = {string}
            var resultExecutingContext = GivenExecutingContextWithActionArguments(new Dictionary<string, object>() { { "id", null } });

            // When: Executing the filter
            await filter.OnActionExecutionAsync(context: resultExecutingContext, next: Next);

            // Then: Bad request result
            // And: Next never called
            ThenBadRequest(resultExecutingContext.Result);
        }

        [TestMethod]
        public async Task IdNotExists()
        {
            // Given: A result executing context with action arguments id = {string}
            var resultExecutingContext = GivenExecutingContextWithActionArguments(new Dictionary<string, object>() { { "id", 2 } });

            // When: Executing the filter
            await filter.OnActionExecutionAsync(context: resultExecutingContext, next: Next);

            // Then: Not found result
            Assert.That.IsOfType<NotFoundResult>(resultExecutingContext.Result);

            // And: Next never called
            Assert.AreEqual(0, next_called);
        }

        [TestMethod]
        public async Task IdExists()
        {
            // Given: A result executing context with action arguments id = {string}
            var resultExecutingContext = GivenExecutingContextWithActionArguments(new Dictionary<string, object>() { { "id", 1 } });

            // When: Executing the filter
            await filter.OnActionExecutionAsync(context: resultExecutingContext, next: Next);

            // Then: No result set
            Assert.IsNull(resultExecutingContext.Result);

            // And: Next called once
            Assert.AreEqual(1, next_called);
        }

        [TestMethod]
        public void PayeeExists()
        {
            var attribute = new ValidatePayeeExistsAttribute();
            Assert.IsNotNull(attribute);
        }

        [TestMethod]
        public void BudgetTxExists()
        {
            var attribute = new ValidateBudgetTxExistsAttribute();
            Assert.IsNotNull(attribute);
        }

        [TestMethod]
        public void SplitExists()
        {
            var attribute = new ValidateSplitExistsAttribute();
            Assert.IsNotNull(attribute);
        }
    }

}
