using System;
using System.Collections.Generic;

namespace B1TuneUp.Models
{
    public class UniversalFunctionTestResult
    {
        public string FunctionCode { get; set; }
        public string FunctionType { get; set; }
        public bool Success { get; set; }
        public string Result { get; set; }
        public string Error { get; set; }
        public DateTime StartedAtUtc { get; set; }
        public DateTime FinishedAtUtc { get; set; }
        public IList<string> Chain { get; set; } = new List<string>();
    }
}
