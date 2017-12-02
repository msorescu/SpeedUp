using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Tcs.SearchEngineService;

namespace SpeedUp.Model
{
    public class LLGapHelper
    {
        protected const int MatchNone = 0;
        protected const int MatchAll = 1;
        protected const int MatchFirst = 2;
        protected const int MatchSecond = 3;

        public List<cma_mls_board_connections> GetDefConnections(short moduleId)
        {
            List<cma_mls_board_connections> connctions = null;
            using (var tcsDb = new TCSEntitiesProduction())
            {
                var board = tcsDb.cma_mls_boards.FirstOrDefault(x => x.module_id == moduleId && x.board_status_id == 1);
                if (board != null)
                {
                    var boardId = board.board_id;
                    connctions = tcsDb.cma_mls_board_connections.Where(x => x.board_id == boardId).ToList();
                }
            }
            return connctions;
        }

        public void AnalyzeDef(ref LLGapCompareResults result)
        {
            var iniReader = new IniFile(result.DefFilePath);
            //DEFNotAvailableTo=ORCA
            var defNotAvailable = iniReader.Read("DEFNotAvailableTo", "Common") ?? "";
            if (defNotAvailable.Equals("ORCA", StringComparison.OrdinalIgnoreCase))
            {
                result.IsNotAvailableForOrca = true;
                return;
            }
            else
            {
                result.IsNotAvailableForOrca = false;
            }
            var tcpIp = iniReader.Read("TcpIp", "Common");
            if (tcpIp.Equals("10"))
            {
                result.IsOpenHouseOnlyModule = true;
                return;
            }

            var test = iniReader.GetSectionNames();
            var test2 = iniReader.GetEntryNames("MLSRecordsEx");
            //result.LatLongClassesMapped = FindLatLongDEF(iniReader.GetSectionContent("MLSRecordsEX"));

            if (FindLatLongDEF(iniReader.GetSectionContent("MLSRecordsEX")))
            {
                if (result.TotalLatLongMapped == 0)
                {
                    result.LatLongClassesMapped = result.PropertyClass;
                }
                else
                {
                    result.LatLongClassesMapped = result.LatLongClassesMapped + "," + result.PropertyClass;
                }


                result.TotalLatLongMapped++;
            }
            else
            {
                if (result.TotalLatLongMapped == result.TotalClasses)
                {
                    result.LatLongClassesNotMapped = result.PropertyClass;
                }
                else
                {
                    result.LatLongClassesNotMapped = result.LatLongClassesNotMapped + "," + result.PropertyClass;
                }
                
            }

            FindURLFieldsinDEF(iniReader.GetSectionContent("MLSRecordsEX"),ref result);

            result.TotalClasses++;

            result.IsOpenHouseOnlyModule = false;
            result.TcsActive = iniReader.Read("Active", "Sorting") ?? "";
            result.TcsPending = iniReader.Read("Pending", "Sorting") ?? "";
            result.RdcActive = iniReader.Read("Active", "SortingPublicListingStatus") ?? "";
            result.RdcSold = iniReader.Read("Sold", "SortingPublicListingStatus") ?? "";
            result.TcsSold = iniReader.Read("Sold", "Sorting") ?? "";
            result.RdcOffMarket = iniReader.Read("OffMarketOrOther", "SortingPublicListingStatus") ?? "";
            result.TcsOffMarket = iniReader.Read("Expired", "Sorting") ?? "";
            if (!string.IsNullOrEmpty(result.RdcActive))
            {
                result.TcsPendingGap = FindGap(result.TcsActive + "," + result.TcsPending, result.RdcActive);
                result.RdcPendingGap = FindGap(result.RdcActive, result.TcsActive + "," + result.TcsPending);
                result.RdcPending = FindCommon(result.TcsPending, result.RdcActive);
                result.PendingGap = FindGap(result.RdcPending, result.TcsPending);
                result.ActiveDifNotInRdc = FindGap(result.RdcActive, result.TcsActive);
                result.ActiveDifNotInTcs = FindGap(result.TcsActive + "," + result.TcsPending, result.RdcActive);
                result.CurrentRdcPendingStatus = FindCommon(result.TcsPending, result.RdcActive);
            }
            else
            {
                result.RdcActive = result.TcsActive;
                result.TcsPendingGap = "";
                result.RdcPendingGap = "";
                result.PendingGap = result.TcsPending;
                result.RdcPending = "";
                result.ActiveDifNotInRdc = "";
            }
            if (!string.IsNullOrEmpty(result.RdcSold))
            {
                result.SoldDifNotInTcs = FindGap(result.TcsSold, result.RdcSold);
                result.SoldDifNotInRdc = FindGap(result.RdcSold, result.TcsSold);
            }
            else
            {
                result.SoldDifNotInRdc = "";
                result.SoldDifNotInTcs = "";
            }
            if (!string.IsNullOrEmpty(result.RdcOffMarket))
            {
                result.OffMarketDifNotInTcs = FindGap(result.TcsOffMarket + "," + result.TcsPending, result.RdcOffMarket);
                result.OffMarketDifNotInRdc = FindGap(result.RdcOffMarket, result.TcsOffMarket);
            }
            else
            {
                result.OffMarketDifNotInRdc = "";
                result.OffMarketDifNotInTcs = "";
            }
        }

