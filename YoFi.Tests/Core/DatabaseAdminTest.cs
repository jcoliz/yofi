using Common.DotNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YoFi.Core;
using YoFi.Tests.Helpers;

namespace YoFi.Tests.Core
{
    [TestClass]
    public class DatabaseAdminTest
    {
        private DatabaseAdministration dbadmin;
        private MockDataContext context;
        private TestClock clock;

        [TestInitialize]
        public void SetUp()
        {
            clock = new TestClock() { IsLocked = true, Now = new System.DateTime(2022,1,1) };
            context = new MockDataContext();
            dbadmin = new DatabaseAdministration(context,clock);
        }

        [TestMethod]
        public void Empty()
        {
            Assert.IsNotNull(dbadmin);
        } 
    }
}