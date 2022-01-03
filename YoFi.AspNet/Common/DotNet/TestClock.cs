using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Common.DotNet
{
    public class TestClock: IClock
    {
        public bool IsLocked { get; set; } = false;

        private bool IsSet = false;

        private TimeSpan Offset = TimeSpan.Zero;

        private DateTime Explict = default;

        public DateTime Now
        {
            set
            {
                // Do this first before more time passes
                var offset = value - DateTime.Now;

                IsSet = true;
                if (IsLocked)
                {
                    Explict = value;
                }
                else
                {
                    Offset = offset;
                }
            }
            get
            {
                var modified = DateTime.Now + Offset;

                if (IsSet && IsLocked)
                    return Explict;
                else
                    return modified;
            }
        }
    }
}