        public static bool FindLatLongDEF(string MLSRecordsEx)
        {
            string[] stringSeparators = new string[] { "\r\n" };
            var result = MLSRecordsEx.Split(stringSeparators, StringSplitOptions.None);
            bool latPresent = false;
            bool longPresent = false;

            foreach (string line in result)
            {
                if ((!line.StartsWith(";"))&&line.ToLower().Contains("fldname=stdflong"))
                {
                    longPresent = true;
                }
                if ((!line.StartsWith(";")) && line.ToLower().Contains("fldname=stdflat"))
                {
                    latPresent = true;
                }
            }

            if (latPresent&&longPresent)
            {
                return true;
            }


            return false;

        }

        public void FindURLFieldsinDEF(string MLSRecordsEx, ref LLGapCompareResults result)
        {
            string[] stringSeparators = new string[] { "\r\n" };
            var RecordMapping = MLSRecordsEx.Split(stringSeparators, StringSplitOptions.None);
            var LDPMapped = false;
            var OfficeURLMapped = false;
            var BrokerURLMapped = false;
            var FranchiseURLMapped = false;
            var ListingSys = new List<string>
                                     {
                                            "stdflistingdetailspageurl", "stdflistingofficeurl", "stdflistingbrokerageurl", 
                                            "stdflistingfranchiseurl", "ag_officeurl", "ag_brokerageurl", "ag_franchiseurl", 
                                            "of_websiteurl", "of_brokerageurl", "of_franchiseurl"
                                     };

            foreach (string line in RecordMapping)
            {
                if(result.IsAgent)
                {
                    if ((!line.StartsWith(";")) && line.ToLower().Contains("fldname=ag_officeurl"))
                    {
                        result.AG_OfficeURLStatus = "Mapped";
                    }
                    if ((!line.StartsWith(";")) && line.ToLower().Contains("fldname=ag_brokerageurl"))
                    {
                        result.AG_BrokerageURLStatus = "Mapped";
                    }
                    if ((!line.StartsWith(";")) && line.ToLower().Contains("fldname=ag_franchiseurl"))
                    {
                        result.AG_FranchiseURLStatus = "Mapped";
                    }
                }
                else if(result.IsOffice)
                {

                    if ((!line.StartsWith(";")) && line.ToLower().Contains("fldname=of_websiteurl"))
                    {
                        result.OF_WebsiteURLStatus = "Mapped";
                    }
                    if ((!line.StartsWith(";")) && line.ToLower().Contains("fldname=of_brokerageurl"))
                    {
                        result.OF_BrokerageURLStatus = "Mapped";
                    }
                    if ((!line.StartsWith(";")) && line.ToLower().Contains("fldname=of_franchiseurl"))
                    {
                        result.OF_FranchiseURLStatus = "Mapped";
                    }
                }
                else
                {
                    if ((!line.StartsWith(";")) && line.ToLower().Contains("fldname=stdflistingdetailspageurl"))
                    {
                        LDPMapped = true;
                    }
                    if ((!line.StartsWith(";")) && line.ToLower().Contains("fldname=stdflistingofficeurl"))
                    {
                        OfficeURLMapped = true;
                    }
                    if ((!line.StartsWith(";")) && line.ToLower().Contains("fldname=stdflistingbrokerageurl"))
                    {
                        BrokerURLMapped = true;
                    }
                    if ((!line.StartsWith(";")) && line.ToLower().Contains("fldname=stdflistingfranchiseurl"))
                    {
                        FranchiseURLMapped = true;
                    }
                }

            }

            if (LDPMapped)
            {
                if (result.STDFListingDetailsPageURLStatus == "Partial-Mapped")
                {
                }
                else
                {
                    result.STDFListingDetailsPageURLStatus = "Mapped";
                }
            }
            else
            {
                if (result.STDFListingDetailsPageURLStatus == "Mapped")
                {
                    result.STDFListingDetailsPageURLStatus = "Partial-Mapped";
                }
            }


            if (OfficeURLMapped)
            {
                if (result.STDFListingOfficeURLStatus == "Partial-Mapped")
                {
                }
                else
                {
                    result.STDFListingOfficeURLStatus = "Mapped";
                }
            }
            else
            {
                if (result.STDFListingOfficeURLStatus == "Mapped")
                {
                    result.STDFListingOfficeURLStatus = "Partial-Mapped";
                }
            }

            if (BrokerURLMapped)
            {
                if (result.STDFListingBrokerageURLStatus == "Partial-Mapped")
                {
                }
                else
                {
                    result.STDFListingBrokerageURLStatus = "Mapped";
                }
            }
            else
            {
                if (result.STDFListingBrokerageURLStatus == "Mapped")
                {
                    result.STDFListingBrokerageURLStatus = "Partial-Mapped";
                }
            }

            if (FranchiseURLMapped)
            {
                if (result.STDFListingFranchiseURLStatus == "Partial-Mapped")
                {
                }
                else
                {
                    result.STDFListingFranchiseURLStatus = "Mapped";
                }
            }
            else
            {
                if (result.STDFListingFranchiseURLStatus == "Mapped")
                {
                    result.STDFListingFranchiseURLStatus = "Partial-Mapped";
                }
            }

        }

