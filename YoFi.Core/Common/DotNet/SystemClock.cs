using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Common.DotNet
{
    public class SystemClock : IClock
    {
        public DateTime Now => DateTime.Now;
    }
}
