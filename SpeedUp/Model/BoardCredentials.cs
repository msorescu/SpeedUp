using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpeedUp.Model
{
    public class BoardCredentials
    {
        public short ModuleId { get; set; }
        public short BoardId { get; set; }
        public string ModuleName { get; set; }
        public string LoginType { get; set; }
        public string RETSURL { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string UserAgent { get; set; }
        public string UserAgentPass { get; set; }
        public string DatasourceId { get; set; }
        public string MainDatasourceId { get; set; }
        public bool isRets { get; set; }
        public bool IsValidCredential { get; set; }
    }
}