        public static string FindGap(string strSet, string strToCheck)
        {
            if (strSet == null)
                strSet = "";
            if (strToCheck == null)
                strToCheck = "";

            var result = string.Empty;
            var hs = new HashSet<string>();
            foreach (var item in strSet.ToLower().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                hs.Add(item);
            }

            result = strToCheck.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Where(str => !hs.Contains(str.ToLower())).Aggregate(result, (current, str) => current + (str + ","));
            if (result.EndsWith(","))
                result = result.Substring(0, result.Length - 1);
            return result;
        }

        private static string FindCommon(string strSet, string strToCheck)
        {
            if (strSet == null)
                strSet = "";
            if (strToCheck == null)
                strToCheck = "";
            var result = string.Empty;
            var hs = new HashSet<string>();
            foreach (var item in strSet.ToLower().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                hs.Add(item);
            }

            result = strToCheck.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Where(str => hs.Contains(str.ToLower())).Aggregate(result, (current, str) => current + (str + ","));
            if (result.EndsWith(","))
                result = result.Substring(0, result.Length - 1);
            return result;
        }

        public void WriteToLine(StreamWriter sw, LLGapCompareResults result)
        {
            var sb = new StringBuilder();
            sb.Append(result.ModuleId).Append(",");
            sb.Append("\"").Append(result.ModuleName).Append("\",");
            sb.Append(result.RdcCode).Append(",");
            sb.Append(result.TraceName).Append(",");
            if (result.IsTpAccountLoginSuccess || string.IsNullOrEmpty(result.StatusRdcAccountReturnNoRecord))
                sb.Append("\"").Append(result.StatusHasNoListing ?? "").Append("\",");
            else if (!string.IsNullOrEmpty(result.TpAccountCheckResult))
                sb.Append(result.TpAccountCheckResult).Append(",");
            else
                sb.Append("Error - Authentication failed").Append(",");
            sb.Append("\"").Append(result.URLMappingOpp).Append("\",");
            sb.Append("\"").Append(result.STDFListingDetailsPageURLStatus).Append("\",");
            sb.Append("\"").Append(result.STDFListingDetailsPageURLCount).Append("\",");
            sb.Append("\"").Append(result.STDFListingOfficeURLStatus).Append("\",");
            sb.Append("\"").Append(result.STDFListingOfficeURLCount).Append("\",");
            sb.Append("\"").Append(result.STDFListingBrokerageURLStatus).Append("\",");
            sb.Append("\"").Append(result.STDFListingBrokerageURLCount).Append("\",");
            sb.Append("\"").Append(result.STDFListingFranchiseURLStatus).Append("\",");
            sb.Append("\"").Append(result.STDFListingFranchiseURLCount).Append("\",");
            sb.Append("\"").Append(result.ListingCount).Append("\",");
            sb.Append("\"").Append(result.AG_OfficeURLStatus).Append("\",");
            sb.Append("\"").Append(result.AG_OfficeURLCount).Append("\",");
            sb.Append("\"").Append(result.AG_BrokerageURLStatus).Append("\",");
            sb.Append("\"").Append(result.AG_BrokerageURLCount).Append("\",");
            sb.Append("\"").Append(result.AG_FranchiseURLStatus).Append("\",");
            sb.Append("\"").Append(result.AG_FranchiseURLCount).Append("\",");
            sb.Append("\"").Append(result.AgentCount).Append("\",");
            sb.Append("\"").Append(result.OF_WebsiteURLStatus).Append("\",");
            sb.Append("\"").Append(result.OF_WebsiteURLCount).Append("\",");
            sb.Append("\"").Append(result.OF_BrokerageURLStatus).Append("\",");
            sb.Append("\"").Append(result.OF_BrokerageURLCount).Append("\",");
            sb.Append("\"").Append(result.OF_FranchiseURLStatus).Append("\",");
            sb.Append("\"").Append(result.OF_FranchiseURLCount).Append("\",");
            sb.Append("\"").Append(result.OfficeCount).Append("\",");
            //sb.Append(result.PropertyClass).Append(",");
            //sb.Append("\"").Append(result.TcsPending).Append("\",");
            //sb.Append("\"").Append(result.RdcPending).Append("\",");
            //sb.Append("\"").Append(result.PendingGap).Append("\",");
            //sb.Append("\"").Append(result.TcsActive).Append("\",");
            //sb.Append("\"").Append(result.RdcActive).Append("\",");
            //sb.Append("\"").Append(result.ActiveDifNotInTcs).Append("\",");
            //sb.Append("\"").Append(result.ActiveDifNotInRdc).Append("\",");
            //sb.Append("\"").Append(result.TcsSold).Append("\",");
            //sb.Append("\"").Append(result.RdcSold).Append("\",");
            //sb.Append("\"").Append(result.SoldDifNotInTcs).Append("\",");
            //sb.Append("\"").Append(result.SoldDifNotInRdc).Append("\",");
            //sb.Append("\"").Append(result.TcsOffMarket).Append("\",");
            //sb.Append("\"").Append(result.RdcOffMarket).Append("\",");
            //sb.Append("\"").Append(result.OffMarketDifNotInTcs).Append("\",");
            //sb.Append("\"").Append(result.OffMarketDifNotInRdc).Append("\",");
            //sb.Append("\"").Append(result.TestResult).Append("\",");
            //sb.Append("\"").Append(result.StatusRdcAccountSuccess).Append("\",");
            //sb.Append("\"").Append(result.StatusRdcAccountRequestFailed).Append("\",");
            //sb.Append("\"").Append(result.StatusRdcAccountReturnNoRecord ?? "").Append("\",");
            //sb.Append(result.ProdutionListingCount).Append(",");
            //sb.Append(result.QaListingCount).Append(",");
            // sb.Append(result.IncreaseInActiveCounts ?? "").Append(",");
            //sb.Append(result.RdcCurrentPendingListingCount == 0 ? "" : result.RdcCurrentPendingListingCount.ToString(CultureInfo.InvariantCulture)).Append(",");
            //sb.Append("\"").Append(result.TotalClasses).Append("\",");
            //sb.Append("\"").Append(result.TotalLatLongMapped).Append("\",");
            //sb.Append("\"").Append(result.LatLongClassesMapped).Append("\",");
            //sb.Append("\"").Append(result.LatLongClassesNotMapped).Append("\",");
            //sb.Append("\"").Append(result.LatLongDataExists).Append("\",");
            //sb.Append("\"").Append(result.CandidateDEFs).Append("\",");
            //sb.Append("\"").Append(result.NonCandidateDEFs).Append("\",");
            //sb.Append("\"").Append(result.RawDataInClasses).Append("\",");
            sw.WriteLine(sb.ToString());
        }

