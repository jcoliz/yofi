using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YoFi.Core.Models.Attributes
{
    // Marks that a property can be used as the single key value for equality
    // in tests.
    //
    // Also note that you should construct test data such that
    // ordering by ascending testkey equals ordering by IModelItem.InDefaultOrder()
    //
    // Also note that the repositories are expected to honor q={test key} for searching

    [AttributeUsage(AttributeTargets.Property)]
    public class TestKeyAttribute: Attribute
    {
        public TestKeyAttribute()
        {
        }
    }
}
