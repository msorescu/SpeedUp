using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using Core.Log;

namespace SpeedUp.Model
{
    public class PsaManager
    {
        private const string SearchRequest = "<TCService ClientName=\"ORCA\"><Function>GetMLSPlusStandardListings</Function><Login>{0}</Login><Board BoardId=\"{1}\" ClassName=\"{2}\"/><Search {3}=\"{4}\" BypassARAuthentication=\"{5}\" Count=\"0\" RecordsLimit=\"10\" SearchLayout=\"Advanced search\"></Search><StandardSearch></StandardSearch></TCService>";
        private const string SearchTMKCountRequest = "<TCService ClientName=\"ORCA\"><Function>GetMLSPlusStandardListings</Function><Login>{0}</Login><Board BoardId=\"{1}\" ClassName=\"{2}\"/><Search {3}=\"{4}\" BypassARAuthentication=\"{5}\" Count=\"1\" SearchLayout=\"Advanced search\"></Search><StandardSearch></StandardSearch></TCService>";

        private const string SearchRequestListingCount = "<TCService ClientName=\"ORCA\"><Function>GetDataAggListings</Function><Login>{0}</Login><Board ModuleId=\"{1}\" MetadataVersion=\"0\"/><Search BypassARAuthentication=\"1\" ST_PublicListingStatus=\"A\" Count=\"1\" DACount=\"0\" ST_LastMod=\"{2}\"/></TCService>";

        private const string LoginNodeText = "<password>{0}</password><password>{1}</password><UserAgent>{2}</UserAgent><RetsUAPwd>{3}</RetsUAPwd>";
        //private const string SearchRequest = "<TCService ClientName=\"ORCA\"><Function>GetMLSPlusStandardListings</Function><Login><password>{0}</password><password>{1}</password><UserAgent>{2}</UserAgent><RetsUAPwd>{3}</RetsUAPwd></Login><Board BoardId=\"{4}\" ClassName=\"{5}\"/><Search {6}=\"{7}\" {8}=\"{9}\" BypassARAuthentication=\"1\" Count=\"0\" RecordsLimit=\"10\" SearchLayout=\"Advanced search\"></Search><StandardSearch></StandardSearch></TCService>";
        private LoginInfo _loginTrace;

        public void CompareTcsRdcActivePendingStatus(PendingStatusCompareResult compareResult, StreamWriter streamWriterReport)
        {
            try
            {
                var helper = new PsaHelper();
                var connections = helper.GetDefConnections(compareResult.ModuleId);

                foreach (var connection in connections)
                {
                    _loginTrace = null;
                    if (connection.connection_name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!connection.connection_name.EndsWith("re.def", StringComparison.OrdinalIgnoreCase) &&
                        !connection.connection_name.EndsWith("al.def", StringComparison.OrdinalIgnoreCase) &&
                        !connection.connection_name.EndsWith("cp.def", StringComparison.OrdinalIgnoreCase)) continue;
                    var resultByPropertyClass = new PendingStatusCompareResult
                        {
                            ModuleId = compareResult.ModuleId,
                            ModuleName = compareResult.ModuleName,
                            PropertyClass = connection.connection_type,
                            ConnectionName = connection.connection_name,
                            DefFilePath = connection.definition_file,
                            RdcCode = compareResult.RdcCode,
                            BoardId = connection.board_id ?? 0
                        };
                    helper.AnalyzeDef(ref resultByPropertyClass);
                    if (resultByPropertyClass.IsNotAvailableForOrca || resultByPropertyClass.IsOpenHouseOnlyModule)
                        continue;
                    CheckRdcAccountAccess(ref resultByPropertyClass);
                    if (resultByPropertyClass.IsDataSourceRemoved)
                        continue;
                    
                    if(_loginTrace !=null)
                        GetListCount(ref resultByPropertyClass);
                    helper.WriteToReport(streamWriterReport, resultByPropertyClass);
                    break;
                }
            }
            catch (Exception ex)
            {
                Log.Error(typeof(PsaManager), "Module ID = " + compareResult.ModuleId + "\r\n" + ex.Message + ex.StackTrace);
            }
        }