        public void WriteToReportEnd(StreamWriter sw, StringBuilder[] result)
        {
            
        }

        public static void GetStatusInfo(LLGapCompareResults compareResult, string status, out string statusName, out string statusValue, out string lastModifyDateFieldName, out string lastModifyDateFiledValue)
        {
            statusValue = "";
            var iniReader = new IniFile(compareResult.DefFilePath);
            lastModifyDateFieldName = iniReader.Read("ST_LastMod", "Standard_Search") ?? "";
            var dateFormat = iniReader.Read("SDateFormat", "field_" + lastModifyDateFieldName) ?? "";
            if (string.IsNullOrEmpty(dateFormat))
                dateFormat = "MM/dd/yyyyTHH:mm:ss";
            lastModifyDateFiledValue = DateTime.Now.AddYears(-10).ToString(dateFormat) + "-" +
                                       DateTime.Now.ToString(dateFormat);
            statusName = iniReader.Read("ST_Status", "Standard_Search") ?? "";
            var statusFieldName = "field_" + statusName;
            var statusFormat = iniReader.Read("Format", statusFieldName) ?? "";
            var sortingFormat = iniReader.Read("Format", "Sorting");
            var formatFlag = GetFormatFlag(statusFormat, sortingFormat);
            var delimiter = GetFormatDelimiter(statusFormat);
            var initialValues = new List<string>();
            for (var i = 1; i < 100; i++)
            {
                var val = iniReader.Read("Value" + i, statusFieldName);
                if (!string.IsNullOrEmpty(val))
                    initialValues.Add(val);
                else
                    break;
            }
            if (initialValues.Count > 0)
            {
                var statusList = status.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var item in statusList)
                {
                    statusValue += GetStStatus(item, delimiter, initialValues, formatFlag) + ",";
                }
                if (statusValue.EndsWith(","))
                    statusValue = statusValue.Substring(0, statusValue.Length - 1);
            }
            else
            {
                statusValue = "";
            }
        }

