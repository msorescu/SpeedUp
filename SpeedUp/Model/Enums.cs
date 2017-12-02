using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpeedUp.Model
{
    public class Enums
    {
        public enum CheckAccountReturnCode
        {
            Success,
            RequestFailed,
            RequestFailed60310,
            RequestFailed60320,
            NoRecordFound
        }
    }
}