        private void GetListCount(ref PendingStatusCompareResult result)
        {
            try
            {
                var request = string.Format(SearchRequestListingCount, GetLoginText(_loginTrace), result.ModuleId,
                                            DateTime.Now.AddYears(-10).ToString("MM/dd/yyyyTHH:mm:ss") + "-" +
                                            DateTime.Now.ToString("MM/dd/yyyyTHH:mm:ss"));
                result.QaListingCount = PsaHelper.GetListingCount(request, ConfigurationManager.AppSettings["TcsQaUrl"]);
                result.ProdutionListingCount = PsaHelper.GetListingCount(request, "");
                
                if (result.QaListingCount > 0 && result.ProdutionListingCount > 0)
                {
                    result.IncreaseInActiveCounts = ((int)(0.5f + 100f*(result.QaListingCount - result.ProdutionListingCount)/result.ProdutionListingCount)).ToString(CultureInfo.InvariantCulture) + "%";
                }
                if (!string.IsNullOrEmpty(result.CurrentRdcPendingStatus))
                {
                    result.RdcCurrentPendingListingCount = SearchByTmkCountRequest(result,
                                                                                   result.CurrentRdcPendingStatus,
                                                                                   _loginTrace);
                }
            }
            catch (Exception ex)
            { 
                Log.Error(typeof(PsaManager), "Failed to get list count for Module ID: " + result.ModuleId + ex.Message + ex.StackTrace);
            }
        }

        private void CheckRdcAccountAccess(ref PendingStatusCompareResult compareResult)
        {
            try
            {
                GetTraceLogin(ref compareResult);
                if (compareResult.IsDataSourceRemoved)
                    return;
                
                if (string.IsNullOrEmpty(compareResult.PendingGap))
                {
                    compareResult.TestResult = PsaConstants.NoGapExisted;
                    return;
                }

                if (_loginTrace != null)
                {
                    var helper = new PsaHelper();
                    foreach (
                        var item in
                            (compareResult.TcsPending??""+ ","+ compareResult.PendingGap + "," + compareResult.RdcPending).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Distinct())
                    {
                        var returnCode = SearchByTmkRequest(compareResult, item, _loginTrace);
                        if (returnCode == Enums.CheckAccountReturnCode.RequestFailed60310)
                        {
                            returnCode = SearchByTmkRequest(compareResult, item, _loginTrace);
                            if (returnCode == Enums.CheckAccountReturnCode.RequestFailed60310)
                                returnCode = SearchByTmkRequest(compareResult, item, _loginTrace);
                        }
                        ProcessSearchResult(returnCode,ref compareResult,item);
                    }
                    helper.AnlyzeAccountAccessResult(ref compareResult);
                    if (!string.IsNullOrEmpty(compareResult.StatusRdcAccountReturnNoRecord))
                    {
                        foreach (
                            var item in
                                (compareResult.StatusRdcAccountReturnNoRecord).Split(
                                     new[] {','}, StringSplitOptions.RemoveEmptyEntries).Distinct())
                        {
                            CheckTpAccountAccess(ref compareResult, item);
                        }
                        compareResult.StatusHasNoListing = PsaHelper.FindGap(
                            compareResult.TpHasAccessStatusInRemainGap, compareResult.StatusRdcAccountReturnNoRecord);
                    }
                    else
                    {
                        compareResult.IsTpAccountLoginSuccess = true;
                    }
                }
                else
                {
                    compareResult.TestResult = PsaConstants.ResultError + " -- Cannot retrieve RDC credential";
                }
            }
            catch (Exception ex)
            {
                Log.Error(typeof(PsaManager), ex.Message + ex.StackTrace);
            }
        }