        public static string GetStStatus(string status, string delimiter, List<string> initialValues, int formatFlag)
        {
            var result = "";
            try
            {
                foreach (var item in initialValues)
                {
                    if (!CompareString(item, status, delimiter, formatFlag)) continue;
                    result = item;
                    break;
                }
            }
            catch (Exception ex)
            {
            }
            return result;
        }

        private static int GetFormatFlag(string statusFormat, string sortingFormat)
        {
            statusFormat = RemoveSpace(statusFormat);
            sortingFormat = RemoveSpace(sortingFormat);
            int pos = 0;
            pos = statusFormat.ToUpper().IndexOf(sortingFormat.ToUpper(), System.StringComparison.Ordinal);

            if (statusFormat.Length == 0 || sortingFormat.Length == 0 || pos == -1)
                return MatchNone;

            if (statusFormat.ToUpper().Equals(sortingFormat.ToUpper()))
                return MatchAll;

            if (pos == 0)
                return MatchFirst;
            else
                return MatchSecond;
        }

        private static string RemoveSpace(string str)
        {
            str = !string.IsNullOrEmpty(str) ? str.Replace(" ", "") : "";
            return str;
        }

        private static string GetFormatDelimiter(string statusFormat)
        {
            string delimiter = "";
            if (statusFormat.Length > 2)
                delimiter = statusFormat.Substring(1, (statusFormat.Length - 1) - (1));

            return delimiter;
        }

