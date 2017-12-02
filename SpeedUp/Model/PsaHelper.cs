using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Tcs.SearchEngineService;
using System.Data.Objects;

using MySql.ServiceInterface;
using MySql.ServiceModel;
using MySql.ServiceModel.Types;
using ServiceStack.Testing;
using ServiceStack;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using MySql;

namespace SpeedUp.Model
{
    public class PsaHelper
    {
        protected const int MatchNone = 0;
        protected const int MatchAll = 1;
        protected const int MatchFirst = 2;
        protected const int MatchSecond = 3;

        public List<string> GetValidModuleIDs()
        {
            ServiceStackHost appHost;

            appHost = new AppHost();
            appHost.Init().Start("http://*:2337/");

            var service = appHost.Container.Resolve<VendorService>();

            var response = service.Get(new GetVendors());
            List<string> moduleList = new List<string>();
            foreach (var vendorItem in response.Results)
                if (!string.IsNullOrWhiteSpace(vendorItem.moduleid))
                    moduleList.Add(vendorItem.moduleid);

            return moduleList;
        }

        public List<cma_mls_board_connections> GetDefConnections(short moduleId)
        {
            List<cma_mls_board_connections> connctions = null;
            using (var tcsDb = new TCSEntitiesProduction())
            {
                var board = tcsDb.cma_mls_boards.FirstOrDefault(x => x.module_id == moduleId && x.board_status_id==1);
                if (board != null)
                {
                    var boardId = board.board_id;
                    connctions = tcsDb.cma_mls_board_connections.Where(x => x.board_id == boardId).ToList();
                }
            }
            return connctions;
        }


        public Dictionary<string, string> GetDataListSoldToActiveTranslation()
        {
            Dictionary<string, string> dictDataSourcesTranslated = new Dictionary<string, string>();
            using (SystemEntities se = new SystemEntities())
            {
                try
                {
                    se.CommandTimeout = 0;
                    var dataSourceResult =
                        se.spDA_SoldToActiveDataSource_sel().ToList();
                    
                    foreach (var item in dataSourceResult)                    
                        dictDataSourcesTranslated.Add(item.sold_data_source_id.Trim(), item.main_data_source_id.Trim());

                }
                catch (Exception ex)
                {
                }
            }
            return dictDataSourcesTranslated;
        }

        public LoginInfo GetDataAggTraceInfo(string dataSourceId, ref string retsLoginURL)
        {
            //RdcCode is data source id. 

            LoginInfo loginInfo = null;
            using (SystemEntities se = new SystemEntities())
            {
                try
                {
            
                    var loginTraceInfo = se.spDA_RETSConnectionInfo_sel(dataSourceId.ToString(CultureInfo.InvariantCulture),
                        false).FirstOrDefault();
                    //var dataSourceResult =
                    //    se.spDA_TCSDatasource_sel(moduleId.ToString(CultureInfo.InvariantCulture), false).ToList();

                    //if (dataSourceResult.Count == 0)
                    //{
                    //    return null;
                    //}
                    //string traceName = string.Empty;
                    //foreach (var item in dataSourceResult)
                    //{
                    //    if (!item.OriginalDataSourceID.Equals(dataSourceId)) continue;
                    //    traceName = item.datasourcename.Trim();
                    //    break;
                    //}
                    //if (string.IsNullOrEmpty(traceName) || traceName.EndsWith("Test", StringComparison.OrdinalIgnoreCase))
                    //{
                    //    return null;
                    //}

                    if (loginTraceInfo != null)
                    {
                        loginInfo = new LoginInfo
                        {
                            UserName = loginTraceInfo.RETSUserName,
                            Password = loginTraceInfo.RETSPassword,
                            UserAgent = loginTraceInfo.RETSUserAgent,
                            UaPassword = loginTraceInfo.RETSUserAgentPassword,
                            ByPassAuthentication = "1"
                        };                        
                        retsLoginURL = loginTraceInfo.RETSLoginURL; 
                    }

                    //se.Connection.Open();
                    //ObjectResult<spDA_RETSConnectionInfo_sel_Result> loginTrace =
                    //    se.spDA_TCSDatasource_sel(dataSourceId, false);
                    //foreach (var connInfo in loginTrace)
                    //{
                    //    ModuleLoginContainer connection = new ModuleLoginContainer();                                        
                    //    connection.LoginURL = connInfo.RETSLoginURL;
                    //    connection.LoginUserName = connInfo.RETSUserName;
                    //    connection.LoginPassword = connInfo.RETSPassword;
                    //    connection.UserAgent = connInfo.RETSUserAgent;
                    //    connection.UserAgentPW = connInfo.RETSUserAgentPassword;
                    //    connections.Add(connection);
                    //}
                }
                catch (Exception ex)
                {
                }
            
            }
            return loginInfo;
        }

