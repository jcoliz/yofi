using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Common.AspNet
{
    /// <summary>
    /// Standard paramters that viewmodels can implement
    /// </summary>
    public interface IViewParameters
    {
        string QueryParameter { get; }
        string ViewParameter { get; }
        public string OrderParameter { get; }
    }
}
