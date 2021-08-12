using Microsoft.VisualStudio.TestTools.UnitTesting;
using YoFi.AspNet.Models;
using System.Collections.Generic;
using System.Linq;

namespace YoFi.Tests
{
    [TestClass]
    public class CategoryMapperTest
    {
        CategoryMapper mapper;

        [TestInitialize]
        public void SetUp()
        {
            var maps = new List<CategoryMap>()
            {
                new CategoryMap() { Category = "A", Key1 = "1", Key2 = "2" },
                new CategoryMap() { Category = "B", SubCategory = "C", Key1 = "3", Key2 = "4" },
                new CategoryMap() { Category = "D", SubCategory = "E", Key1 = "5", Key2 = "6", Key3 = "7" },
                new CategoryMap() { Category = "F", Key1 = "12" },
                new CategoryMap() { Category = "H", Key1 = "10", Key2 = "^([^\\.]*)", Key3 = "^[^\\.]*\\.(.+)" },
                new CategoryMap() { Category = "I", SubCategory = "J", Key1 = "11" },
                new CategoryMap() { Category = "J", Key1 = "12", Key2 = "13" },
                new CategoryMap() { Category = "J", SubCategory = "^K", Key1 = "14", Key2 = "15", Key3 = "^[^\\.]*\\.(.+)" },
            };

            mapper = new CategoryMapper(maps.AsQueryable());
        }

        [DataRow("A", "B", "1:2:B:")]
        [DataRow("B", "C", "3:4:C:")]
        [DataRow("B", "X", "Unmapped:B:X:")]
        [DataRow("D", "E", "5:6:7:")]
        [DataRow("D", "X", "Unmapped:D:X:")]
        [DataRow("J", "K.FOO", "14:15:FOO:")]
        [DataRow("J", "X", "12:13:X:")]
        [DataRow("F", null, "12:F::")]
        [DataRow("F", "X", "12:X::")]
        [DataRow("H", "GOO.FOO", "10:GOO:FOO:")]
        [DataRow("H", "GOO", "10:GOO::")]
        [DataRow("H", "Bob Sauce", "10:Bob Sauce::")]
        [DataRow("H", null, "10:::")]
        [DataRow("I", "J", "11:J::")]
        [DataRow("I", "X", "Unmapped:I:X:")]
        [DataRow("J", null, "12:13::")]
        [DataRow("J", "X", "12:13:X:")]
        [DataRow("X", null, "Unmapped:X::")]
        [DataRow("X", "Y", "Unmapped:X:Y:")]
        [DataRow("X:Y", null, "X:Y::")]
        [DataRow("X:Y", "Z", "X:Y:Z:")]
        [DataRow("X:Y", "Z:R", "X:Y:Z:R")]
        [DataRow("X:Y:Z", null, "X:Y:Z:")]
        [DataRow("X:Y:Z:R", null, "X:Y:Z:R")]
        [DataTestMethod]
        public void MapCatSubcat(string category, string subcategory, string expected)
        {
            var result = mapper.KeysFor(category,subcategory);

            var actual = string.Join(':',result);

            Assert.AreEqual(expected, actual);
        }

        [DataRow("A", "B", "1:2:B")]
        [DataRow("B", "C", "3:4:C")]
        [DataRow("B", "X", "Unmapped:B:X")]
        [DataRow("D", "E", "5:6:7")]
        [DataRow("D", "X", "Unmapped:D:X")]
        [DataRow("J", "K.FOO", "14:15:FOO")]
        [DataRow("J", "X", "12:13:X")]
        [DataRow("F", null, "12:F")]
        [DataRow("F", "X", "12:X")]
        [DataRow("H", "GOO.FOO", "10:GOO:FOO")]
        [DataRow("H", "GOO", "10:GOO")]
        [DataRow("H", "Bob Sauce", "10:Bob Sauce")]
        [DataRow("H", null, "10")]
        [DataRow("I", "J", "11:J")]
        [DataRow("I", "X", "Unmapped:I:X")]
        [DataRow("J", null, "12:13")]
        [DataRow("J", "X", "12:13:X")]
        [DataRow("X", null, "Unmapped:X")]
        [DataRow("X", "Y", "Unmapped:X:Y")]
        [DataRow("X:Y", null, "X:Y")]
        [DataRow("X:Y", "Z", "X:Y:Z")]
        [DataRow("X:Y", "Z:R", "X:Y:Z:R")]
        [DataRow("X:Y:Z", null, "X:Y:Z")]
        [DataRow("X:Y:Z:R", null, "X:Y:Z:R")]
        [DataTestMethod]

        public void MapTransaction(string category, string subcategory, string expected)
        {
            var transaction = new Transaction() { Category = category, SubCategory = subcategory };
            mapper.MapObject(transaction);

            Assert.AreEqual(expected, transaction.Category);
            Assert.IsNotNull(expected, transaction.SubCategory);
        }

        [DataRow("A", "B", "1:2:B")]
        [DataRow("B", "C", "3:4:C")]
        [DataRow("B", "X", "Unmapped:B:X")]
        [DataRow("D", "E", "5:6:7")]
        [DataRow("D", "X", "Unmapped:D:X")]
        [DataRow("J", "K.FOO", "14:15:FOO")]
        [DataRow("J", "X", "12:13:X")]
        [DataRow("F", null, "12:F")]
        [DataRow("F", "X", "12:X")]
        [DataRow("H", "GOO.FOO", "10:GOO:FOO")]
        [DataRow("H", "GOO", "10:GOO")]
        [DataRow("H", "Bob Sauce", "10:Bob Sauce")]
        [DataRow("H", null, "10")]
        [DataRow("I", "J", "11:J")]
        [DataRow("I", "X", "Unmapped:I:X")]
        [DataRow("J", null, "12:13")]
        [DataRow("J", "X", "12:13:X")]
        [DataRow("X", null, "Unmapped:X")]
        [DataRow("X", "Y", "Unmapped:X:Y")]
        [DataRow("X:Y", null, "X:Y")]
        [DataRow("X:Y", "Z", "X:Y:Z")]
        [DataRow("X:Y", "Z:R", "X:Y:Z:R")]
        [DataRow("X:Y:Z", null, "X:Y:Z")]
        [DataRow("X:Y:Z:R", null, "X:Y:Z:R")]
        [DataTestMethod]

        public void MapPayee(string category, string subcategory, string expected)
        {
            var payee = new Payee() { Category = category, SubCategory = subcategory };
            mapper.MapObject(payee);

            Assert.AreEqual(expected, payee.Category);
            Assert.IsNotNull(expected, payee.SubCategory);
        }

    }
}