        public LoginInfo GetDataAggTraceInfoExtra(string dataSourceId, int moduleId, ref string retsLoginURL, string protocol, string aggregationType="REALListing")
        {
            //RdcCode is data source id. 

            LoginInfo loginInfo = null;
            using (SystemEntities se = new SystemEntities())
            {
                try
                {

                    var loginTraceInfo = se.spDA_RETSConnectionInfo_sel(dataSourceId.ToString(CultureInfo.InvariantCulture),
                        false).FirstOrDefault();
                    
                    if (loginTraceInfo != null)
                    {
                        var aggregationSubType = "Full";
                        var paramValue = this.GetConfigParameterValueFromDataSourceInfo(moduleId.ToString(CultureInfo.InvariantCulture), dataSourceId.ToString(CultureInfo.InvariantCulture), protocol, "TCSUserID", aggregationSubType, aggregationType);
                        if (string.IsNullOrWhiteSpace(paramValue))
                        { 
                            aggregationSubType = "Incremental";
                            paramValue = this.GetConfigParameterValueFromDataSourceInfo(moduleId.ToString(CultureInfo.InvariantCulture), dataSourceId.ToString(CultureInfo.InvariantCulture), protocol, "TCSUserID", aggregationSubType, aggregationType);
                        }

                        var userName = paramValue == null ? "" : paramValue.ToString();
                        paramValue = this.GetConfigParameterValueFromDataSourceInfo(moduleId.ToString(CultureInfo.InvariantCulture), dataSourceId.ToString(CultureInfo.InvariantCulture), protocol, "TCSPassword", aggregationSubType, aggregationType);
                        var password = paramValue == null ? "" : paramValue.ToString();
                        paramValue = this.GetConfigParameterValueFromDataSourceInfo(moduleId.ToString(CultureInfo.InvariantCulture), dataSourceId.ToString(CultureInfo.InvariantCulture), protocol, "TCSUserAgent", aggregationSubType, aggregationType);
                        var userAgent = paramValue == null ? "" : paramValue.ToString();
                        paramValue = this.GetConfigParameterValueFromDataSourceInfo(moduleId.ToString(CultureInfo.InvariantCulture), dataSourceId.ToString(CultureInfo.InvariantCulture), protocol, "TCSUserAgentPwd", aggregationSubType, aggregationType);
                        var uaPassword = paramValue == null ? "" : paramValue.ToString();
                        
                        loginInfo = new LoginInfo
                        {
                            UserName = userName,
                            Password = password,
                            UserAgent = userAgent,
                            UaPassword = uaPassword,
                            ByPassAuthentication = "1"
                        };
                        retsLoginURL = loginTraceInfo.RETSLoginURL;
                    }
                }
                catch (Exception ex)
                {                  
                }

            }
            return loginInfo;
        }

        public string GetConfigParameterValueFromDataSourceInfo(string moduleId, string dataSourceId, string protocol, string fileFormat, string aggregationSubType="Incremental", string aggregationType="REALListing")
        {
            //RdcCode is data source id. 

            string  paramValue = "";
            using (SystemEntities se = new SystemEntities())
            {
                try
                {
                    var dataSourceResult =
                        se.spDA_TCSDatasource_sel(moduleId.ToString(CultureInfo.InvariantCulture), false).ToList();

                    if (dataSourceResult.Count == 0)
                    {
                        return null;
                    }

                    int dsid = 0;
                    foreach (var item in dataSourceResult)
                    {
                        if (item.OriginalDataSourceID.ToLower().Trim().Equals(dataSourceId.ToLower().Trim()))
                        {
                            dsid = item.datasourceid;
                            break;
                        }
                    }


                    var dataSourceConfigResult =
                        se.spDA_GetDataSourceConfigBasic(dsid.ToString(CultureInfo.InvariantCulture), protocol.ToString(CultureInfo.InvariantCulture), fileFormat.ToString(CultureInfo.InvariantCulture)).ToList();

                    if (dataSourceConfigResult.Count == 0)
                    {
                        return null;
                    }
                    
                    foreach (var item in dataSourceConfigResult)
                    {
                        if (!(item.AggregationSubType.Equals(aggregationSubType) && item.AggregationType.Equals(aggregationType))) continue;
                        else
                        {
                            paramValue = item.FileFormatInfo.Trim();
                            break;
                        }
                    }
                    
                    
                }
                catch (Exception ex)
                {
                }

            }
            return paramValue;
        }

