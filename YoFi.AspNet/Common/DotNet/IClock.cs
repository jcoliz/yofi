using System;

namespace Common.DotNet
{
    public interface IClock
    {
        DateTime Now { get; }
    }
}
