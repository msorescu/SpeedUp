using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SpeedUp.Model;

namespace SpeedUp.Helper
{
    public class RequestXmlHelper
    {
        private const string SearchRequestDataTimeFormat = "MM/dd/yyyyTHH:mm:ss";
        private const string SearchRequestForAgentRoster = @"<TCService ClientName=""ORCA""><Function>GetAgentRoster</Function><Login><password>{0}</password><password>{1}</password><UserAgent>{2}</UserAgent><RetsUAPwd>{3}</RetsUAPwd></Login><Board BoardId=""99999"" DefPath=""{4}"" MetadataVersion=""0""/><Search BypassARAuthentication=""1"" Count=""0"" RecordsLimit=""200"" ReturnDataAggListingsXML=""1"" /></TCService>";
        private const string SearchRequestForOfficeRoster =@"<TCService ClientName=""ORCA""><Function>GetOfficeRoster</Function><Login><password>{0}</password><password>{1}</password><UserAgent>{2}</UserAgent><RetsUAPwd>{3}</RetsUAPwd></Login><Board BoardId=""99999"" DefPath=""{4}"" MetadataVersion=""0""/><Search BypassARAuthentication=""1"" Count=""0"" RecordsLimit=""200"" ReturnDataAggListingsXML=""1"" /></TCService>";
        private const string SearchRequestForOpenHouse = @"<TCService ClientName=""ORCA""><Function>GetOpenHouse</Function><Login><password>{0}</password><password>{1}</password><UserAgent>{2}</UserAgent><RetsUAPwd>{3}</RetsUAPwd></Login><Board BoardId=""99999"" DefPath=""{4}"" MetadataVersion=""0""/><Search BypassARAuthentication=""1"" Count=""0"" RecordsLimit=""200"" ST_SearchDate=""{5}"" ReturnDataAggListingsXML=""1"" /></TCService>";
        private const string SearchRequestForProperty = @"<TCService ClientName=""ORCA""><Function>GetDataAggListings</Function><Login><password>{0}</password><password>{1}</password><UserAgent>{2}</UserAgent><RetsUAPwd>{3}</RetsUAPwd></Login><Board BoardId=""99999"" DefPath=""{4}"" MetadataVersion=""0""/><Search BypassARAuthentication=""1"" Count=""0""  RecordsLimit=""200""  ST_LastMod=""{5}"" ST_MLSNo=""{6}"" IncludeCDATA=""0"" SelectPicFieldsOnly=""0"" ReturnDataAggListingsXML=""1"" ST_PublicListingStatus=""A""/></TCService>";
        private const string SearchRequestForRetsBlobProperty = @"<TCService ClientName=""ORCA""><Function>GetDataAggListings</Function><Login><password>{0}</password><password>{1}</password><UserAgent>{2}</UserAgent><RetsUAPwd>{3}</RetsUAPwd></Login><Board BoardId=""99999"" DefPath=""{4}"" MetadataVersion=""0""/><Search BypassARAuthentication=""1"" Count=""0""  RecordsLimit=""200""  ST_LastMod=""{5}"" ST_MLSNo=""{6}"" IncludeCDATA=""0"" IncludeRETSBlob=""1""  CompactFormat=""1""  SelectPicFieldsOnly=""0"" ReturnDataAggListingsXML=""1"" ST_PublicListingStatus=""A""/></TCService>";
        //private const string SearchRequestForListhubCount = @"<TCService ClientName=""IDX""><Function>GetIDXListings</Function><Login><password>{0}</password><password>{1}</password><UserAgent>{2}</UserAgent><RetsUAPwd>{3}</RetsUAPwd></Login><Board BoardId=""99999"" DefPath=""{4}"" MetadataVersion=""0""/><Search BypassARAuthentication=""1"" Count=""1""  OverrideRecordsLimit=""999999""  ST_Status=""A""/></TCService>"; // TODO: Initialize to an appropriate value                                                        
        private const string SearchRequestForListhubAgentRosterCount = @"<TCService ClientName=""ORCA""><Function>GetAgentRoster</Function><Login><password>{0}</password><password>{1}</password><UserAgent>{2}</UserAgent><RetsUAPwd>{3}</RetsUAPwd></Login><Board BoardId=""99999"" DefPath=""{4}"" MetadataVersion=""0""/><Search BypassARAuthentication=""1"" Count=""1"" RecordsLimit=""200"" ST_ConditionCode=""{5}"" RETSQueryParameter=""{6}""/></TCService>";
        private const string SearchRequestForListhubOfficeRosterCount = @"<TCService ClientName=""ORCA""><Function>GetOfficeRoster</Function><Login><password>{0}</password><password>{1}</password><UserAgent>{2}</UserAgent><RetsUAPwd>{3}</RetsUAPwd></Login><Board BoardId=""99999"" DefPath=""{4}"" MetadataVersion=""0""/><Search BypassARAuthentication=""1"" Count=""1"" RecordsLimit=""200"" ST_ConditionCode=""{5}"" RETSQueryParameter=""{6}""/></TCService>";
        private const string SearchRequestRDCForListhubCount = @"<TCService ClientName=""ORCA""><Function>GetDataAggListings</Function><Login><password>{0}</password><password>{1}</password><UserAgent>{2}</UserAgent><RetsUAPwd>{3}</RetsUAPwd></Login><Board BoardId=""99999"" DefPath=""{4}"" MetadataVersion=""0""/><Search BypassARAuthentication=""1"" Count=""1""  RecordsLimit=""10"" IncludeCDATA=""0"" SelectPicFieldsOnly=""0"" ReturnDataAggListingsXML=""0"" ST_PublicListingStatus=""{5}"" ST_ConditionCode=""{6}"" RETSQueryParameter=""{7}""/></TCService>";
        private const string SearchRequestRDCForListhubSoldCount = @"<TCService ClientName=""ORCA""><Function>GetDataAggListings</Function><Login><password>{0}</password><password>{1}</password><UserAgent>{2}</UserAgent><RetsUAPwd>{3}</RetsUAPwd></Login><Board BoardId=""99999"" DefPath=""{4}"" MetadataVersion=""0""/><Search BypassARAuthentication=""1"" Count=""1""  RecordsLimit=""10"" IncludeCDATA=""0"" SelectPicFieldsOnly=""0"" ReturnDataAggListingsXML=""0"" ST_SaleDate=""{5}"" ST_PublicListingStatus=""{6}"" ST_ConditionCode=""{7}"" RETSQueryParameter=""{8}""/></TCService>";
        private const string SearchRequestRDCForListhubOffMarketCount = @"<TCService ClientName=""ORCA""><Function>GetDataAggListings</Function><Login><password>{0}</password><password>{1}</password><UserAgent>{2}</UserAgent><RetsUAPwd>{3}</RetsUAPwd></Login><Board BoardId=""99999"" DefPath=""{4}"" MetadataVersion=""0""/><Search BypassARAuthentication=""1"" Count=""1""  RecordsLimit=""10"" IncludeCDATA=""0"" SelectPicFieldsOnly=""0"" ReturnDataAggListingsXML=""0"" ST_LastMod=""{5}"" ST_PublicListingStatus=""{6}"" ST_ConditionCode=""{7}"" RETSQueryParameter=""{8}""/></TCService>";
        private const string SearchRequestForListhubCount = @"<TCService ClientName=""TMK""><Function>GetComparableListings</Function><Login><password>{0}</password><password>{1}</password><UserAgent>{2}</UserAgent><RetsUAPwd>{3}</RetsUAPwd></Login><Board BoardId=""99999"" DefPath=""{4}""/><Search Count=""1""  RecordsLimit=""10""  ST_Status=""{5}""/></TCService>"; // TODO: Initialize to an appropriate value
        private const string SearchRequestForListhubCountExpired = @"<TCService ClientName=""TMK""><Function>GetComparableListings</Function><Login><password>{0}</password><password>{1}</password><UserAgent>{2}</UserAgent><RetsUAPwd>{3}</RetsUAPwd></Login><Board BoardId=""99999"" DefPath=""{4}""/><Search Count=""1""  RecordsLimit=""10""  ST_Status=""{5}"" ST_StatusDate=""{6}""/></TCService>"; // TODO: Initialize to an appropriate value
        private const string SearchRequestForListhubCountSold = @"<TCService ClientName=""TMK""><Function>GetComparableListings</Function><Login><password>{0}</password><password>{1}</password><UserAgent>{2}</UserAgent><RetsUAPwd>{3}</RetsUAPwd></Login><Board BoardId=""99999"" DefPath=""{4}""/><Search Count=""1""  RecordsLimit=""10""  ST_Status=""{5}"" ST_SaleDate=""{6}""/></TCService>"; // TODO: Initialize to an appropriate value
        private const string SearchRequestForListhubOpenHouseCount = @"<TCService ClientName=""ORCA""><Function>GetOpenHouse</Function><Login><password>{0}</password><password>{1}</password><UserAgent>{2}</UserAgent><RetsUAPwd>{3}</RetsUAPwd></Login><Board BoardId=""99999"" DefPath=""{4}"" MetadataVersion=""0""/><Search BypassARAuthentication=""1"" Count=""1"" RecordsLimit=""200"" ST_SearchDate=""{5}"" ST_ConditionCode=""{6}""/></TCService>";

        
        public enum SearchRequestType
        {
            Property,
            AgentRoster,
            OfficeRoster,
            OpenHouse
        }

