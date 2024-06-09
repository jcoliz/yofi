using System;

namespace Common.DotNet;

public class SystemClock : IClock
{
    public DateTime Now => DateTime.Now;
}