        private void CheckTpAccountAccess(ref PendingStatusCompareResult compareResult, string status)
        {
            var resultClone = compareResult;
            using (var dbPro = new TCSEntitiesProduction())
            {
                var tcsRequestLog = dbPro.tcs_request_log.Where(x => x.board_id == resultClone.BoardId && (x.function_id == 6 || x.function_id == 3)).OrderByDescending(x => x.when_created).Take(5);
                if (!tcsRequestLog.Any())
                { 
                    compareResult.TpAccountCheckResult = "Error -- No account found in TCS";
                    return;
                }
                foreach (var item in tcsRequestLog)
                {
                    var request = item.request_xml;
                    if (!File.Exists(item.location_path))
                        continue;
                    var loginInfo = GetLoginInfoFromXml(File.ReadAllText(item.location_path));
                    if(loginInfo!=null && !string.IsNullOrEmpty(loginInfo.UserName))
                    {
                        var returnCode = SearchByTmkRequest(compareResult, status, loginInfo);
                        if (returnCode != Enums.CheckAccountReturnCode.RequestFailed60310)
                        {
                            if (returnCode == Enums.CheckAccountReturnCode.Success)
                            {
                                if (!string.IsNullOrEmpty(compareResult.TpHasAccessStatusInRemainGap))
                                    compareResult.TpHasAccessStatusInRemainGap += "," + status;
                                else
                                {
                                    compareResult.TpHasAccessStatusInRemainGap = status;
                                }
                            }
                            compareResult.IsTpAccountLoginSuccess = true;
                            break;
                        }
                    }
                }
            }
        }

        private static LoginInfo GetLoginInfoFromXml(string requestXml)
        {
            var loginInfo = new LoginInfo();
            var request = XElement.Parse(requestXml);
            var dataSet = request.Descendants("password");
            var userName = "";
            var password = "";
            var userAgent = string.Empty;
            var uaPassword = string.Empty;
            var userAgentElment = request.Descendants("UserAgent").FirstOrDefault();
            if (userAgentElment != null)
            {
                loginInfo.UserAgent = userAgentElment.Value;
            }
            var uaPasswordElement = request.Descendants("RetsUAPwd").FirstOrDefault();
            if (uaPasswordElement != null)
            {
                loginInfo.UaPassword = uaPasswordElement.Value;
            }
            var i = 0;
            foreach (var obj in dataSet)
            {
                if (i == 0)
                    loginInfo.UserName = obj.Value;
                else
                    loginInfo.Password = obj.Value;
                i++;
            }
            loginInfo.ByPassAuthentication = "0";
            return loginInfo;
        }

        private Enums.CheckAccountReturnCode SearchByTmkRequest(PendingStatusCompareResult compareResult, string status, LoginInfo loginInfo)
        {
            string statusName;
            string statusValue;
            string lastModidfyFieldName;
            string lastMoidfyFieldValue;
            var helper = new PsaHelper();
            PsaHelper.GetStatusInfo(compareResult, status, out statusName, out statusValue, out lastModidfyFieldName, out lastMoidfyFieldValue);

            if (!string.IsNullOrEmpty(statusValue))
            {
                var request = string.Format(SearchRequest, GetLoginText(loginInfo),
                                            compareResult.BoardId.ToString(CultureInfo.InvariantCulture),
                                            PsaHelper.EncodeXml(compareResult.PropertyClass), statusName, statusValue, loginInfo.ByPassAuthentication);
                    //, lastModidfyFieldName, DateTime.Now.AddYears(-10).ToString("MM/dd/yyyyTHH/mm/ss") + "-" + DateTime.Now.ToString("MM/dd/yyyyTHH/mm/ss"));
                return helper.CheckRdcAccount(request);
            }
           
            return Enums.CheckAccountReturnCode.NoRecordFound;
        }

