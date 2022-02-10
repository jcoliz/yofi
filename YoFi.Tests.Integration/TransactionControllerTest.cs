using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Tests.Integration.Helpers;

namespace YoFi.Tests.Integration
{
    [TestClass]
    public class TransactionControllerTest : ControllerTest<Transaction>
    {
        protected override string urlroot => "/Transactions";

        #region Init/Cleanup

        [ClassInitialize]
        public static void InitialSetup(TestContext tcontext)
        {
            integrationcontext = new IntegrationContext(tcontext.FullyQualifiedTestClassName);
        }

        [ClassCleanup]
        public static void FinalCleanup()
        {
            integrationcontext.Dispose();
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Clean out database
            context.Set<Transaction>().RemoveRange(context.Set<Transaction>());
            context.SaveChanges();
        }

        #endregion

        #region

        // Need to hide download test. Works differently for transactions
        public override Task Download()
        {
           return Task.CompletedTask;
        }


        public override Task Upload()
        {
            return Task.CompletedTask;
        }

        public override Task UploadDuplicate()
        {
            return Task.CompletedTask;
        }

        public override Task UploadFailsNoFile()
        {
            return Task.CompletedTask;
        }

        #endregion
    }
}
