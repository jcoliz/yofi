using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Core.Repositories;
using YoFi.Core.Repositories.Wire;
using YoFi.Tests.Helpers;

namespace YoFi.Tests.Core
{
    [TestClass]
    public class SplitRepositoryTest
    {
        #region Fields

        private MockDataContext context;
        private BaseRepository<Split> repository;

        #endregion

        #region Init/Cleanup

        [TestInitialize]
        public void SetUp()
        {
            context = new MockDataContext();
            repository = new BaseRepository<Split>(context);
        }

        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public async Task ForQueryFails()
        {
            // You can create a base repository of splits, but you can't do much with it!

            _ = await repository.GetByQueryAsync(new WireQueryParameters() { Query = "Hello" } );
        }

        #endregion
    }
}