        public bool ExistAggregationTypeFromDataSourceInfo(string moduleId, string dataSourceId, string protocol, string fileFormat, string aggregationType)
        {
            //RdcCode is data source id. 
            bool existAggregationType = false;
           
            using (SystemEntities se = new SystemEntities())
            {
                try
                {
                    var dataSourceResult =
                        se.spDA_TCSDatasource_sel(moduleId.ToString(CultureInfo.InvariantCulture), false).ToList();

                    if (dataSourceResult.Count == 0)
                    {
                        return existAggregationType;
                    }

                    int dsid = 0;
                    foreach (var item in dataSourceResult)
                    {
                        if (item.OriginalDataSourceID.ToLower().Trim().Equals(dataSourceId.ToLower().Trim()))
                        {
                            dsid = item.datasourceid;
                            break;
                        }
                    }


                    var dataSourceConfigResult =
                        se.spDA_GetDataSourceConfigBasic(dsid.ToString(CultureInfo.InvariantCulture), protocol.ToString(CultureInfo.InvariantCulture), fileFormat.ToString(CultureInfo.InvariantCulture)).ToList();

                    if (dataSourceConfigResult.Count == 0)
                    {
                        return existAggregationType;
                    }

                    foreach (var item in dataSourceConfigResult)
                    {
                        if ((item.AggregationType.Equals(aggregationType))) 
                        {
                            existAggregationType = true;
                            break;
                        }
                    }


                }
                catch (Exception ex)
                {
                   
                }

            }
            return existAggregationType;
        }

        public bool ExistDataSource(string dataSourceId)
        {
            bool existDataSource = false;

            using (SystemEntities se = new SystemEntities())
            {
                try
                {
                    var dsid = se.spDA_GetDataSourceID(dataSourceId.ToString(CultureInfo.InvariantCulture)).ToList();

                    existDataSource = (dsid.Count > 0);
                }

                catch (Exception ex)
                {
                   
                }
            }
            
            return existDataSource; 
        }

        public string GetExactDataSourceId(string moduleId, string dataSourceId)
        {
            string exactDataSourceId = string.Empty;
            TCSEntities tcsDb = null;
            tcsDb = new TCSEntities();

            try
            {

                var datasource = from di in tcsDb.mls_vendor
                                 where ((di.moduleid == moduleId) && (di.mlsid == dataSourceId))
                                 select di.mlsid;


                exactDataSourceId = datasource.First();
            }
            catch (Exception ex1)
            {
                exactDataSourceId = string.Empty;
                Console.WriteLine(ex1.Message + " Failed to get datasource id for " + moduleId);
            }
            finally
            {
                tcsDb.Dispose();
            }

            return exactDataSourceId;
        }

        public string GetDataSourceId(string moduleId)
        {
            string dataSourceId = string.Empty;
            TCSEntities tcsDb = null;
            tcsDb = new TCSEntities();

            try
            {

                var datasource = from di in tcsDb.mls_vendor
                                 where (di.moduleid == moduleId)
                                 select di.mlsid;


                dataSourceId = datasource.First();
            }
            catch (Exception ex1)
            {
                dataSourceId = string.Empty;
                Console.WriteLine(ex1.Message + " Failed to get datasource id for " + moduleId);
            }
            finally
            {
                tcsDb.Dispose();
            }

            return dataSourceId;
        }

        public short GetModuleId(string dataSourceId)
        {
            short moduleId = 0;
            TCSEntities tcsDb = null;
            tcsDb = new TCSEntities();

            try
            {

                var module = from di in tcsDb.mls_vendor
                             where (di.mlsid == dataSourceId)
                             select di.moduleid;


                moduleId = Int16.Parse(module.First());
            }
            catch (Exception ex1)
            {
                moduleId = 0;
                Console.WriteLine(ex1.Message + " Failed to get module id for " + dataSourceId);
            }
            finally
            {
                tcsDb.Dispose();
            }
            return moduleId;
        }


        public short GetOtherModuleId(string dataSourceId)
        {
            short moduleId = 0;
            TCSEntitiesProduction tcsDb = null;
            tcsDb = new TCSEntitiesProduction();

            try
            {
                var module = from di in tcsDb.tcs_module_id_to_data_source_id
                             where ((di.data_source_id == dataSourceId) && (di.data_source_id.Substring(di.data_source_id.Length - 2).ToUpper() == "FD" || di.data_source_id.Substring(di.data_source_id.Length - 2).ToUpper() == "OM" || di.data_source_id.Substring(di.data_source_id.Length - 2).ToUpper() == "TC"))
                             select di.module_id;

                moduleId = module.First();
            }
            catch (Exception ex1)
            {
                moduleId = 0;
                Console.WriteLine(ex1.Message + " Failed to get module id for " + dataSourceId);
            }
            finally
            {
                tcsDb.Dispose();
            }
            return moduleId;
        }
        

        public void AnalyzeDef(ref PendingStatusCompareResult result)
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
            