        private int SearchByTmkCountRequest(PendingStatusCompareResult compareResult, string status, LoginInfo loginInfo)
        {
            try
            {
                string statusName;
                string statusValue;
                string lastModidfyFieldName;
                string lastMoidfyFieldValue;
                var helper = new PsaHelper();
                PsaHelper.GetStatusInfo(compareResult, status, out statusName, out statusValue, out lastModidfyFieldName,
                                        out lastMoidfyFieldValue);

                if (!string.IsNullOrEmpty(compareResult.CurrentRdcPendingStatus))
                {
                    var request = string.Format(SearchTMKCountRequest, GetLoginText(loginInfo),
                                                compareResult.BoardId.ToString(CultureInfo.InvariantCulture),
                                                PsaHelper.EncodeXml(compareResult.PropertyClass), statusName,
                                                statusValue, loginInfo.ByPassAuthentication);
                    return PsaHelper.GetListingCount(request, "");
                }
            }
            catch (Exception ex)
            {
                Log.Error(typeof(PsaManager), ex.Message);
            }

            return 0;
        }
        private void ProcessSearchResult(Enums.CheckAccountReturnCode code, ref PendingStatusCompareResult compareResult, string status)
        {
            switch (code)
            {
                case Enums.CheckAccountReturnCode.Success:
                    if (!string.IsNullOrEmpty(compareResult.StatusRdcAccountSuccess))
                        compareResult.StatusRdcAccountSuccess += "," + status;
                    else
                    {
                        compareResult.StatusRdcAccountSuccess = status;
                    }
                    break;
                case Enums.CheckAccountReturnCode.RequestFailed:
                    compareResult.TestResult = PsaConstants.ResultError;
                    return;
                case Enums.CheckAccountReturnCode.RequestFailed60310:
                    compareResult.TestResult = PsaConstants.ResultError + "Authentication Error";
                    return;
                case Enums.CheckAccountReturnCode.RequestFailed60320:
                    compareResult.TestResult = PsaConstants.ResultError + "Search Failed";
                    return;
                case Enums.CheckAccountReturnCode.NoRecordFound:
                    if (!string.IsNullOrEmpty(compareResult.StatusRdcAccountReturnNoRecord))
                        compareResult.StatusRdcAccountReturnNoRecord += "," + status;
                    else
                    {
                        compareResult.StatusRdcAccountReturnNoRecord = status;
                    }
                    break;
            }
        }

        private void GetTraceLogin(ref PendingStatusCompareResult compareResult)
        {
            using (var se = new SystemEntities())
            {
                try
                {
                    var loginTraceInfo =
                        se.spDA_RETSConnectionInfo_sel(compareResult.RdcCode, false).FirstOrDefault();
                    var dataSourceResult =
                        se.spDA_TCSDatasource_sel(compareResult.ModuleId.ToString(CultureInfo.InvariantCulture),
                                                  false).ToList();
                    if (dataSourceResult.Count == 0)
                    {
                        compareResult.IsDataSourceRemoved = true;
                        _loginTrace = null;
                        return;
                    }
                    foreach (var item in dataSourceResult)
                    {
                        if (!item.OriginalDataSourceID.Equals(compareResult.RdcCode)) continue;
                        compareResult.TraceName = item.datasourcename.Trim();
                        break;
                    }
                    if (string.IsNullOrEmpty(compareResult.TraceName) || compareResult.TraceName.EndsWith("Test", StringComparison.OrdinalIgnoreCase))
                    {
                        compareResult.IsDataSourceRemoved = true;
                        _loginTrace = null;
                    }

                    if (loginTraceInfo != null)
                    {
                        _loginTrace = new LoginInfo
                            {
                                UserName = loginTraceInfo.RETSUserName,
                                Password = loginTraceInfo.RETSPassword,
                                UserAgent = loginTraceInfo.RETSUserAgent,
                                UaPassword = loginTraceInfo.RETSUserAgentPassword,
                                ByPassAuthentication = "1"
                            };
                    }
                }
                catch (Exception ex)
                {
                    compareResult.IsDataSourceRemoved = true;
                    Log.Error(typeof(PsaManager), "Cannot retrieve log in info from Trace for Module:" + compareResult.ModuleId + ex.Message + ex.StackTrace);
                }
            }
        }

        private string GetLoginText(LoginInfo loginInfo)
        {
            var result = string.Format(LoginNodeText, PsaHelper.EncodeXml(loginInfo.UserName),
                                       PsaHelper.EncodeXml(loginInfo.Password),
                                       loginInfo.UserAgent, PsaHelper.EncodeXml(loginInfo.UaPassword));
            return result;
        }
    }
}