        public static SearchRequestType GetSearchRequestType(string fileName)
        {
            var resultType = SearchRequestType.Property;
            if(string.IsNullOrEmpty(fileName))
                throw new Exception("File name is not null or empty in Function: GetSearchRequestType");

            if (fileName.IndexOf("ag.sql", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
                resultType = SearchRequestType.AgentRoster;
            else if (fileName.IndexOf("of.sql", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
                resultType = SearchRequestType.OfficeRoster;
            else if (fileName.IndexOf("oh.sql", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
                resultType = SearchRequestType.OpenHouse;
            else
                resultType = SearchRequestType.Property;

            return resultType;
        }

        public static string GetRequestXmlForSampleData(string defPath, LoginInfo login, string mlsNumbers, bool isRetsBlobRequest)
        {
            var last20Days = DateTime.Now.AddDays(-90).ToString(SearchRequestDataTimeFormat) + "-" + DateTime.Now.ToString(SearchRequestDataTimeFormat);
            var requestType = GetSearchRequestType(defPath);
            login.UserName = Util.ConvertStringToXml(login.UserName);
            login.Password = Util.ConvertStringToXml(login.Password);
            login.UserAgent = Util.ConvertStringToXml(login.UserAgent);
            login.UaPassword = Util.ConvertStringToXml(login.UaPassword);
            var requestXml = "";
            switch (requestType)
            {
                case SearchRequestType.AgentRoster:
                    requestXml = string.Format(SearchRequestForAgentRoster, login.UserName, login.Password, login.UserAgent, login.UaPassword, defPath);
                    break;
                case SearchRequestType.OfficeRoster:
                    requestXml = string.Format(SearchRequestForOfficeRoster, login.UserName, login.Password, login.UserAgent, login.UaPassword, defPath);
                    break;
                case SearchRequestType.OpenHouse:
                    var nextOneYear = DateTime.Now.ToString(SearchRequestDataTimeFormat) + "-" + DateTime.Now.AddYears(1).ToString(SearchRequestDataTimeFormat);
                    requestXml = string.Format(SearchRequestForOpenHouse, login.UserName, login.Password, login.UserAgent, login.UaPassword, defPath, nextOneYear);
                    break;
                case SearchRequestType.Property:
                    var propertySearchReqeust = SearchRequestForProperty;
                    if (isRetsBlobRequest)
                        propertySearchReqeust = SearchRequestForRetsBlobProperty;
                    requestXml = string.Format(propertySearchReqeust, login.UserName, login.Password, login.UserAgent, login.UaPassword, defPath, last20Days, mlsNumbers);
                    break;
            }
            return requestXml;
        }

        public static string GetRequestXmlForListhubAgentRosterCount(string defPath, LoginInfo login, string conditionCode = "", string retsQueryParameter = "")
        {
            var requestXml = "";
            if (string.IsNullOrEmpty(defPath))
                throw new Exception("File name is not null or empty in Function: GetSearchRequestType");

            if (defPath.IndexOf("ag.sql", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
            {
                var login_UserName = Util.ConvertStringToXml(login.UserName);
                var login_Password = Util.ConvertStringToXml(login.Password);
                var login_UserAgent = Util.ConvertStringToXml(login.UserAgent);
                var login_UaPassword = Util.ConvertStringToXml(login.UaPassword);
                requestXml = string.Format(SearchRequestForListhubAgentRosterCount, login_UserName, login_Password, login_UserAgent, login_UaPassword, defPath, conditionCode, retsQueryParameter);
            }
            return requestXml;
        }

        public static string GetRequestXmlForListhubOpenHouseCount(string defPath, LoginInfo login, string conditionCode = "")
        {
            var requestXml = "";
            if (string.IsNullOrEmpty(defPath))
                throw new Exception("File name is not null or empty in Function: GetSearchRequestType");

            if ((defPath.IndexOf("oh.sql", 0, StringComparison.CurrentCultureIgnoreCase) > -1) ||
                (defPath.IndexOf("oh .sql", 0, StringComparison.CurrentCultureIgnoreCase) > -1))
            {
                var login_UserName = Util.ConvertStringToXml(login.UserName);
                var login_Password = Util.ConvertStringToXml(login.Password);
                var login_UserAgent = Util.ConvertStringToXml(login.UserAgent);
                var login_UaPassword = Util.ConvertStringToXml(login.UaPassword);
                var nextOneYear = DateTime.Now.ToString(SearchRequestDataTimeFormat) + "-" + DateTime.Now.AddYears(1).ToString(SearchRequestDataTimeFormat);
                requestXml = string.Format(SearchRequestForListhubOpenHouseCount, login_UserName, login_Password, login_UserAgent, login_UaPassword, defPath, nextOneYear, conditionCode);
            }
            return requestXml;
        }

        public static string GetRequestXmlForListhubOfficeRosterCount(string defPath, LoginInfo login, string conditionCode = "", string retsQueryParameter = "")
        {
            var requestXml = "";
            if (string.IsNullOrEmpty(defPath))
                throw new Exception("File name is not null or empty in Function: GetSearchRequestType");

            if (defPath.IndexOf("of.sql", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
            {
                var login_UserName = Util.ConvertStringToXml(login.UserName);
                var login_Password = Util.ConvertStringToXml(login.Password);
                var login_UserAgent = Util.ConvertStringToXml(login.UserAgent);
                var login_UaPassword = Util.ConvertStringToXml(login.UaPassword);
                requestXml = string.Format(SearchRequestForListhubOfficeRosterCount, login_UserName, login_Password, login_UserAgent, login_UaPassword, defPath, conditionCode, retsQueryParameter);
            }
            return requestXml;
        }
        public static string GetRequestXmlRDCForListhubCount(string defPath, LoginInfo login, string status, string conditionCode = "", string retsQueryParameter="")
        {
            var requestXml = "";
            if (string.IsNullOrEmpty(defPath))
                throw new Exception("File name is not null or empty in Function: GetSearchRequestType");

            if (defPath.IndexOf("ag.sql", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
                return "";
            else if (defPath.IndexOf("of.sql", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
                return "";
            else if (defPath.IndexOf("oh.sql", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
                return "";
            //var last10Years = DateTime.Now.AddYears(-10).ToString(SearchRequestDataTimeFormat) + "-" + DateTime.Now.ToString(SearchRequestDataTimeFormat);
            var login_UserName = Util.ConvertStringToXml(login.UserName);
            var login_Password = Util.ConvertStringToXml(login.Password);
            var login_UserAgent = Util.ConvertStringToXml(login.UserAgent);
            var login_UaPassword = Util.ConvertStringToXml(login.UaPassword);
            requestXml = string.Format(SearchRequestRDCForListhubCount, login_UserName, login_Password, login_UserAgent, login_UaPassword, defPath, status, conditionCode, retsQueryParameter);
            return requestXml;
        }
        public static string GetRequestXmlRDCForListhubCountSold(string defPath, LoginInfo login, string status, string conditionCode = "", string retsQueryParameter="")
        {
            var requestXml = "";
            if (string.IsNullOrEmpty(defPath))
                throw new Exception("File name is not null or empty in Function: GetSearchRequestType");

            if (defPath.IndexOf("ag.sql", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
                return "";
            else if (defPath.IndexOf("of.sql", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
                return "";
            else if (defPath.IndexOf("oh.sql", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
                return "";
            var last6Months = DateTime.Now.AddMonths(-6).ToString(SearchRequestDataTimeFormat) + "-" + DateTime.Now.ToString(SearchRequestDataTimeFormat);
            var login_UserName = Util.ConvertStringToXml(login.UserName);
            var login_Password = Util.ConvertStringToXml(login.Password);
            var login_UserAgent = Util.ConvertStringToXml(login.UserAgent);
            var login_UaPassword = Util.ConvertStringToXml(login.UaPassword);
            requestXml = string.Format(SearchRequestRDCForListhubSoldCount, login_UserName, login_Password, login_UserAgent, login_UaPassword, defPath, last6Months, status, conditionCode, retsQueryParameter);
            return requestXml;
        }

        public static string GetRequestXmlRDCForListhubCountOffMarket(string defPath, LoginInfo login, string status, string conditionCode = "", string retsQueryParameter = "")
        {
            var requestXml = "";
            if (string.IsNullOrEmpty(defPath))
                throw new Exception("File name is not null or empty in Function: GetSearchRequestType");

            if (defPath.IndexOf("ag.sql", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
                return "";
            else if (defPath.IndexOf("of.sql", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
                return "";
            else if (defPath.IndexOf("oh.sql", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
                return "";
            var last6Months = DateTime.Now.AddMonths(-6).ToString(SearchRequestDataTimeFormat) + "-" + DateTime.Now.ToString(SearchRequestDataTimeFormat);
            var login_UserName = Util.ConvertStringToXml(login.UserName);
            var login_Password = Util.ConvertStringToXml(login.Password);
            var login_UserAgent = Util.ConvertStringToXml(login.UserAgent);
            var login_UaPassword = Util.ConvertStringToXml(login.UaPassword);
            requestXml = string.Format(SearchRequestRDCForListhubOffMarketCount, login_UserName, login_Password, login_UserAgent, login_UaPassword, defPath, last6Months, status, conditionCode, retsQueryParameter);
            return requestXml;
        }
        
        public static string GetRequestXmlForListhubCount(string defPath, LoginInfo login, string status)
        {
            var requestXml = "";
            if (string.IsNullOrEmpty(defPath))
                throw new Exception("File name is not null or empty in Function: GetSearchRequestType");

            if (defPath.IndexOf("ag.sql", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
                return "";
            else if (defPath.IndexOf("of.sql", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
                return "";
            else if (defPath.IndexOf("oh.sql", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
                return "";
            var login_UserName = Util.ConvertStringToXml(login.UserName);
            var login_Password = Util.ConvertStringToXml(login.Password);
            var login_UserAgent = Util.ConvertStringToXml(login.UserAgent);
            var login_UaPassword = Util.ConvertStringToXml(login.UaPassword);
            requestXml = string.Format(SearchRequestForListhubCount, login_UserName, login_Password, login_UserAgent, login_UaPassword, defPath, status);
            return requestXml;
        }
        public static string GetRequestXmlForListhubCountExpired(string defPath, LoginInfo login, string status)
        {
            var requestXml = "";
            if (string.IsNullOrEmpty(defPath))
                throw new Exception("File name is not null or empty in Function: GetSearchRequestType");

            if (defPath.IndexOf("ag.sql", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
                return "";
            else if (defPath.IndexOf("of.sql", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
                return "";
            else if (defPath.IndexOf("oh.sql", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
                return "";
            var last6Months = DateTime.Now.AddMonths(-6).ToString(SearchRequestDataTimeFormat) + "-" + DateTime.Now.ToString(SearchRequestDataTimeFormat);
            var login_UserName = Util.ConvertStringToXml(login.UserName);
            var login_Password = Util.ConvertStringToXml(login.Password);
            var login_UserAgent = Util.ConvertStringToXml(login.UserAgent);
            var login_UaPassword = Util.ConvertStringToXml(login.UaPassword);
            requestXml = string.Format(SearchRequestForListhubCountExpired, login_UserName, login_Password, login_UserAgent, login_UaPassword, defPath, status, last6Months);
            return requestXml;
        }
        public static string GetRequestXmlForListhubCountSold(string defPath, LoginInfo login, string status)
        {
            var requestXml = "";
            if (string.IsNullOrEmpty(defPath))
                throw new Exception("File name is not null or empty in Function: GetSearchRequestType");

            if (defPath.IndexOf("ag.sql", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
                return "";
            else if (defPath.IndexOf("of.sql", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
                return "";
            else if (defPath.IndexOf("oh.sql", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
                return "";
            var last6Months = DateTime.Now.AddMonths(-6).ToString(SearchRequestDataTimeFormat) + "-" + DateTime.Now.ToString(SearchRequestDataTimeFormat);
            var login_UserName = Util.ConvertStringToXml(login.UserName);
            var login_Password = Util.ConvertStringToXml(login.Password);
            var login_UserAgent = Util.ConvertStringToXml(login.UserAgent);
            var login_UaPassword = Util.ConvertStringToXml(login.UaPassword);
            requestXml = string.Format(SearchRequestForListhubCountSold, login_UserName, login_Password, login_UserAgent, login_UaPassword, defPath, status, last6Months);
            return requestXml;
        }


    }
}