            result.IsOpenHouseOnlyModule = false;
            result.TcsActive = iniReader.Read("Active", "Sorting")??"";
            result.TcsPending = iniReader.Read("Pending", "Sorting")??"";
            result.RdcActive = iniReader.Read("Active", "SortingPublicListingStatus")??"";
            result.RdcSold = iniReader.Read("Sold", "SortingPublicListingStatus") ?? "";
            result.TcsSold = iniReader.Read("Sold", "Sorting")??"";
            result.RdcOffMarket = iniReader.Read("OffMarketOrOther", "SortingPublicListingStatus") ?? "";
            result.TcsOffMarket = iniReader.Read("Expired", "Sorting")??"";
            if(!string.IsNullOrEmpty(result.RdcActive))
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

        public static string FindGap(string strSet, string strToCheck)
        {
            if (strSet == null)
                strSet = "";
            if (strToCheck == null)
                strToCheck = "";
            
            var result = string.Empty;
            var hs = new HashSet<string>();
            foreach (var item in strSet.ToLower().Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries))
            {
                hs.Add(item);
            }

            result = strToCheck.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries).Where(str => !hs.Contains(str.ToLower())).Aggregate(result, (current, str) => current + (str + ","));
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

        public void WriteToReport(StreamWriter sw, PendingStatusCompareResult result)
        {
            var sb = new StringBuilder();
            sb.Append(result.ModuleId).Append(",");
            sb.Append("\"").Append(result.ModuleName).Append("\",");
            sb.Append(result.RdcCode).Append(",");
            sb.Append(result.TraceName).Append(",");
            //sb.Append(result.PropertyClass).Append(",");
            sb.Append("\"").Append(result.TcsPending).Append("\",");
            sb.Append("\"").Append(result.RdcPending).Append("\",");
            sb.Append("\"").Append(result.PendingGap).Append("\",");
            //sb.Append("\"").Append(result.TcsActive).Append("\",");
            //sb.Append("\"").Append(result.RdcActive).Append("\",");
            sb.Append("\"").Append(result.ActiveDifNotInTcs).Append("\",");
            sb.Append("\"").Append(result.ActiveDifNotInRdc).Append("\",");
            //sb.Append("\"").Append(result.TcsSold).Append("\",");
            //sb.Append("\"").Append(result.RdcSold).Append("\",");
            sb.Append("\"").Append(result.SoldDifNotInTcs).Append("\",");
            sb.Append("\"").Append(result.SoldDifNotInRdc).Append("\",");
            //sb.Append("\"").Append(result.TcsOffMarket).Append("\",");
            //sb.Append("\"").Append(result.RdcOffMarket).Append("\",");
            sb.Append("\"").Append(result.OffMarketDifNotInTcs).Append("\",");
            sb.Append("\"").Append(result.OffMarketDifNotInRdc).Append("\",");
            sb.Append("\"").Append(result.TestResult).Append("\",");
            //sb.Append("\"").Append(result.StatusRdcAccountSuccess).Append("\",");
            //sb.Append("\"").Append(result.StatusRdcAccountRequestFailed).Append("\",");
            sb.Append("\"").Append(result.StatusRdcAccountReturnNoRecord??"").Append("\",");
            if (result.IsTpAccountLoginSuccess || string.IsNullOrEmpty(result.StatusRdcAccountReturnNoRecord))
                sb.Append("\"").Append(result.StatusHasNoListing ?? "").Append("\",");
            else if(!string.IsNullOrEmpty(result.TpAccountCheckResult))
                sb.Append(result.TpAccountCheckResult).Append(",");
            else
                sb.Append("Error - Authentication failed").Append(",");
            sb.Append(result.ProdutionListingCount).Append(",");
            sb.Append(result.QaListingCount).Append(",");
            sb.Append(result.IncreaseInActiveCounts??"").Append(",");
                sb.Append(result.RdcCurrentPendingListingCount==0?"":result.RdcCurrentPendingListingCount.ToString(CultureInfo.InvariantCulture)).Append(",");
            sw.WriteLine(sb.ToString());
        }

        public static void GetStatusInfo(PendingStatusCompareResult compareResult, string status, out string statusName, out string statusValue, out string lastModifyDateFieldName, out string lastModifyDateFiledValue)
        {
            statusValue = "";
            var iniReader = new IniFile(compareResult.DefFilePath);
            lastModifyDateFieldName = iniReader.Read("ST_LastMod", "Standard_Search") ?? "";
            var dateFormat = iniReader.Read("SDateFormat", "field_" + lastModifyDateFieldName)??"";
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
                var statusList = status.Split(new []{','}, StringSplitOptions.RemoveEmptyEntries);
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
                searchUrl = ConfigurationManager.AppSettings["TcsUrl"];
            var searchHandler = new SearchHandler(searchUrl);
            return searchHandler.SearchMls(request);
        }

        public void AnlyzeAccountAccessResult(ref PendingStatusCompareResult result)
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