        private static bool CompareString(string s1, string s2, string delimiter, int formatFlag)
        {
            try
            {
                s1 = s1.Trim(); //removeSpace( s1 );
                s2 = s2.Trim(); //removeSpace( s2 );
                bool result = false;

                int pos = s1.IndexOf(delimiter, System.StringComparison.Ordinal);

                switch (formatFlag)
                {

                    case MatchAll:
                        result = s1.ToUpper().Equals(s2.ToUpper());
                        break;

                    case MatchFirst:
                        s1 = s1.Substring(0, (pos) - (0));
                        result = s1.ToUpper().Equals(s2.ToUpper()) ? true : false;
                        break;

                    case MatchSecond:
                        s1 = s1.Substring(pos + delimiter.Length);
                        result = s1.ToUpper().Equals(s2.ToUpper()) ? true : false;
                        break;

                    case MatchNone:
                        break;
                }
                return result;
            }
            catch (System.Exception e)
            {
                return false;
            }
        }

        public Enums.CheckAccountReturnCode CheckRdcAccount(string request)
        {
            var result = SearchMls(request);
            if (result.IndexOf("ReplyText=\"Success\"", StringComparison.CurrentCultureIgnoreCase) >
                        -1)
            {
                var listingCount = 0;
                var xelement = XElement.Parse(result);
                listingCount = xelement.Descendants("Listing").Count();
                return listingCount > 0 ? Enums.CheckAccountReturnCode.Success : Enums.CheckAccountReturnCode.NoRecordFound;
            }

            if (result.IndexOf("ReplyCode=\"60310\"", StringComparison.OrdinalIgnoreCase) > -1)
                return Enums.CheckAccountReturnCode.RequestFailed60310;
            if (result.IndexOf("ReplyCode=\"60320\"", StringComparison.OrdinalIgnoreCase) > -1)
                return Enums.CheckAccountReturnCode.RequestFailed60320;

            return Enums.CheckAccountReturnCode.RequestFailed;
        }

        public Enums.CheckAccountReturnCode CheckRdcAccountLatLongValues(string request)
        {
            var result = SearchMls(request);
            if (result.IndexOf("ReplyText=\"Success\"", StringComparison.CurrentCultureIgnoreCase) >
                        -1)
            {
                var listingCount = 0;
                bool longi = false;
                bool lati = false;
                var xelement = XElement.Parse(result);
                var listings = xelement.Descendants("Listing");
                listingCount = listings.Count();
                foreach (var node in listings)
                {
                    if ((string)node.Attribute("Longitude") != "")
                    {
                        longi = true;
                    }

                    if ((string)node.Attribute("Latitude") != "")
                    {
                        lati = true;
                    }

                    if(longi&&lati)
                    {
                        break;
                    }
                }
                return (longi&&lati) ? Enums.CheckAccountReturnCode.Success : Enums.CheckAccountReturnCode.NoRecordFound;
            }

            if (result.IndexOf("ReplyCode=\"60310\"", StringComparison.OrdinalIgnoreCase) > -1)
                return Enums.CheckAccountReturnCode.RequestFailed60310;
            if (result.IndexOf("ReplyCode=\"60320\"", StringComparison.OrdinalIgnoreCase) > -1)
                return Enums.CheckAccountReturnCode.RequestFailed60320;

            return Enums.CheckAccountReturnCode.RequestFailed;
        }

