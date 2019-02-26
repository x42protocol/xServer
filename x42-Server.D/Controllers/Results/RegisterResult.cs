using System;
using System.Collections.Generic;

namespace X42.Controllers.Results
{
    public class RegisterResult
    {
        public RegisterResult()
        {
        }

        public bool Success { get; set; }

        public string FailReason { get; set; }
    }
}