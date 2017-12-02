using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpeedUp.Model
{
    public class ModuleLoginContainer
    {
        public short ModuleId { get; set; }
        public short BoardId { get; set; }
        public string DefFilePath { get; set; }
        public string ModuleName { get; set; }
        public string Vendor { get; set; }
        public string PropertyClass { get; set; }
        public string TraceName { get; set; }
        public string RdcCode { get; set; }
        public string LoginURL { get; set; }
        public string LoginUserName { get; set; }
        public string LoginPassword { get; set; }
        public string UserAgent { get; set; }
        public string UserAgentPW { get; set; }

    }
}
