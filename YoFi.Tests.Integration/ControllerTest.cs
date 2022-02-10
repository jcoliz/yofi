using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YoFi.Tests.Integration
{
    [TestClass]

    public abstract class ControllerTest<T>: IntegrationTest
    {
        protected abstract string urlroot { get; }

        [TestMethod]
        public async Task IndexEmpty()
        {
            // Given: Empty database

            // When: Getting the index
            var document = await WhenGetAsync($"{urlroot}/");

            // Then: No items are returned
            ThenResultsAreEqualByTestKey(document, Enumerable.Empty<T>());
        }
    }
}
