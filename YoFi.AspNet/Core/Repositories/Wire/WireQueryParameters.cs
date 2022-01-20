using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YoFi.Core.Repositories.Wire
{
    /// <summary>
    /// Default implementation of IWireQueryParameters
    /// </summary>
    public class WireQueryParameters : IWireQueryParameters
    {
        public string Query { get; set; }

        public int? Page { get; set; }

        public string Order { get; set; }

        public string View { get; set; }

        public bool All { get; set; }
    }
}
