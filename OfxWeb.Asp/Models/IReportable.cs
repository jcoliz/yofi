﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OfxWeb.Asp.Models
{
    public interface IReportable
    {
        decimal Amount { get; }
        DateTime Timestamp { get; }
        string Category { get; }
    }
}
