using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using YoFi.AspNet.Pages;

namespace YoFi.Tests.Pages
{
    [TestClass]
    public class PageModelTest
    {
        HelpModel pagemodel;

        [TestInitialize]
        public void SetUp()
        {   
            pagemodel = new HelpModel();
        }

        [TestMethod]
        public void Empty()
        {
            // When: Creating a new object
            // (in setup)

            // Then: It's valid
            Assert.IsNotNull(pagemodel);
        }

        [DataRow("payees")]
        [DataRow("import")]
        [DataRow("budget")]
        [DataTestMethod]
        public void TopicFound(string topic)
        {
            // When: getting help for {topic}
            pagemodel.OnGet(topic);

            // Then: title matches the topic
            Assert.IsTrue(pagemodel.Topic.Title.ToLower().Contains(topic[..2]));

            // And: The properties are all non-null
            Assert.IsTrue(pagemodel.Topic.Contents.Any());
            Assert.IsTrue(pagemodel.Topic.Extended.Any());

        }

        [TestMethod]
        public void TopicBlankNotFound()
        {
            // When: getting help for (null)
            pagemodel.OnGet(null);

            // Then: "Sorry" returned as title
            Assert.AreEqual("Sorry",pagemodel.Topic.Title);

            // And: Contents is non-null
            Assert.IsTrue(pagemodel.Topic.Contents.Any());
        }
    }
}