        public static int GetListingCount(string request, string searchUrl)
        {
            var result = SearchMls(request, searchUrl);
            var listingCount = 0;
            if (result.IndexOf("ReplyText=\"Success\"", StringComparison.CurrentCultureIgnoreCase) >
                -1)
            {
                var xelement = XElement.Parse(result);
                var listingsNode = xelement.Descendants("Listings").SingleOrDefault();
                if (listingsNode != null)
                    int.TryParse(listingsNode.Attribute("TotalCount").Value, out listingCount);
            }
            return listingCount;
        }

        public static string SearchMls(string request, string searchUrl = "")
        {


            if (string.IsNullOrEmpty(searchUrl))
                searchUrl = ConfigurationManager.AppSettings["TcsQaUrl"];
            var searchHandler = new SearchHandler(searchUrl);
            return searchHandler.SearchMls(request);
        }

        public void AnlyzeAccountAccessResult(ref LLGapCompareResults result)
        {
            if (!string.IsNullOrEmpty(result.TestResult))
                return;

            if (!string.IsNullOrEmpty(result.StatusRdcAccountRequestFailed))
            {
                result.TestResult = PsaConstants.ResultError;
                return;
            }

            if (!string.IsNullOrEmpty(result.StatusRdcAccountSuccess))
            {
                if (string.IsNullOrEmpty(FindGap(result.StatusRdcAccountSuccess, result.TcsPending)))
                {
                    result.TestResult = PsaConstants.AllPendingPresent;
                }
                else
                {
                    result.TestResult = PsaConstants.PartialPending;
                }
                //else if (!string.IsNullOrEmpty(FindCommon(result.StatusRdcAccountSuccess, result.PendingGap)))
                //    result.TestResult = PsaConstants.PartialPendingImprovement;
                //else if (string.IsNullOrEmpty(FindCommon(result.StatusRdcAccountSuccess, result.PendingGap)) && string.IsNullOrEmpty(FindGap(result.StatusRdcAccountSuccess,result.RdcPending)))
                //    result.TestResult = PsaConstants.PartialPendingNoImprovement;
                //else if (!string.IsNullOrEmpty(FindGap(result.StatusRdcAccountSuccess, result.RdcPending)))
                //    result.TestResult = PsaConstants.PartialPendingWorse;
                //else if (string.IsNullOrEmpty(FindCommon(result.StatusRdcAccountSuccess, result.TcsPending)))
                //    result.TestResult = PsaConstants.NoPending;
            }
            else
            {
                result.TestResult = PsaConstants.NoPending;
            }

        }

        public static string EncodeXml(string strToEncode)
        {
            if (string.IsNullOrEmpty(strToEncode))
                return "";
            var buffer = new StringBuilder();

            int count = strToEncode.Length;
            for (int i = 0; i < count; i++)
            {
                char c = strToEncode[i];
                switch (c)
                {

                    case '&':
                        buffer.Append("&amp;");
                        break;

                    case '<':
                        buffer.Append("&lt;");
                        break;

                    case '>':
                        buffer.Append("&gt;");
                        break;

                    case '\'':
                        buffer.Append("&apos;");
                        break;

                    case '"':
                        buffer.Append("&quot;");
                        break;

                    default:
                        buffer.Append(c);
                        break;

                }
            }
            return buffer.ToString();
        }

        public string GetLastModifyDateFieldName()
        {
            throw new NotImplementedException();
        }
    }
}
