using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QuackAMole
{
    public class Result
    {
        public string Message = "";
        public bool Success = false;

        public Result(string message, bool succes)
        {
            this.Message = message;
            this.Success = succes;

        }

        public Result()
        { }
    }
}