using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpeedUp.Model
{
    public class LLGapCompareResults
    {
        public short ModuleId { get; set; }
        public short BoardId { get; set; }
        public string DefFilePath { get; set; }
        public string ModuleName { get; set; }
        public string Vendor { get; set; }
        public string PropertyClass { get; set; }
        public string TraceName { get; set; }
        public string RdcCode { get; set; }
        public string TcsPending { get; set; }
        public string RdcPending { get; set; }
        public string TcsActive { get; set; }
        public string PendingGap { get; set; }
        public string ActiveDifNotInTcs { get; set; }
        public string ActiveDifNotInRdc { get; set; }
        public string SoldDifNotInTcs { get; set; }
        public string SoldDifNotInRdc { get; set; }
        public string OffMarketDifNotInTcs { get; set; }
        public string OffMarketDifNotInRdc { get; set; }
        public string RdcActive { get; set; }
        public string RdcPendingGap { get; set; }
        public string TcsPendingGap { get; set; }
        public string StatusRdcAccountReturnNoRecord { get; set; }
        public string StatusRdcAccountRequestFailed { get; set; }
        public string StatusRdcAccountSuccess { get; set; }
        public string TestResult { get; set; }
        public string RemainingGap { get; set; }
        public string ActivePercentageChange { get; set; }
        public string ActiveCountBeforChange { get; set; }
        public string ActiveCountAfterChange { get; set; }
        public string TcsSold { get; set; }
        public string RdcSold { get; set; }
        public string TcsOffMarket { get; set; }
        public string RdcOffMarket { get; set; }
        public bool IsOpenHouseOnlyModule { get; set; }
        public bool IsNotAvailableForOrca { get; set; }
        public bool IsDataSourceRemoved { get; set; }
        public string ConnectionName { get; set; }
        public int QaListingCount { get; set; }
        public int ProdutionListingCount { get; set; }
        public string TpHasAccessStatusInRemainGap { get; set; }
        public string StatusHasNoListing { get; set; }
        public string IncreaseInActiveCounts { get; set; }
        public bool IsTpAccountLoginSuccess { get; set; }
        public string TpAccountCheckResult { get; set; }
        public int RdcCurrentPendingListingCount { get; set; }
        public string CurrentRdcPendingStatus { get; set; }
        public int TotalClasses { get; set; }
        public int TotalLatLongMapped { get; set; }
        public string LatLongClassesMapped { get; set; }
        public string LatLongClassesNotMapped { get; set; }
        public int OfMappedClassesLatLongHasData { get; set; }
        public string LatLongDataExists { get; set; }
        public string CandidateDEFs { get; set; }
        public string NonCandidateDEFs { get; set; }
        public string RawDataInClasses { get; set; }
        public bool IsOffice { get; set; }
        public bool IsAgent { get; set; }
        public string URLMappingOpp { get; set; }
        public string STDFListingDetailsPageURLStatus { get; set; }
        public string STDFListingOfficeURLStatus { get; set; }
        public string STDFListingBrokerageURLStatus { get; set; }
        public string STDFListingFranchiseURLStatus { get; set; }
        public string AG_OfficeURLStatus { get; set; }
        public string AG_BrokerageURLStatus { get; set; }
        public string AG_FranchiseURLStatus { get; set; }
        public string OF_WebsiteURLStatus { get; set; }
        public string OF_BrokerageURLStatus { get; set; }
        public string OF_FranchiseURLStatus { get; set; }
        public int STDFListingDetailsPageURLCount { get; set; }
        public int STDFListingOfficeURLCount { get; set; }
        public int STDFListingBrokerageURLCount { get; set; }
        public int STDFListingFranchiseURLCount { get; set; }
        public int AG_OfficeURLCount { get; set; }
        public int AG_BrokerageURLCount { get; set; }
        public int AG_FranchiseURLCount { get; set; }
        public int OF_WebsiteURLCount { get; set; }
        public int OF_BrokerageURLCount { get; set; }
        public int OF_FranchiseURLCount { get; set; }
        public int ListingCount { get; set; }
        public int OfficeCount { get; set; }
        public int AgentCount { get; set; }
    }
}
