using System;

namespace Common.DotNet;

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
            Explict = value;
            Offset = offset;
        }
        get
        {
            if (IsSet && IsLocked)
                return Explict;
            else
                return DateTime.Now + Offset;
        }
    }

    public void Reset()
    {
        IsSet = false;
        Explict = default;
        IsLocked = false;
        Offset = TimeSpan.Zero;
    }
}
