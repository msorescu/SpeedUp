using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpeedUp.Model
{
    public class TigerLeadReportRow
    {
        public string TigerLeadCode { get; set; }
        public string TigerLeadName { get; set; }
        public string RDCID { get; set; }
        public string ModuleID { get; set; }
        public string Notes { get; set; }
        public string ORCAReadyMatch { get; set; }
        public string TPOnlyMatch { get; set; }
        public string ORCAReadyExtra { get; set; }
        public string TPOnlyExtra { get; set; }
        public string TigerLeadOnly { get; set; }
        public string TigerLeadCodeOld { get; set; }
        public string TigerLeadNameOld { get; set; }
        public int ORCAReadyMatchCount { get; set; }
        public int TPOnlyMatchCount { get; set; }
        public int ORCAReadyExtraCount { get; set; }
        public int TPOnlyExtraCount { get; set; }
        public int TigerLeadOnlyCount { get; set; }
    }
}
