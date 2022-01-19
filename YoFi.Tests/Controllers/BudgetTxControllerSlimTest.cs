using Common.AspNet;
using Common.DotNet.Test;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoFi.AspNet.Controllers;
using YoFi.Core.Models;
using YoFi.Core.Repositories;
using YoFi.Tests.Helpers;

namespace YoFi.Tests.Controllers.Slim
{
    /// <summary>
    /// Test the budget tx controller in the slimmest way possible
    /// </summary>
    /// <remarks>
    /// This variation of controller test mocks out the underlying repository
    /// </remarks>
    [TestClass]
    public class BudgetTxControllerSlimTest : BaseControllerSlimTest<BudgetTx>
    {
        private BudgetTxsController itemController => base.controller as BudgetTxsController;
        private MockBudgetTxRepository itemRepository  => base.repository as MockBudgetTxRepository;

        [TestInitialize]
        public void SetUp()
        {
            repository = new MockBudgetTxRepository();
            controller = new BudgetTxsController(repository as IBudgetTxRepository);
        }

        [TestMethod]
        public async Task IndexPage1()
        {
            // Given: A very long set of items 
            var numitems = 100;
            repository.AddItems(numitems);

            // When: Calling Index page 1
            var actionresult = await itemController.Index(p: 1);

            // Then: View is returned
            var viewresult = Assert.That.IsOfType<ViewResult>(actionresult);

            // And: Correct kind of model is returned 
            var model = Assert.That.IsOfType<IEnumerable<BudgetTx>>(viewresult.Model);

            // And: Model has one page of items
            Assert.AreEqual(BudgetTxsController.PageSize, model.Count());

            // And: Page Item values are as expected
            var pages = viewresult.ViewData[nameof(PageDivider)] as PageDivider;
            Assert.AreEqual(1, pages.PageFirstItem);
            Assert.AreEqual(BudgetTxsController.PageSize, pages.PageLastItem);
            Assert.AreEqual(numitems, pages.PageTotalItems);
        }

        [TestMethod]
        public async Task IndexPage2()
        {
            // Given: A long set of items, which is longer than one page, but not as long as two pages 
            var itemcount = BudgetTxsController.PageSize + PayeesController.PageSize / 2;
            repository.AddItems(itemcount);

            // When: Calling Index page 2
            var actionresult = await itemController.Index(p: 2);

            // Then: View is returned
            var viewresult = Assert.That.IsOfType<ViewResult>(actionresult);

            // And: Correct kind of model is returned 
            var model = Assert.That.IsOfType<List<BudgetTx>>(viewresult.Model);

            // And: Only items after one page's worth of items are returned
            Assert.AreEqual(BudgetTxsController.PageSize / 2, model.Count);

            // And: Page Item values are as expected
            var pages = viewresult.ViewData[nameof(PageDivider)] as PageDivider;
            Assert.AreEqual(1 + BudgetTxsController.PageSize, pages.PageFirstItem);
            Assert.AreEqual(itemcount, pages.PageLastItem);
            Assert.AreEqual(itemcount, pages.PageTotalItems);
        }

        [TestMethod]
        public async Task IndexQSubstring()
        {
            // Given: A mix of items, some with '{word}' in their category and some without
            repository.AddItems(11);

            // When: Calling index q={word}
            var word = "1";
            var actionresult = await itemController.Index(q: word);
            var viewresult = Assert.That.IsOfType<ViewResult>(actionresult);
            var model = Assert.That.IsOfType<List<BudgetTx>>(viewresult.Model);

            // Then: Only the exoected items are returned
            var expected = repository.All.Where(x => x.Category.Contains(word));
            Assert.IsTrue(expected.SequenceEqual(model));
        }
        [TestMethod]
        public async Task BulkDelete()
        {
            // When: Calling BulkDelete
            var actionresult = await itemController.BulkDelete();

            // Then: Returns a redirection to Index
            var redirresult = Assert.That.IsOfType<RedirectToActionResult>(actionresult);
            Assert.AreEqual("Index", redirresult.ActionName);

            // And: Bulk edit operation was performed
            Assert.IsTrue(itemRepository.WasBulkDeleteCalled);
        }
    }
}
