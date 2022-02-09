using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YoFi.Core.Models.Attributes
{
    // Marks that a property can be used as the single key value for equality
    // in tests
    [AttributeUsage(AttributeTargets.Property)]
    public class TestKeyAttribute: Attribute
    {
        public TestKeyAttribute()
        {
        }
    }
}
