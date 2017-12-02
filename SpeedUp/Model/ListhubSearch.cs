using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Data.Objects;
using System.Data.EntityClient;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.TeamFoundation.Framework.Client;
using Tcs.Mls;
using System.Windows.Threading;
using System.Xml;
using System.Net;
using System.Web;
using SpeedUp.Model;
using Util = SpeedUp.Helper.Util;
using SpeedUp.Helper;
using System.Xml.Linq;
using net.toppro.components.mls.engine;
using System.Configuration;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SpeedUp.Model
{
    class ListhubSearch
    {
        private string moduleId;
        private string dataSourceId;
        private string boardId;
        public static string workingDirectory = string.Format(@"{0}\Work\rootR9\TCDEF\TCDEF_a.1_Main\", ConfigurationManager.AppSettings["Drive"]); 
        private const string DEF_LOCATION = @"\\netapp02\tcs\Templates\MLS DEFs";
        private const string SECTION_SETTING = "RETS Setting";
        private const string INSTANCE_DEF = "instance.def";
        private LoginInfo loginInfo;        
        private BoardCredentials boardInfo;
        public static string  listhubPath = string.Format(@"{0}\listhub\",ConfigurationManager.AppSettings["Drive"]);
        private string dmqlFilePath = string.Format(@"{0}\listhub\DMQL-",ConfigurationManager.AppSettings["Drive"]);
        private string loginFilePath = string.Format(@"{0}\listhub\CleanLogins-",ConfigurationManager.AppSettings["Drive"]);
        private string exceptFilePath = string.Format(@"{0}\listhub\Exception\Error-",ConfigurationManager.AppSettings["Drive"]);
        private StringBuilder dmqlSb;
        private StringBuilder loginSb;
        private StringBuilder badMoudleSb;
        private String[] status = { "A", "E", "P", "S" };
        private String[] rdc_status = { "A", "S", "O" };
        private String[] rdc_other_status = { "S", "O"};
        private String[] exceptional_error_messages = { "Duplicate login detected - this session is invalid", "Miscellaneous Search Error. RETS-Session-ID is invalid."};
        private static readonly String[] DATA_SOURCE_NAMES_WITH_COUNT_ISSUES = new String[]{
            "ARAR",
            "BBAL",
            "BCOK",
            "BICA",
            "BLAR",
            "CAAR",
            "CGMI",
            "CHNY",
            "CLGA",
            "CLMS",
            "CLNC",
            "CPAR",
            "DCNE",
            "DUGA",
            "DUTX",
            "EDAR",
            "ESVA",
            "IWCA",
            "JAND",
            "JCAR",
            "KAUT",
            "LICA",
            "LKFL",
            "LKOR",
            "MAAR",
            "MDCA",
            "MENV",
            "MIMI",
            "MNPA",
            "NEGA",
            "NOTX",
            "PAAZ",
            "PANE",
            "PVTX",
            "RUNC",
            "SAAZ",
            "SJPR",
            "SWMS",
            "TCCA",
            "UVTX",
            "WAPA",
            "WEAL",
            "WECO"
        };

        //private static readonly String[] DATA_SOURCE_OH_NAMES_WITH_COUNT_ISSUES = new String[]{
        //    "ASGA",
        //    "GAFL",
        //    "PEFL",
        //    "CEWI",
        //    "NEWI",
        //    "CHWY",
        //    "SJFL",
        //    "LEID",
        //    "LAOK",
        //    "TANC",
        //    "SDCA",
        //    "LAKS",
        //    "ROIL",
        //    "HUKS",
        //    "BNIL",
        //    "MONY",
        //    "BLIN",
        //    "CIIN",
        //    "ECIN",
        //    "EVIN",
        //    "FWIN",
        //    "KCIN",
        //    "KEIN",
        //    "NCIN",
        //    "SBIN",
        //    "WAIN",
        //    "CEPA",
        //    "NKKY",
        //    "LRAR",
        //    "TYTX",
        //    "ALCA",
        //    "RSNV",
        //    "ALNY",
        //    "CHVA",
        //    "HAVA",
        //    "CVNC",
        //    "WIOR",
        //    "MBSC",
        //    "TOKS",
        //    "CMNJ",
        //    "ANSC",
        //    "FLSC",
        //    "ACCA",
        //    "BGKY",
        //    "GRSC",
        //    "NONE",
        //    "NCWV",
        //    "TRMI",
        //    "CETN",
        //    "ACNJ",
        //    "TCWA",
        //    "LEMI",
        //    "CSWY",
        //    "WRGA",
        //    "ICIA",
        //    "MAMN",
        //    "MIND",
        //    "SFNM",
        //    "METN",
        //    "PEIL",
        //    "DPIL",
        //    "GJCO",
        //    "MAKS",
        //    "JUAK",
        //    "FSAR",
        //    "SAVA"
        //};

        private bool hasAddedToLogin = false;
        private HashSet<String> moduleSet = new HashSet<string>();
        private Dictionary<string, LoginInfoExtra> loginDict = new Dictionary<string, LoginInfoExtra>(); 

        public LoginInfo Login
        {
            get { return loginInfo; }
            set { loginInfo = value; }
        }
                
        public string WorkingDirectory
        {
            get { return workingDirectory; }
            set { workingDirectory = value; }
        }

        public string BoardId
        {
            get { return boardId; }
            set { boardId = value; }
        }
        public string ModuleId
        {
            get { return moduleId; }
            set { moduleId = value; }
        }

        public string DataSourceId
        {
            get { return dataSourceId; }
            set { dataSourceId = value; }
        }

        public string DmqlFilePath
        {
            get { return dmqlFilePath; }
            set { dmqlFilePath = value; }
        }

        public string LoginFilePath
        {
            get { return loginFilePath; }
            set { loginFilePath = value; }
        }

        public ObservableCollection<DefFileData> DefFileDataCollection
        { get { return _defFileDataCollection; } }

        readonly ObservableCollection<DefFileData> _defFileDataCollection =
        new ObservableCollection<DefFileData>();

        public ListhubSearch()
        {
            badMoudleSb = new StringBuilder();
            dmqlSb = new StringBuilder();
            dmqlSb.Append("module_id,mls_name,data_source_id,resource_type,resource,class,search_limit,search_format,count_records_active,count_records_expired,count_records_pending,count_records_sold,last_modified_date_field,last_modified_date_format,sold_date_field,sold_date_format,expired_date_field,expired_date_format,key_field,status_active_dqml_query,status_expired_dqml_query,status_pending_dqml_query,status_sold_dqml_query,photo_collected_yn,photo_type,photo_count_field,photo_last_modified_field,photo_last_modified_format\r\n");
            loginSb = new StringBuilder();
            loginSb.Append("module_id,mls_name,data_source_id,login_type,rets_url,user_name,password,user_agent,user_agent_password,rets_version,http_version,http_method,auth_type,note\r\n");
        }

        public ListhubSearch(string dmqlHeader)
        {
            badMoudleSb = new StringBuilder();
            dmqlSb = new StringBuilder();
            dmqlSb.Append(dmqlHeader);
            loginSb = new StringBuilder();
            loginSb.Append("module_id,mls_name,data_source_id,login_type,rets_url,user_name,password,user_agent,user_agent_password,rets_version,http_version,http_method,auth_type,note\r\n");
        }

        public ListhubSearch(string dmqlHeader, string loginHeader)
        {
            badMoudleSb = new StringBuilder();
            dmqlSb = new StringBuilder();
            dmqlSb.Append(dmqlHeader);
            loginSb = new StringBuilder();
            loginSb.Append(loginHeader);
        }

        public void WriteToFile()
        {
            dmqlFilePath += DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".csv";
            loginFilePath += DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".csv";
            File.WriteAllText(dmqlFilePath, dmqlSb.ToString());
            File.WriteAllText(loginFilePath, loginSb.ToString());

        }

        public void search(string boardId, string moduleId, BoardCredentials boardInfo)
        {
            BoardId = boardId;
            ModuleId = moduleId;
            this.boardInfo = boardInfo;

            loginInfo = new LoginInfo();
            loginInfo.ByPassAuthentication = "1";
            loginInfo.UaPassword = boardInfo.UserAgentPass == null ? "" : boardInfo.UserAgentPass;
            loginInfo.UserAgent = boardInfo.UserAgent == null ? "" : boardInfo.UserAgent;
            loginInfo.UserName = boardInfo.Username == null ? "" : boardInfo.Username;
            loginInfo.Password = boardInfo.Password==null?"":boardInfo.Password;
            hasAddedToLogin = false;
            if (!boardInfo.IsValidCredential)
            {
                loginSb.Append(boardInfo.ModuleId).Append(",");
                loginSb.Append(boardInfo.ModuleName).Append(",");
                loginSb.Append(boardInfo.DatasourceId.Trim()).Append(",");
                loginSb.Append(boardInfo.LoginType).Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                if(boardInfo.isRets)
                    loginSb.Append("NoAgentCredential").Append("\r\n");
                else
                    loginSb.Append("NonRETS").Append("\r\n");

                return;
            }            
            var helper = new PsaHelper();
            var connections = helper.GetDefConnections(Int16.Parse(ModuleId));
            try
            {
                foreach (var connection in connections)
                {                    
                    if (connection.connection_name.EndsWith("ag.sql", StringComparison.OrdinalIgnoreCase)) continue;
                    if (connection.connection_name.EndsWith("of.sql", StringComparison.OrdinalIgnoreCase)) continue;
                    if (connection.connection_name.EndsWith("da.sql", StringComparison.OrdinalIgnoreCase)) continue;
                    if (connection.connection_name.EndsWith("oh .sql", StringComparison.OrdinalIgnoreCase)) continue;
                    if (connection.connection_name.EndsWith("oh.sql", StringComparison.OrdinalIgnoreCase)) continue;
                    string defFilePath = connection.definition_file;
                    GetSampleDataForSingleDef(System.IO.Path.GetFileName(defFilePath), listhubPath + ModuleId + "/data", listhubPath + ModuleId + "/log", listhubPath + ModuleId + "/result");
                }            
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + "Failed to generate report for Module:" + ModuleId);
            }
        }

        public void rdcSearch(string boardId, string moduleId, string dataSourceId, BoardCredentials boardInfo)
        {
            BoardId = boardId;
            ModuleId = moduleId;
            DataSourceId = dataSourceId;
            this.boardInfo = boardInfo;

            loginInfo = new LoginInfo();
            loginInfo.ByPassAuthentication = "1";
            loginInfo.UaPassword = boardInfo.UserAgentPass == null ? "" : boardInfo.UserAgentPass;
            loginInfo.UserAgent = boardInfo.UserAgent == null ? "" : boardInfo.UserAgent;
            loginInfo.UserName = boardInfo.Username == null ? "" : boardInfo.Username;
            loginInfo.Password = boardInfo.Password == null ? "" : boardInfo.Password;
            hasAddedToLogin = false;
                        
            if (!boardInfo.IsValidCredential)
            {
                loginSb.Append(boardInfo.ModuleId).Append(",");
                loginSb.Append(boardInfo.ModuleName).Append(",");
                loginSb.Append(boardInfo.DatasourceId.Trim()).Append(",");
                loginSb.Append(boardInfo.LoginType).Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                if (boardInfo.isRets)
                    loginSb.Append("NoAgentCredential").Append("\r\n");
                else
                    loginSb.Append("NonRETS").Append("\r\n");

                return;
            }

            var helper = new PsaHelper();
            var paramValue = helper.GetConfigParameterValueFromDataSourceInfo(ModuleId, DataSourceId,  "TCS", "PublicStatuses");
            string publicStatuses = paramValue == null ? "" : paramValue.ToString();
            paramValue = helper.GetConfigParameterValueFromDataSourceInfo(ModuleId, DataSourceId, "TCS", "TCSConditionCode");
            if (string.IsNullOrWhiteSpace(paramValue))
                paramValue = helper.GetConfigParameterValueFromDataSourceInfo(ModuleId, DataSourceId, "TCS", "TCSConditionCode", "Full");
            
            string conditionCode = paramValue == null ? "" : paramValue.ToString();

            paramValue = helper.GetConfigParameterValueFromDataSourceInfo(ModuleId, DataSourceId, "TCS", "TCSRETSQueryParameter");
            if (string.IsNullOrWhiteSpace(paramValue))
                paramValue = helper.GetConfigParameterValueFromDataSourceInfo(ModuleId, DataSourceId, "TCS", "TCSRETSQueryParameter", "Full");

            string retsQueryParameter = paramValue == null ? "" : paramValue.ToString();

            var retsSearchPhotoQuery = string.Empty;
            var useImages = helper.ExistAggregationTypeFromDataSourceInfo(ModuleId, dataSourceId, "TCS", "TCSUserID", "REALImage");
            if (useImages)
            {
                paramValue = helper.GetConfigParameterValueFromDataSourceInfo(ModuleId, DataSourceId, "TCS", "RETSSearchQuery", "Incremental", "REALImage");
                if (string.IsNullOrWhiteSpace(paramValue))
                    paramValue = helper.GetConfigParameterValueFromDataSourceInfo(ModuleId, DataSourceId, "TCS", "RETSSearchQuery", "Full", "REALImage");
                if (string.IsNullOrWhiteSpace(paramValue))
                    paramValue = helper.GetConfigParameterValueFromDataSourceInfo(ModuleId, DataSourceId, "TCS", "RETSSearchQuery", "Custom", "REALImage");

                retsSearchPhotoQuery = paramValue == null ? "" : paramValue.ToString();
            }
            

            var connections = helper.GetDefConnections(Int16.Parse(ModuleId));
            try
            {
                foreach (var connection in connections)
                {
                    if (connection.connection_name.EndsWith("ag.sql", StringComparison.OrdinalIgnoreCase)) continue;
                    if (connection.connection_name.EndsWith("of.sql", StringComparison.OrdinalIgnoreCase)) continue;
                    if (connection.connection_name.EndsWith("da.sql", StringComparison.OrdinalIgnoreCase)) continue;
                    if (connection.connection_name.EndsWith("oh .sql", StringComparison.OrdinalIgnoreCase)) continue;
                    if (connection.connection_name.EndsWith("oh.sql", StringComparison.OrdinalIgnoreCase)) continue;
                    string defFilePath = connection.definition_file;
                    GetSampleDataForRDCSingleDef(System.IO.Path.GetFileName(defFilePath), listhubPath + ModuleId + "/data", listhubPath + ModuleId + "/log", listhubPath + ModuleId + "/result", publicStatuses, conditionCode, retsQueryParameter, useImages ? "y":"n", retsSearchPhotoQuery);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + "Failed to generate report for Module:" + ModuleId);
            }
        }
                
        public void rdcOtherSearch(string boardId, string moduleId, string dataSourceId, BoardCredentials boardInfo)
        {
            BoardId = boardId;
            ModuleId = moduleId;
            DataSourceId = dataSourceId;
            this.boardInfo = boardInfo;

            loginInfo = new LoginInfo();
            loginInfo.ByPassAuthentication = "1";
            loginInfo.UaPassword = boardInfo.UserAgentPass == null ? "" : boardInfo.UserAgentPass;
            loginInfo.UserAgent = boardInfo.UserAgent == null ? "" : boardInfo.UserAgent;
            loginInfo.UserName = boardInfo.Username == null ? "" : boardInfo.Username;
            loginInfo.Password = boardInfo.Password == null ? "" : boardInfo.Password;
            hasAddedToLogin = false;
            if (!boardInfo.IsValidCredential)
            {
                loginSb.Append(boardInfo.ModuleId).Append(",");
                loginSb.Append(boardInfo.ModuleName).Append(",");
                loginSb.Append(boardInfo.DatasourceId.Trim()).Append(",");
                loginSb.Append(boardInfo.LoginType).Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                if (boardInfo.isRets)
                    loginSb.Append("NoAgentCredential").Append("\r\n");
                else
                    loginSb.Append("NonRETS").Append("\r\n");

                return;
            }

            var helper = new PsaHelper();
            var paramValue = helper.GetConfigParameterValueFromDataSourceInfo(ModuleId, DataSourceId, "TCS", "PublicStatuses");
            if (string.IsNullOrWhiteSpace(paramValue))
                paramValue = helper.GetConfigParameterValueFromDataSourceInfo(ModuleId, DataSourceId, "TCS", "Statuses");

            string publicStatuses = paramValue == null ? "" : paramValue.ToString().Replace("E","O");
            
            paramValue = helper.GetConfigParameterValueFromDataSourceInfo(ModuleId, DataSourceId, "TCS", "TCSConditionCode");
            if (string.IsNullOrWhiteSpace(paramValue))
                paramValue = helper.GetConfigParameterValueFromDataSourceInfo(ModuleId, DataSourceId, "TCS", "TCSConditionCode", "Full");
            
            string conditionCode = paramValue == null ? "" : paramValue.ToString();

            paramValue = helper.GetConfigParameterValueFromDataSourceInfo(ModuleId, DataSourceId, "TCS", "TCSRETSQueryParameter");
            if (string.IsNullOrWhiteSpace(paramValue))
                paramValue = helper.GetConfigParameterValueFromDataSourceInfo(ModuleId, DataSourceId, "TCS", "TCSRETSQueryParameter", "Full");

            string retsQueryParameter = paramValue == null ? "" : paramValue.ToString();
            
            var retsSearchPhotoQuery = string.Empty;
            var useImages = helper.ExistAggregationTypeFromDataSourceInfo(ModuleId, dataSourceId, "TCS", "TCSUserID", "REALImage");
            if (useImages)
            {
                paramValue = helper.GetConfigParameterValueFromDataSourceInfo(ModuleId, DataSourceId, "TCS", "RETSSearchQuery", "Incremental", "REALImage");
                if (string.IsNullOrWhiteSpace(paramValue))
                    paramValue = helper.GetConfigParameterValueFromDataSourceInfo(ModuleId, DataSourceId, "TCS", "RETSSearchQuery", "Full", "REALImage");
                if (string.IsNullOrWhiteSpace(paramValue))
                    paramValue = helper.GetConfigParameterValueFromDataSourceInfo(ModuleId, DataSourceId, "TCS", "RETSSearchQuery", "Custom", "REALImage");

                retsSearchPhotoQuery = paramValue == null ? "" : paramValue.ToString();
            }

            var connections = helper.GetDefConnections(Int16.Parse(ModuleId));
            try
            {
                foreach (var connection in connections)
                {
                    if (connection.connection_name.EndsWith("ag.sql", StringComparison.OrdinalIgnoreCase)) continue;
                    if (connection.connection_name.EndsWith("of.sql", StringComparison.OrdinalIgnoreCase)) continue;
                    if (connection.connection_name.EndsWith("da.sql", StringComparison.OrdinalIgnoreCase)) continue;
                    if (connection.connection_name.EndsWith("oh .sql", StringComparison.OrdinalIgnoreCase)) continue;
                    if (connection.connection_name.EndsWith("oh.sql", StringComparison.OrdinalIgnoreCase)) continue;
                    string defFilePath = connection.definition_file;
                    GetSampleDataForRDCOtherSingleDef(System.IO.Path.GetFileName(defFilePath), listhubPath + ModuleId + "/data", listhubPath + ModuleId + "/log", listhubPath + ModuleId + "/result", publicStatuses, conditionCode, retsQueryParameter, useImages ? "y":"n", retsSearchPhotoQuery);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + "Failed to generate report for Module:" + ModuleId);
            }            
        }

        public void rosterSearch(string boardId, string moduleId, BoardCredentials boardInfo)
        {
            BoardId = boardId;
            ModuleId = moduleId;
            this.boardInfo = boardInfo;

            loginInfo = new LoginInfo();
            loginInfo.ByPassAuthentication = "1";
            loginInfo.UaPassword = boardInfo.UserAgentPass == null ? "" : boardInfo.UserAgentPass;
            loginInfo.UserAgent = boardInfo.UserAgent == null ? "" : boardInfo.UserAgent;
            loginInfo.UserName = boardInfo.Username == null ? "" : boardInfo.Username;
            loginInfo.Password = boardInfo.Password == null ? "" : boardInfo.Password;
            hasAddedToLogin = false;            
            if (!boardInfo.IsValidCredential)
            {
                loginSb.Append(boardInfo.ModuleId).Append(",");
                loginSb.Append(boardInfo.ModuleName).Append(",");
                loginSb.Append(boardInfo.DatasourceId.Trim()).Append(",");
                loginSb.Append(boardInfo.LoginType).Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                if (boardInfo.isRets)
                    loginSb.Append("NoAgentCredential").Append("\r\n");
                else
                    loginSb.Append("NonRETS").Append("\r\n");

                return;
            }

            var helper = new PsaHelper();
            var connections = helper.GetDefConnections(Int16.Parse(ModuleId));
            try
            {
                if (boardInfo.LoginType == "Master" && boardInfo.Username == "fakeuser" && boardInfo.Password == "fakepassword")
                {
                    //get the master account info
                    foreach (var connection in connections)
                    {
                        if (connection.connection_name.EndsWith("ag.sql", StringComparison.OrdinalIgnoreCase)) continue;
                        if (connection.connection_name.EndsWith("of.sql", StringComparison.OrdinalIgnoreCase)) continue;
                        if (connection.connection_name.EndsWith("da.sql", StringComparison.OrdinalIgnoreCase)) continue;
                        if (connection.connection_name.EndsWith("oh .sql", StringComparison.OrdinalIgnoreCase)) continue;
                        if (connection.connection_name.EndsWith("oh.sql", StringComparison.OrdinalIgnoreCase)) continue;
                        string defFilePath = connection.definition_file;
                        GetMasterAccountForSingleDef(System.IO.Path.GetFileName(defFilePath), ref loginInfo);
                        break;
                    }

                }
                foreach (var connection in connections)
                {
                    if (connection.connection_name.EndsWith("ag.sql", StringComparison.OrdinalIgnoreCase) ||
                        connection.connection_name.EndsWith("of.sql", StringComparison.OrdinalIgnoreCase))
                    {                        
                        string defFilePath = connection.definition_file;
                        GetSampleDataForSingleRoster(System.IO.Path.GetFileName(defFilePath), listhubPath + ModuleId + "/data", listhubPath + ModuleId + "/log", listhubPath + ModuleId + "/result");                        
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + "Failed to generate report for Module:" + ModuleId);
            }
        }
        
        public void rosterRDCSearch(string boardId, string moduleId, string dataSourceId, BoardCredentials boardInfo)
        {
            BoardId = boardId;
            ModuleId = moduleId;
            DataSourceId = dataSourceId;
            this.boardInfo = boardInfo;

            loginInfo = new LoginInfo();
            loginInfo.ByPassAuthentication = "1";
            loginInfo.UaPassword = boardInfo.UserAgentPass == null ? "" : boardInfo.UserAgentPass;
            loginInfo.UserAgent = boardInfo.UserAgent == null ? "" : boardInfo.UserAgent;
            loginInfo.UserName = boardInfo.Username == null ? "" : boardInfo.Username;
            loginInfo.Password = boardInfo.Password == null ? "" : boardInfo.Password;
            hasAddedToLogin = false;
            if (!boardInfo.IsValidCredential)
            {
                loginSb.Append(boardInfo.ModuleId).Append(",");
                loginSb.Append(boardInfo.ModuleName).Append(",");
                loginSb.Append(boardInfo.DatasourceId.Trim()).Append(",");
                loginSb.Append(boardInfo.LoginType).Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                if (boardInfo.isRets)
                    loginSb.Append("NoAgentCredential").Append("\r\n");
                else
                    loginSb.Append("NonRETS").Append("\r\n");

                return;
            }

            var helper = new PsaHelper();
            var paramValue = helper.GetConfigParameterValueFromDataSourceInfo(ModuleId, DataSourceId, "TCS", "TCSConditionCode","Incremental","REALRoster");
            if (string.IsNullOrWhiteSpace(paramValue))
                paramValue = helper.GetConfigParameterValueFromDataSourceInfo(ModuleId, DataSourceId, "TCS", "TCSConditionCode", "Full", "REALRoster");

            string conditionCode = paramValue == null ? "" : paramValue.ToString();

            //TCSRETSQueryParameter is not working when run this sp:

            //USE [System]
            //GO

            //DECLARE	@return_value int

            //EXEC	@return_value = [dbo].[spDA_GetDataSourceConfigBasic]
            //        @ID = N'815',
            //        @PROTOCOL = N'TCS',
            //        @FILEFORMAT = N'TCSRETSQueryParameter'

            //SELECT	'Return Value' = @return_value

            //GO

            //paramValue = helper.GetConfigParameterValueFromDataSourceInfo(ModuleId, DataSourceId, "TCS", "TCSRETSQueryParameter", "Incremental", "REALRoster");
            //if (string.IsNullOrWhiteSpace(paramValue))
            //    paramValue = helper.GetConfigParameterValueFromDataSourceInfo(ModuleId, DataSourceId, "TCS", "TCSRETSQueryParameter", "Full", "REALRoster");

            //string retsQueryParameter = paramValue == null ? "" : paramValue.ToString();

            var retsQueryParameter = "";

            var connections = helper.GetDefConnections(Int16.Parse(ModuleId));

            try
            {
                foreach (var connection in connections)
                {
                    if (connection.connection_name.EndsWith("ag.sql", StringComparison.OrdinalIgnoreCase) ||
                        connection.connection_name.EndsWith("of.sql", StringComparison.OrdinalIgnoreCase))
                    {
                        string defFilePath = connection.definition_file;
                        GetSampleDataForSingleRDCRoster(System.IO.Path.GetFileName(defFilePath), listhubPath + ModuleId + "/data", listhubPath + ModuleId + "/log", listhubPath + ModuleId + "/result", conditionCode, retsQueryParameter);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + "Failed to generate report for Module:" + ModuleId);
            }            
        }

        public void openhouseSearch(string boardId, string moduleId, string dataSourceId, BoardCredentials boardInfo)
        {
            BoardId = boardId;
            ModuleId = moduleId;
            DataSourceId = dataSourceId;

            this.boardInfo = boardInfo;

            loginInfo = new LoginInfo();
            loginInfo.ByPassAuthentication = "1";
            loginInfo.UaPassword = boardInfo.UserAgentPass == null ? "" : boardInfo.UserAgentPass;
            loginInfo.UserAgent = boardInfo.UserAgent == null ? "" : boardInfo.UserAgent;
            loginInfo.UserName = boardInfo.Username == null ? "" : boardInfo.Username;
            loginInfo.Password = boardInfo.Password == null ? "" : boardInfo.Password;
            hasAddedToLogin = false;
            if (!boardInfo.IsValidCredential)
            {
                loginSb.Append(boardInfo.ModuleId).Append(",");
                loginSb.Append(boardInfo.ModuleName).Append(",");
                loginSb.Append(boardInfo.DatasourceId.Trim()).Append(",");
                loginSb.Append(boardInfo.LoginType).Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                loginSb.Append("").Append(",");
                if (boardInfo.isRets)
                    loginSb.Append("NoAgentCredential").Append("\r\n");
                else
                    loginSb.Append("NonRETS").Append("\r\n");

                return;
            }

            var helper = new PsaHelper();
            var connections = helper.GetDefConnections(Int16.Parse(ModuleId));
            var paramValue = helper.GetConfigParameterValueFromDataSourceInfo(ModuleId, DataSourceId, "TCS", "TCSConditionCode", "Incremental", "REALOpenHouse");
            if (string.IsNullOrWhiteSpace(paramValue))
                paramValue = helper.GetConfigParameterValueFromDataSourceInfo(ModuleId, DataSourceId, "TCS", "TCSConditionCode", "Full", "REALOpenHouse");

            string conditionCode = paramValue == null ? "" : paramValue.ToString();
            try
            {
                foreach (var connection in connections)
                {
                    if (connection.connection_name.EndsWith("oh.sql", StringComparison.OrdinalIgnoreCase) ||
                        connection.connection_name.EndsWith("oh .sql", StringComparison.OrdinalIgnoreCase))
                    {
                        string defFilePath = connection.definition_file;
                        GetSampleDataForOpenHouses(System.IO.Path.GetFileName(defFilePath), listhubPath + ModuleId + "/data", listhubPath + ModuleId + "/log", listhubPath + ModuleId + "/result", conditionCode);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + "Failed to generate report for Module:" + ModuleId);
            }
        }

        private void LoadDefFileList()
        {
            _defFileDataCollection.Clear();
            var TFSFileDataDict = new Dictionary<string, TfsFileData>();
            
            using (var tcsDB = new TCSEntities())
            {
                tcsDB.Connection.Open();
                ObjectResult<cma_GetMLSConnection_Result> connectoinResult = tcsDB.cma_GetMLSConnection(Convert.ToInt16(boardId), "", "");
                foreach (var item in connectoinResult)
                {
                    cma_GetMLSConnection_Result item1 = item;
                    var proclass =
                        tcsDB.tcs_module_class_settings.Where(
                            cl => item1 != null && cl.connection_name == item1.connection_name)
                             .Select(cl => new { cl.@class });
                    string pClass = "";
                    try
                    {
                        pClass = proclass.First().@class;
                    }
                    catch (Exception ex1)
                    {
                        string msg = ex1.Message;
                    }
                    _defFileDataCollection.Add(new DefFileData
                    {

                        FileName = item.connection_name,
                        //FileSize = fi.Length.ToString(),
                        BoardType = item.connection_type,
                        PropertyClass = pClass,
                        //Version = item.version,
                    });

                    boardId = item.board_id.ToString();
                }
            }

            try
            {
                var di = new DirectoryInfo(WorkingDirectory + moduleId);

                foreach (var fi in di.GetFiles())
                {
                    var defFileData = _defFileDataCollection.Where(c => c.FileName.ToLower() == fi.Name.ToLower());

                    var def = new IniFile(GetDefFilePath(fi.Name));

                    var defFileDatas = defFileData as DefFileData[] ?? defFileData.ToArray();
                    DefFileData first = defFileDatas.FirstOrDefault();
                    if (first != null)
                    {
                        var firstOrDefault = defFileDatas.FirstOrDefault();
                        if (firstOrDefault != null) firstOrDefault.FileName = fi.Name;
                        var orDefault = defFileDatas.FirstOrDefault();
                        if (orDefault != null)
                            orDefault.FileSize = fi.Length.ToString();
                        var fileData = defFileDatas.FirstOrDefault();
                        if (fileData != null)
                            fileData.Version = def.Read("Version", "Common");
                        var @default = defFileDatas.FirstOrDefault();
                        if (@default != null)
                            @default.BoardType = def.Read("BoardType", "Common");
                        //_version = int.Parse(def.Read("Version", "Common"));
                    }
                    else
                    {
                        if (fi.Name.ToLower().EndsWith(".def") || fi.Name.ToLower().EndsWith(".sql"))
                        {

                            var dfd = new DefFileData();
                            dfd.FileName = fi.Name;
                            dfd.FileSize = fi.Length.ToString();
                            dfd.Version = def.Read("Version", "Common");
                            dfd.BoardType = def.Read("BoardType", "Common");
                            _defFileDataCollection.Add(dfd);
                            //_version = int.Parse(def.Read("Version", "Common"));
                        }
                    }
                }
            }
            catch (Exception)
            { }
        }

        private void GetSampleDataForSingleRoster(string defName, string sampleDataFilePath, string logPath, string resultFilePath)
        {
            try
            {
                var defPath = GetDefFilePath(defName);
                var searchEngine = new SearchEngine();
                var mlsEngine = new MLSEngine(File.ReadAllText(defPath), null);
                                
                var recordLimit = mlsEngine.getDefParser().getValue("Common.MaxRecordsLimit");
                searchEngine.IsDebug = true;
                searchEngine.BoardID = 999999;
                bool isFailed = false;
                var resultFolder = "";
                StringBuilder defDmql = new StringBuilder();
                string defDmqlString = "";
                
                resultFolder = GetTempResultFolder("prosoftroster");
                var defPathInTempResultFolder = System.IO.Path.Combine(resultFolder, defName);
                File.Copy(defPath, defPathInTempResultFolder, true);

                //newwork.GetSampleDataForSingleDef(result.DefFilePath,
                //    filePath + "_grid.dat",
                //    filePath + "_comm.log",
                //    filePath + "_tcs.xml");

                var searchRequestXml = "";
                string resourceType = "";
                var requestType = RequestXmlHelper.GetSearchRequestType(defPath);
                switch (requestType)
                {
                    case SpeedUp.Helper.RequestXmlHelper.SearchRequestType.AgentRoster:
                        searchRequestXml = RequestXmlHelper.GetRequestXmlForListhubAgentRosterCount(defPathInTempResultFolder, Login);
                        resourceType = "A";
                        break;
                    case SpeedUp.Helper.RequestXmlHelper.SearchRequestType.OfficeRoster:
                        searchRequestXml = RequestXmlHelper.GetRequestXmlForListhubOfficeRosterCount(defPathInTempResultFolder, Login);
                        resourceType = "O";
                        break;
                }

                if (string.IsNullOrWhiteSpace(searchRequestXml)) return;
  
                var result = searchEngine.RunClientRequest(searchRequestXml);
                var replyText = "Success";
                var resultCount = GetResultCount(File.ReadAllText(resultFolder + "\\tcslisting.xml"), ref replyText);

                string logText = File.ReadAllText(resultFolder + "\\communication.log");

                bool loginFailed = false;
                if (resultCount == 0 || logText.IndexOf("ListhubSearchString") == -1)
                {
                    //isFailed = true;
                    string countResult = File.ReadAllText(resultFolder + "\\tcslisting.xml");
                    if (countResult.IndexOf("60310") > -1)
                        loginFailed = true;
                    File.WriteAllText(exceptFilePath + string.Format("{0}-{1}-", resourceType, boardInfo.DatasourceId.Trim()) + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log", logText);
                }

                if (!GetParameter(logText, "ListhubSearchString").ToLower().Contains("searchtype"))
                    return;

                if (string.IsNullOrEmpty(boardInfo.DatasourceId))
                    boardInfo.DatasourceId = "";



                if (!hasAddedToLogin && logText.IndexOf("Listhub") > 0)
                {
                    loginSb.Append(boardInfo.ModuleId).Append(",");
                    loginSb.Append(boardInfo.ModuleName).Append(",");
                    loginSb.Append(boardInfo.DatasourceId.Trim()).Append(",");
                    loginSb.Append(boardInfo.LoginType).Append(",");
                    loginSb.Append(GetParameter(logText, "ListhubLoginUrl")).Append(",");
                    loginSb.Append(GetParameter(logText, "ListhubUsername")).Append(",");
                    loginSb.Append(GetParameter(logText, "ListhubPassword")).Append(",");
                    loginSb.Append(GetParameter(logText, "ListhubUserAgent")).Append(",");
                    loginSb.Append(GetParameter(logText, "ListhubUAPassword")).Append(",");
                    loginSb.Append(GetParameter(logText, "ListhubRETSVersion")).Append(",");
                    loginSb.Append(GetParameter(logText, "ListhubHttpVersion")).Append(",");
                    loginSb.Append(GetParameter(logText, "ListhubHttpMethod")).Append(",");
                    loginSb.Append(GetParameter(logText, "ListhubAuthentication")).Append(",");
                    if (loginFailed)
                        loginSb.Append("FailedLogin");
                    else
                        loginSb.Append(" ");
                    loginSb.Append("\r\n");
                    hasAddedToLogin = true;
                }
                if (!moduleSet.Contains(ModuleId + defName))
                {
                    //defDmql.Append(boardInfo.ModuleId).Append(",");
                    //defDmql.Append(boardInfo.ModuleName).Append(",");
                    //defDmql.Append(boardInfo.DatasourceId).Append(",");
                    //defDmql.Append(string.Format("{0},", resourceType));
                    //string dmqlSearch = GetParameter(logText, "ListhubSearchString");
                    //defDmql.Append(GetParameter(dmqlSearch, "searchtype", "&")).Append(",");
                    //defDmql.Append(GetParameter(dmqlSearch, "Class", "&")).Append(",");
                    //defDmql.Append(recordLimit).Append(","); //Search limit
                    //defDmql.Append(GetParameter(dmqlSearch, "Format", "&")).Append(",");
                    //defDmql.Append(Convert.ToString(resultCount)).Append(","); //Count                    
                    //defDmql.Append("\"").Append(GetParameter(dmqlSearch + "\r\n", "query", "\r\n")).Append("\"").Append(",");
                    defDmql.Append(boardInfo.ModuleId).Append(",");
                    defDmql.Append(boardInfo.ModuleName).Append(",");
                    defDmql.Append(boardInfo.DatasourceId).Append(",");
                    defDmql.Append(string.Format("{0},", resourceType));
                    string dmqlSearch = GetParameter(logText, "ListhubSearchString");
                    defDmql.Append(GetParameter(dmqlSearch, "searchtype", "&")).Append(",");
                    defDmql.Append(GetParameter(dmqlSearch, "Class", "&")).Append(",");
                    defDmql.Append(recordLimit).Append(","); //Search limit
                    defDmql.Append(GetParameter(dmqlSearch, "Format", "&")).Append(",");
                    defDmql.Append(Convert.ToString(resultCount)).Append(","); //Count records active
                    defDmql.Append("").Append(","); //Count records expired
                    defDmql.Append("").Append(","); //Count records pending
                    defDmql.Append("").Append(","); //Count records sold
                    defDmql.Append("").Append(","); //Last modified date field
                    defDmql.Append("").Append(","); //Last modified date format
                    defDmql.Append("").Append(","); //Sold Date Field
                    defDmql.Append("").Append(","); //Sold Date Format
                    defDmql.Append("").Append(","); //Expired Date Field
                    defDmql.Append("").Append(","); //Expired Date Format
                    defDmql.Append("").Append(","); //Key field
                    defDmql.Append("\"").Append(GetParameter(dmqlSearch + "\r\n", "query", "\r\n")).Append("\"").Append(",");
                }
                

                defDmqlString = defDmql.ToString();
                if (isFailed)
                {
                    dmqlSb.Append(boardInfo.ModuleId).Append(",");
                    dmqlSb.Append(boardInfo.ModuleName).Append(",");
                    dmqlSb.Append(boardInfo.DatasourceId).Append(",");
                    dmqlSb.Append(string.Format("{0},", resourceType));
                    dmqlSb.Append(resultFolder);
                }
                else
                {
                    dmqlSb.Append(defDmql);
                }
                if (!moduleSet.Contains(ModuleId + defName))
                {
                    dmqlSb.Remove(dmqlSb.Length - 1, 1).Append("\r\n");
                    moduleSet.Add(ModuleId + defName);
                }
            }
            catch (Exception e)
            {
                if (dmqlSb.Length > 0)
                {
                    if (dmqlSb.ToString(dmqlSb.Length - 1, 1).EndsWith(","))
                    {
                        dmqlSb.Remove(dmqlSb.Length - 1, 1).Append("\r\n");
                    }

                }
                Console.WriteLine(e.Message);
            }
        }

        private void GetSampleDataForSingleRDCRoster(string defName, string sampleDataFilePath, string logPath, string resultFilePath, string conditionCode, string retsQueryParameter)
        {
            try
            {
                var defPath = GetDefFilePath(defName);                
                var searchEngine = new SearchEngine();
                var mlsEngine = new MLSEngine(File.ReadAllText(defPath), null);

                var recordLimit = mlsEngine.getDefParser().getValue("Common.MaxRecordsLimit");
                searchEngine.IsDebug = true;
                searchEngine.BoardID = 999999;
                bool isFailed = false;
                var resultFolder = "";
                StringBuilder defDmql = new StringBuilder();
                string defDmqlString = "";

                resultFolder = GetTempResultFolder("rdcroster");
                var defPathInTempResultFolder = System.IO.Path.Combine(resultFolder, defName);
                File.Copy(defPath, defPathInTempResultFolder, true);

                //newwork.GetSampleDataForSingleDef(result.DefFilePath,
                //    filePath + "_grid.dat",
                //    filePath + "_comm.log",
                //    filePath + "_tcs.xml");

                var searchRequestXml = "";
                string resourceType = "";
                var requestType = RequestXmlHelper.GetSearchRequestType(defPath);
                switch (requestType)
                {
                    case SpeedUp.Helper.RequestXmlHelper.SearchRequestType.AgentRoster:
                        searchRequestXml = RequestXmlHelper.GetRequestXmlForListhubAgentRosterCount(defPathInTempResultFolder, Login, conditionCode, retsQueryParameter);
                        resourceType = "A";
                        break;
                    case SpeedUp.Helper.RequestXmlHelper.SearchRequestType.OfficeRoster:
                        searchRequestXml = RequestXmlHelper.GetRequestXmlForListhubOfficeRosterCount(defPathInTempResultFolder, Login, conditionCode, retsQueryParameter);
                        resourceType = "O";
                        break;
                }

                if (string.IsNullOrWhiteSpace(searchRequestXml)) return;

                var result = searchEngine.RunClientRequest(searchRequestXml);
                var replyText = "Success";
                var resultCount = GetResultCount(File.ReadAllText(resultFolder + "\\tcslisting.xml"), ref replyText);

                string logText = File.ReadAllText(resultFolder + "\\communication.log");

                bool loginFailed = false;
                if (resultCount == 0 || logText.IndexOf("ListhubSearchString") == -1)
                {
                    //isFailed = true;
                    string countResult = File.ReadAllText(resultFolder + "\\tcslisting.xml");
                    if (countResult.IndexOf("60310") > -1)
                        loginFailed = true;
                    File.WriteAllText(exceptFilePath + string.Format("{0}-{1}-", resourceType, boardInfo.DatasourceId.Trim()) + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log", logText);
                }


                if (!GetParameter(logText, "ListhubSearchString").ToLower().Contains("searchtype"))
                    return;

                if (string.IsNullOrEmpty(boardInfo.DatasourceId))
                    boardInfo.DatasourceId = "";



                if (!hasAddedToLogin && logText.IndexOf("Listhub") > 0)
                {
                    loginSb.Append(boardInfo.ModuleId).Append(",");
                    loginSb.Append(boardInfo.ModuleName).Append(",");
                    loginSb.Append(boardInfo.DatasourceId.Trim()).Append(",");
                    loginSb.Append(boardInfo.LoginType).Append(",");                    
                    loginSb.Append(GetParameter(logText, "ListhubLoginUrl")).Append(",");
                    loginSb.Append(GetParameter(logText, "ListhubUsername")).Append(",");
                    loginSb.Append(GetParameter(logText, "ListhubPassword")).Append(",");
                    loginSb.Append(GetParameter(logText, "ListhubUserAgent")).Append(",");
                    loginSb.Append(GetParameter(logText, "ListhubUAPassword")).Append(",");
                    loginSb.Append(GetParameter(logText, "ListhubRETSVersion")).Append(",");
                    loginSb.Append(GetParameter(logText, "ListhubHttpVersion")).Append(",");
                    loginSb.Append(GetParameter(logText, "ListhubHttpMethod")).Append(",");
                    loginSb.Append(GetParameter(logText, "ListhubAuthentication")).Append(",");                    
                    
                    if (loginFailed)
                        loginSb.Append("FailedLogin");
                    else
                        loginSb.Append(" ");
                    loginSb.Append("\r\n");
                    hasAddedToLogin = true;
                }
                if (!moduleSet.Contains(DataSourceId + defName))
                {
                    //defDmql.Append(boardInfo.ModuleId).Append(",");
                    //defDmql.Append(boardInfo.ModuleName).Append(",");
                    //defDmql.Append(boardInfo.DatasourceId).Append(",");
                    //defDmql.Append(string.Format("{0},", resourceType));
                    //string dmqlSearch = GetParameter(logText, "ListhubSearchString");
                    //defDmql.Append(GetParameter(dmqlSearch, "searchtype", "&")).Append(",");
                    //defDmql.Append(GetParameter(dmqlSearch, "Class", "&")).Append(",");
                    //defDmql.Append(recordLimit).Append(","); //Search limit
                    //defDmql.Append(GetParameter(dmqlSearch, "Format", "&")).Append(",");
                    //defDmql.Append(Convert.ToString(resultCount)).Append(","); //Count                    
                    //defDmql.Append("\"").Append(GetParameter(dmqlSearch + "\r\n", "query", "\r\n")).Append("\"").Append(",");
                    defDmql.Append(boardInfo.ModuleId).Append(",");
                    defDmql.Append(boardInfo.ModuleName).Append(",");
                    defDmql.Append(boardInfo.DatasourceId).Append(",");
                    defDmql.Append(string.Format("{0},", resourceType));
                    string dmqlSearch = GetParameter(logText, "ListhubSearchString");
                    defDmql.Append(GetParameter(dmqlSearch, "searchtype", "&")).Append(",");
                    defDmql.Append(GetParameter(dmqlSearch, "Class", "&")).Append(",");
                    defDmql.Append(recordLimit).Append(","); //Search limit
                    defDmql.Append(GetParameter(dmqlSearch, "Format", "&")).Append(",");
                    defDmql.Append(Convert.ToString(resultCount)).Append(","); //Count
                    defDmql.Append("").Append(",");
                    defDmql.Append("").Append(",");                    
                    defDmql.Append("").Append(","); //Last modified field                    
                    defDmql.Append("").Append(",");
                    defDmql.Append("").Append(","); //Key field
                    defDmql.Append("\"").Append(GetParameter(dmqlSearch + "\r\n", "query", "\r\n")).Append("\"").Append(",");                    
                }


                defDmqlString = defDmql.ToString();
                if (isFailed)
                {
                    dmqlSb.Append(boardInfo.ModuleId).Append(",");
                    dmqlSb.Append(boardInfo.ModuleName).Append(",");
                    dmqlSb.Append(boardInfo.DatasourceId).Append(",");
                    dmqlSb.Append(string.Format("{0},", resourceType));
                    dmqlSb.Append(resultFolder);
                }
                else
                {
                    dmqlSb.Append(defDmql);
                }
                if (!moduleSet.Contains(DataSourceId + defName))
                {
                    dmqlSb.Remove(dmqlSb.Length - 1, 1).Append("\r\n");
                    moduleSet.Add(DataSourceId + defName);
                }
            }
            catch (Exception e)
            {
                if (dmqlSb.Length > 0)
                {
                    if (dmqlSb.ToString(dmqlSb.Length - 1, 1).EndsWith(","))
                    {
                        dmqlSb.Remove(dmqlSb.Length - 1, 1).Append("\r\n");
                    }

                }
                Console.WriteLine(e.Message);
            }
        }

        private void GetSampleDataForOpenHouses(string defName, string sampleDataFilePath, string logPath, string resultFilePath, string conditionCode)
        {
            try
            {

                var defPath = GetDefFilePath(defName);
                var mlsEngine = new MLSEngine(File.ReadAllText(defPath), null);
                bool isFailed = false;

                var resultFolder = GetTempResultFolder("rdcopenhouse");
                var openHouseClasses = mlsEngine.getDefParser().getValue("TcpIp.OpenHouseClasses");
                StringBuilder defDmql = new StringBuilder();

                if (!string.IsNullOrEmpty(openHouseClasses))
                {
                    string[] classes = openHouseClasses.Split(',');
                    for (int i = 0; i < classes.Length; i++)
                    {
                        mlsEngine.CurrentOpenHouseClass = classes[i];
                        GetSampleDataForSingleOpenHouse(defName, resultFolder, mlsEngine, ref defDmql, conditionCode);
                    }
                }
                else
                {
                    GetSampleDataForSingleOpenHouse(defName, resultFolder, mlsEngine, ref defDmql, conditionCode);
                }

                if (isFailed)
                {
                    dmqlSb.Append(boardInfo.ModuleId).Append(",");
                    dmqlSb.Append(boardInfo.ModuleName).Append(",");
                    dmqlSb.Append(boardInfo.DatasourceId).Append(",");
                    dmqlSb.Append("OH,");
                    dmqlSb.Append(resultFolder);
                }
                else
                {
                    dmqlSb.Append(defDmql);
                }
                if (!moduleSet.Contains(DataSourceId + defName))
                {
                    //dmqlSb.Remove(dmqlSb.Length - 1, 1);
                    moduleSet.Add(DataSourceId + defName);
                }
            }
            catch (Exception e)
            {
                if (dmqlSb.Length > 0)
                {
                    if (dmqlSb.ToString(dmqlSb.Length - 1, 1).EndsWith(","))
                    {
                        dmqlSb.Remove(dmqlSb.Length - 1, 1).Append("\r\n");
                    }

                }
                Console.WriteLine(e.Message);
            }
        }

        private string CalculateCountsByClassForOpenHouses(string dataSourceId, string searchType, string retsClass, string searchQuery, string conditionCode)
        {
            LoginInfoExtra loginInfoExtra = loginDict[dataSourceId.Trim()];
            string defPath = PrepareTransaction(loginInfoExtra, string.Format("?SearchType={0}&Class={1}&Limit=300&QueryType=DMQL2&Format=COMPACT-DECODED&Query={2}", searchType, retsClass, searchQuery));

            var searchEngine = new SearchEngine();
            var mlsEngine = new MLSEngine(File.ReadAllText(defPath), null);

            var recordLimit = mlsEngine.getDefParser().getValue("Common.MaxRecordsLimit");
            searchEngine.IsDebug = true;
            searchEngine.BoardID = 999999;
                       
            var destDefPath = defPath.Replace(".def", "oh.sql");
            File.Move(defPath, destDefPath);
            var searchRequestXml = RequestXmlHelper.GetRequestXmlForListhubOpenHouseCount(destDefPath, Login, conditionCode);

            var result = searchEngine.RunClientRequest(searchRequestXml);

            var destFolder = Directory.GetParent(destDefPath);
            var replyText = "Success";
            var resultCount = GetResultCount(File.ReadAllText(destFolder + "\\tcslisting.xml"), ref replyText);

            string logText = File.ReadAllText(destFolder + "\\communication.log");

            bool loginFailed = false;
            if (resultCount == 0 || logText.IndexOf("ListhubSearchString") == -1)
            {
                string countResult = File.ReadAllText(destFolder + "\\tcslisting.xml");
                if (countResult.IndexOf("60310") > -1)
                    loginFailed = true;

                File.WriteAllText(exceptFilePath + string.Format("OH-{0}-", dataSourceId.Trim()) + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log", logText);

            }
            return (!loginFailed) ? resultCount.ToString() : "0";            
        }

        private void GetSampleDataForSingleOpenHouse(string defName, string resultFolder, MLSEngine mlsEngine, ref StringBuilder defDmql, string conditionCode)
        {
            
            var defPath = GetDefFilePath(defName);
            var searchEngine = new SearchEngine();
            var recordLimit = mlsEngine.getDefParser().getValue("Common.MaxRecordsLimit");
            searchEngine.IsDebug = true;
            searchEngine.BoardID = 999999;
            
            //string defDmqlString = "";
            
            string retsClass = mlsEngine.CurrentOpenHouseClass;

            var defPathInTempResultFolder = System.IO.Path.Combine(resultFolder, defName);
            File.Copy(defPath, defPathInTempResultFolder, true);

            var searchRequestXml = RequestXmlHelper.GetRequestXmlForListhubOpenHouseCount(defPathInTempResultFolder, Login, conditionCode);

            if (string.IsNullOrWhiteSpace(searchRequestXml)) return;

            var result = searchEngine.RunClientRequest(searchRequestXml);
            var replyText = "Success";
            var resultCount = GetResultCount(File.ReadAllText(resultFolder + "\\tcslisting.xml"), ref replyText);

            string logText = File.ReadAllText(resultFolder + "\\communication.log");

            string dmqlSearch = GetParameter(logText, "ListhubSearchString");
            string dmqlSearchDecoded = GetParameter(logText + "\r\n", "ListhubSearchString", "\r\n", false);
            string searchType = GetParameter(dmqlSearch, "searchtype", "&");

            if (string.IsNullOrWhiteSpace(searchType) || searchType.ToLower().Trim().Equals("property")) return;


            var searchDateFormat = "";             
            try
            {
                searchDateFormat = getDateFormatType(defPath, "ST_SearchDate");
            }
            catch (Exception ex1)
            {
            }

            var w3cDataFormat = searchDateFormat.ToLower().Trim().Equals("yyyy-mm-ddthh:mm:ss") ? 
                @"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}" : searchDateFormat.ToLower().Trim().Equals("yyyy-mm-dd") ? 
                @"\d{4}-\d{2}-\d{2}" : @"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}.\d{3}";
            bool loginFailed = false;
            if (resultCount == 0 || logText.IndexOf("ListhubSearchString") == -1)
            {
                //isFailed = true;
                string countResult = File.ReadAllText(resultFolder + "\\tcslisting.xml");
                if (countResult.IndexOf("60310") > -1)
                    loginFailed = true;
                File.WriteAllText(exceptFilePath + string.Format("OH-{0}-", boardInfo.DatasourceId.Trim()) + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log", logText);
            }
                       

            if (string.IsNullOrEmpty(boardInfo.DatasourceId))
                boardInfo.DatasourceId = "";

            

            if (!hasAddedToLogin && logText.IndexOf("Listhub") > 0)
            {
                LoginInfoExtra loginInfoExtra = new LoginInfoExtra();
                loginInfoExtra.ByPassAuthentication = "1";
                loginInfoExtra.UaPassword = GetParameter(logText, "ListhubUAPassword");
                loginInfoExtra.UserAgent = GetParameter(logText, "ListhubUserAgent");
                loginInfoExtra.Username = GetParameter(logText, "ListhubUsername");
                loginInfoExtra.Password = GetParameter(logText, "ListhubPassword");
                loginInfoExtra.RetsUrl = GetParameter(logText, "ListhubLoginUrl");
                loginInfoExtra.RetsVersion = GetParameter(logText, "ListhubRETSVersion");
                loginInfoExtra.HttpUserAgent = GetParameter(logText, "ListhubUserAgent");
                loginInfoExtra.HttpMethod = GetParameter(logText, "ListhubHttpMethod");
                loginInfoExtra.HttpVersion = GetParameter(logText, "ListhubHttpVersion");
                loginDict.Add(boardInfo.DatasourceId.Trim(), loginInfoExtra);

                loginSb.Append(boardInfo.ModuleId).Append(",");
                loginSb.Append(boardInfo.ModuleName).Append(",");
                loginSb.Append(boardInfo.DatasourceId.Trim()).Append(",");
                loginSb.Append(boardInfo.LoginType).Append(",");
                loginSb.Append(loginInfoExtra.RetsUrl).Append(",");
                loginSb.Append(loginInfoExtra.Username).Append(",");
                loginSb.Append(loginInfoExtra.Password).Append(",");
                loginSb.Append(loginInfoExtra.UserAgent).Append(",");
                loginSb.Append(loginInfoExtra.UaPassword).Append(",");
                loginSb.Append(loginInfoExtra.RetsVersion).Append(",");
                loginSb.Append(loginInfoExtra.HttpVersion).Append(",");
                loginSb.Append(loginInfoExtra.HttpMethod).Append(",");
                loginSb.Append(GetParameter(logText, "ListhubAuthentication")).Append(",");
                
                if (loginFailed)
                    loginSb.Append("FailedLogin");
                else
                    loginSb.Append(" ");
                loginSb.Append("\r\n");
                hasAddedToLogin = true;
            }
            if (!moduleSet.Contains(DataSourceId + defName))
            {
                Dictionary<string, string> dictExtraInfo = new Dictionary<string, string>();
                dictExtraInfo.Add("dmql_query", "");
                dictExtraInfo.Add("start_datetime_field_name", "");
                dictExtraInfo.Add("start_datetime_field_format_low", searchDateFormat);
                dictExtraInfo.Add("start_datetime_field_format_delimiter", "-");
                dictExtraInfo.Add("start_datetime_field_format_high", searchDateFormat);
                dictExtraInfo.Add("end_datetime_field_name", "");
                dictExtraInfo.Add("end_datetime_field_format_low", searchDateFormat);
                dictExtraInfo.Add("end_datetime_field_format_delimiter", "-");
                dictExtraInfo.Add("end_datetime_field_format_high", searchDateFormat);
                dictExtraInfo.Add("datetime_field_operation", " ");


                defDmql.Append(boardInfo.ModuleId).Append(",");
                defDmql.Append(boardInfo.ModuleName).Append(",");
                defDmql.Append(boardInfo.DatasourceId).Append(",");
                defDmql.Append("OH,");                
                defDmql.Append(searchType).Append(",");
                defDmql.Append(!string.IsNullOrWhiteSpace(retsClass) ? retsClass : GetParameter(dmqlSearch, "Class", "&")).Append(",");
                defDmql.Append(recordLimit).Append(","); //Search limit
                defDmql.Append(GetParameter(dmqlSearch, "Format", "&")).Append(",");
                defDmql.Append(!string.IsNullOrWhiteSpace(retsClass) ? CalculateCountsByClassForOpenHouses(boardInfo.DatasourceId.Trim(), searchType, retsClass, GetParameter(dmqlSearchDecoded + "\r\n", "query", "\r\n", false), conditionCode) : Convert.ToString(resultCount)).Append(","); //Count
                defDmql.Append("").Append(","); //key field
                
                string searchQuery = GetParameter(dmqlSearch + "\r\n", "query", "\r\n");
                SplitTextResults(searchQuery, w3cDataFormat, ref dictExtraInfo);

                // clean up
                if (string.IsNullOrWhiteSpace(dictExtraInfo["start_datetime_field_name"]))
                {
                    dictExtraInfo["start_datetime_field_format_low"] = "";
                    dictExtraInfo["start_datetime_field_format_delimiter"] = "";
                    dictExtraInfo["start_datetime_field_format_high"] = "";
                }

                if (string.IsNullOrWhiteSpace(dictExtraInfo["end_datetime_field_name"]))
                {
                    dictExtraInfo["end_datetime_field_format_low"] = "";
                    dictExtraInfo["end_datetime_field_format_delimiter"] = "";
                    dictExtraInfo["end_datetime_field_format_high"] = "";
                }

                defDmql.Append("\"").Append(dictExtraInfo["dmql_query"]).Append("\"").Append(","); //dmql_query
                defDmql.Append(dictExtraInfo["start_datetime_field_name"]).Append(","); //start_datetime_field_name
                defDmql.Append(dictExtraInfo["start_datetime_field_format_low"]).Append(","); //start_datetime_field_format_low
                defDmql.Append(dictExtraInfo["start_datetime_field_format_delimiter"]).Append(","); //start_datetime_field_format_delimiter
                defDmql.Append(dictExtraInfo["start_datetime_field_format_high"]).Append(","); //start_datetime_field_format_high
                defDmql.Append(dictExtraInfo["end_datetime_field_name"]).Append(","); //end_datetime_field_name
                defDmql.Append(dictExtraInfo["end_datetime_field_format_low"]).Append(","); //end_datetime_field_format_low
                defDmql.Append(dictExtraInfo["end_datetime_field_format_delimiter"]).Append(","); //end_datetime_field_format_delimiter
                defDmql.Append(dictExtraInfo["end_datetime_field_format_high"]).Append(","); //end_datetime_field_format_high
                defDmql.Append(dictExtraInfo["datetime_field_operation"]).Append(","); //datetime_field_operation               
                defDmql.Append("\"").Append(dmqlSearchDecoded).Append("\"").Append("\r\n"); //raw dmql query                
            }
            
            //defDmqlString = defDmql.ToString();
            
        }

        private void GetMasterAccountForSingleDef(string defName, ref LoginInfo loginInfo)
        {
            try
            {
                var defPath = GetDefFilePath(defName);
                if (!IsProSoftDefFileAvailable(defPath))
                    return;
                var searchEngine = new SearchEngine();
                var mlsEngine = new MLSEngine(File.ReadAllText(defPath), null);
                
                var recordLimit = mlsEngine.getDefParser().getValue("Common.MaxRecordsLimit");
                searchEngine.IsDebug = true;
                searchEngine.BoardID = 999999;                
                var resultFolder = "";
                StringBuilder defDmql = new StringBuilder();
                
                resultFolder = GetTempResultFolder();
                var defPathInTempResultFolder = System.IO.Path.Combine(resultFolder, defName);
                File.Copy(defPath, defPathInTempResultFolder, true);
                
                var searchRequestXml = RequestXmlHelper.GetRequestXmlForListhubCount(defPathInTempResultFolder, Login, "A");

                string logText = "";
                bool loginFailed = false;
                int resultCount = 0;
                string result = "";
                int count = -1;
                while (++count < 10)
                {
                    result = searchEngine.RunClientRequest(searchRequestXml);

                    //if (!string.IsNullOrEmpty(rPath))
                    //    File.Copy(resultFolder + "\\tcslisting.xml", rPath + "\\tcslisting.xml", true);

                    var replyText = "Success";
                    resultCount = GetResultCount(File.ReadAllText(resultFolder + "\\tcslisting.xml"), ref replyText);

                    logText = File.ReadAllText(resultFolder + "\\communication.log");

                    if (resultCount == 0 || logText.IndexOf("ListhubSearchString") == -1)
                    {
                        
                        string countResult = File.ReadAllText(resultFolder + "\\tcslisting.xml");
                        if (countResult.IndexOf("60310") > -1)
                            loginFailed = true;
                        File.WriteAllText(exceptFilePath + string.Format("L-{0}-", boardInfo.DatasourceId.Trim()) + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log", logText);
                        if (logText.IndexOf(exceptional_error_messages[0]) > -1 || logText.IndexOf(exceptional_error_messages[1]) > -1)
                        {
                            Thread.Sleep(300);
                            loginFailed = false;
                            continue;
                        }
                        
                    }
                    break;
                }
                 
                if (logText.IndexOf("Listhub") > 0)
                {
                    loginInfo.UaPassword = GetParameter(logText, "ListhubUAPassword");
                    loginInfo.UserAgent = GetParameter(logText, "ListhubUserAgent");
                    loginInfo.UserName = GetParameter(logText, "ListhubUsername");
                    loginInfo.Password =GetParameter(logText, "ListhubPassword");                    
                }
                 
            }
            catch (Exception e)
            {                
                Console.WriteLine(e.Message);
            }
        }

        private void GetSampleDataForSingleDef(string defName, string sampleDataFilePath, string logPath, string resultFilePath)
        {
            try
            {
                var defPath = GetDefFilePath(defName);
                if (!IsProSoftDefFileAvailable(defPath))
                    return;
                var searchEngine = new SearchEngine();
                var mlsEngine = new MLSEngine(File.ReadAllText(defPath), null);
               
                var picScriptContent = mlsEngine.getDefParser().getPicScriptContent();
                bool photoCollectedYN = !string.IsNullOrWhiteSpace(picScriptContent);
                var photoType = GetExactParameter(picScriptContent + "\r\n", "type", "\"\r\n");
                if (string.IsNullOrWhiteSpace(photoType)) photoType = GetExactFirstParameter(picScriptContent + "\r\n", "type", "\"\r\n"); 
                if (photoType.IndexOf("&") != -1) photoType = photoType.Remove(photoType.IndexOf("&"));
 
                var listingKeyField = mlsEngine.getCmaFields().getField("RecordID").getRecordPosition().Replace("\"", "");
                var lastModifiedField = "";
                try
                {
                    lastModifiedField = mlsEngine.getCmaFields().getField("STDFLastMod").getRecordPosition().Replace("\"", "");
                }
                catch (Exception ex1)
                {
                }
                var lastModifiedDateFormat = "MM/dd/yyyyTHH:mm:ss";
                try
                {
                    lastModifiedDateFormat = getDateFormatType(defPath, "ST_LastMod");
                }
                catch (Exception ex1)
                {
                }
                
                var statusDateField = "";
                try
                {
                    statusDateField = mlsEngine.getCmaFields().getField("STDFStatusDate").getRecordPosition().Replace("\"", "");
                }
                catch (Exception ex1)
                {
                }

                var statusDateFormat = "";
                try
                {
                    statusDateFormat = getDateFormatType(defPath, "ST_StatusDate", "YYYY-MM-DD");
                }
                catch (Exception ex1)
                {
                }

                var saleDateField = "";
                try
                {
                    saleDateField = mlsEngine.getCmaFields().getField("CMASaleDate").getRecordPosition().Replace("\"", "");
                }
                catch (Exception ex1)
                {
                }
                var saleDateFormat = "";
                try
                {
                    saleDateFormat = getDateFormatType(defPath, "ST_SaleDate", "YYYY-MM-DD");
                }
                catch (Exception ex1)
                {
                }

                var photoLastModifiedField = "";
                try
                {
                    photoLastModifiedField = mlsEngine.getCmaFields().getField("STDFPicMod").getRecordPosition().Replace("\"", "");
                }
                catch (Exception ex1)
                {
                }
                var photoLastModifiedDateFormat = "MM/dd/yyyyTHH:mm:ss";
                try
                {
                    photoLastModifiedDateFormat = !string.IsNullOrWhiteSpace(photoLastModifiedField)? getDateFormatType(defPath, "ST_PicMod") : "";
                }
                catch (Exception ex1)
                {
                }
                
                

                var recordLimit = mlsEngine.getDefParser().getValue("Common.MaxRecordsLimit");
                searchEngine.IsDebug = true;
                searchEngine.BoardID = 999999;
                bool isFailed = false;
                var resultFolder = "";
                StringBuilder defDmql = new StringBuilder();                
                for (int i = 0; i < status.Length; i++)
                {

                    resultFolder = GetTempResultFolder();
                    var defPathInTempResultFolder = System.IO.Path.Combine(resultFolder, defName);
                    File.Copy(defPath, defPathInTempResultFolder, true);

                    
                    var searchRequestXml = RequestXmlHelper.GetRequestXmlForListhubCount(defPathInTempResultFolder, Login, status[i]);
                    
                    string logText = "";
                    bool loginFailed = false;
                    int resultCount = 0;
                    string result = "";
                    int count = -1;
                    while (++count < 10)
                    {
                        result = searchEngine.RunClientRequest(searchRequestXml);

                        //if (!string.IsNullOrEmpty(rPath))
                        //    File.Copy(resultFolder + "\\tcslisting.xml", rPath + "\\tcslisting.xml", true);

                        var replyText = "Success";
                        resultCount = GetResultCount(File.ReadAllText(resultFolder + "\\tcslisting.xml"),ref replyText);

                        logText = File.ReadAllText(resultFolder + "\\communication.log");
                        
                        if (resultCount == 0 || logText.IndexOf("ListhubSearchString") == -1)
                        {
                            if (status[i] == "A")
                            {
                                string countResult = File.ReadAllText(resultFolder + "\\tcslisting.xml");
                                if (countResult.IndexOf("60310") > -1)
                                    loginFailed = true;
                                File.WriteAllText(exceptFilePath + string.Format("L-{0}-", boardInfo.DatasourceId.Trim()) + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log", logText);
                                if (logText.IndexOf(exceptional_error_messages[0]) > -1 || logText.IndexOf(exceptional_error_messages[1]) > -1)
                                {
                                    Thread.Sleep(300);
                                    loginFailed = false;
                                    continue;
                                }                            
                            }
                        }
                        break;
                    }

                    if (!GetParameter(logText, "ListhubSearchString").ToLower().Contains("searchtype"))
                        return;
                    //if (resultCount == 0)
                    //{
                    //    badMoudleSb.Append(ModuleId);
                    //    if (status[i] == "A")
                    //    {
                    //        isFailed = true;
                    //        break;
                    //    }
                    //    else
                    //    {
                    //        if (!moduleSet.Contains(ModuleId + defName))
                    //        {
                    //            dmqlSb.Append("\"").Append(resultFolder).Append("\"").Append(",");
                    //        }
                    //    }
                    //    continue;
                    //}

                    if (string.IsNullOrEmpty(boardInfo.DatasourceId))
                        boardInfo.DatasourceId = "";

                    
                    if (status[i].Equals("A"))
                    {
                        if (!hasAddedToLogin && logText.IndexOf("Listhub") > 0)
                        {
                            loginSb.Append(boardInfo.ModuleId).Append(",");
                            loginSb.Append(boardInfo.ModuleName).Append(",");
                            loginSb.Append(boardInfo.DatasourceId.Trim()).Append(",");
                            loginSb.Append(boardInfo.LoginType).Append(",");
                            loginSb.Append(GetParameter(logText, "ListhubLoginUrl")).Append(",");
                            loginSb.Append(GetParameter(logText, "ListhubUsername")).Append(",");
                            loginSb.Append(GetParameter(logText, "ListhubPassword")).Append(",");
                            loginSb.Append(GetParameter(logText, "ListhubUserAgent")).Append(",");
                            loginSb.Append(GetParameter(logText, "ListhubUAPassword")).Append(",");
                            loginSb.Append(GetParameter(logText, "ListhubRETSVersion")).Append(",");
                            loginSb.Append(GetParameter(logText, "ListhubHttpVersion")).Append(",");
                            loginSb.Append(GetParameter(logText, "ListhubHttpMethod")).Append(",");
                            loginSb.Append(GetParameter(logText, "ListhubAuthentication")).Append(",");
                            if (loginFailed)
                                loginSb.Append("FailedLogin");
                            else
                                loginSb.Append(" ");
                            loginSb.Append("\r\n");
                            hasAddedToLogin = true;
                        }
                        if (!moduleSet.Contains(ModuleId + defName))
                        {
                            defDmql.Append(boardInfo.ModuleId).Append(",");
                            defDmql.Append(boardInfo.ModuleName).Append(",");
                            defDmql.Append(boardInfo.DatasourceId).Append(",");
                            defDmql.Append("L,");
                            string dmqlSearch = GetParameter(logText, "ListhubSearchString");
                            defDmql.Append(GetParameter(dmqlSearch, "searchtype", "&")).Append(",");
                            defDmql.Append(GetFirstParameter(dmqlSearch, "Class", "&")).Append(",");
                            defDmql.Append(recordLimit).Append(","); //Search limit
                            defDmql.Append(GetParameter(dmqlSearch, "Format", "&")).Append(",");
                            defDmql.Append(Convert.ToString(resultCount)).Append(","); //Count
                            defDmql.Append("count_records_expired").Append(",");
                            defDmql.Append("count_records_pending").Append(",");
                            defDmql.Append("count_records_sold").Append(",");
                            defDmql.Append(lastModifiedField).Append(","); //Last modified field
                            defDmql.Append(lastModifiedDateFormat).Append(","); //Last modified date format
                            defDmql.Append("sold_date_field").Append(",");
                            defDmql.Append("sold_date_format").Append(","); //sold date format
                            defDmql.Append("expired_date_field").Append(",");
                            defDmql.Append("expired_date_format").Append(","); //expired date format
                            defDmql.Append(listingKeyField).Append(","); //Key field
                            defDmql.Append("\"").Append(GetParameter(dmqlSearch + "\r\n", "query", "\r\n")).Append("\"").Append(","); // status active dmql query                             
                        }
                    }
                    else if(status[i].Equals("E"))
                    {
                        if (!moduleSet.Contains(ModuleId + defName))
                        {                            
                            string dmqlSearch = GetParameter(logText, "ListhubSearchString");
                            string dmqlQuery = GetParameter(dmqlSearch + "\r\n", "query", "\r\n");
                            if (string.IsNullOrWhiteSpace(dmqlQuery)) dmqlQuery = GetFirstParameter(dmqlSearch + "\r\n", "query", "\r\n");
                            defDmql.Append("\"").Append(dmqlQuery).Append("\"").Append(",");

                            resultFolder = GetTempResultFolder();
                            defPathInTempResultFolder = System.IO.Path.Combine(resultFolder, defName);
                            File.Copy(defPath, defPathInTempResultFolder, true);

                            searchRequestXml = RequestXmlHelper.GetRequestXmlForListhubCountExpired(defPathInTempResultFolder, Login, status[i]);
                            result = searchEngine.RunClientRequest(searchRequestXml);
                            var replyText = "Success";
                            resultCount = GetResultCount(File.ReadAllText(resultFolder + "\\tcslisting.xml"), ref replyText);
                            defDmql = defDmql.Replace("count_records_expired", Convert.ToString(resultCount));
                            logText = File.ReadAllText(resultFolder + "\\communication.log");
                            string dmqlSearchWithDate = GetParameter(logText, "ListhubSearchString");
                            string dmqlQueryExpired = GetParameter(dmqlSearchWithDate + "\r\n", "query", "\r\n");
                            string expiredDate = string.IsNullOrWhiteSpace(dmqlQuery) ? dmqlQueryExpired: dmqlQueryExpired.Replace(dmqlQuery, "");
                            if (!string.IsNullOrWhiteSpace(expiredDate))
                            {
                                if (!string.IsNullOrWhiteSpace(statusDateField)  && expiredDate.Contains(statusDateField))
                                    expiredDate = statusDateField;
                                else
                                    expiredDate = GetDateParameter(expiredDate.Replace("(", "(@"), DateTime.Now.Year.ToString(), "(", ")", "=");                                
                                defDmql = defDmql.Replace("expired_date_format", statusDateFormat);
                            } 
                            else
                                defDmql = defDmql.Replace("expired_date_format", "");
                            if (!logText.Contains(string.Format("({0}) is not searchable", expiredDate)) && replyText.Equals("Success"))
                                defDmql = defDmql.Replace("expired_date_field", expiredDate);
                            else
                            {
                                defDmql = defDmql.Replace("expired_date_field", "");
                                defDmql = defDmql.Replace("expired_date_format", "");
                            }

                        }
                    }
                    else if (status[i].Equals("P"))
                    {
                        if (!moduleSet.Contains(ModuleId + defName))
                        {
                            string dmqlSearch = GetParameter(logText, "ListhubSearchString");
                            string dmqlQuery = GetParameter(dmqlSearch + "\r\n", "query", "\r\n");
                            if (string.IsNullOrWhiteSpace(dmqlQuery)) dmqlQuery = GetFirstParameter(dmqlSearch + "\r\n", "query", "\r\n");
                            defDmql.Append("\"").Append(dmqlQuery).Append("\"").Append(",");
                            defDmql = defDmql.Replace("count_records_pending", Convert.ToString(resultCount));
                        }
                    }
                    else if (status[i].Equals("S"))
                    {
                        if (!moduleSet.Contains(ModuleId + defName))
                        {
                            string dmqlSearch = GetParameter(logText, "ListhubSearchString");
                            string dmqlQuery = GetParameter(dmqlSearch + "\r\n", "query", "\r\n");
                            if (string.IsNullOrWhiteSpace(dmqlQuery)) dmqlQuery = GetFirstParameter(dmqlSearch + "\r\n", "query", "\r\n");
                            defDmql.Append("\"").Append(dmqlQuery).Append("\"").Append(",");

                            resultFolder = GetTempResultFolder();
                            defPathInTempResultFolder = System.IO.Path.Combine(resultFolder, defName);
                            File.Copy(defPath, defPathInTempResultFolder, true);

                            searchRequestXml = RequestXmlHelper.GetRequestXmlForListhubCountSold(defPathInTempResultFolder, Login, status[i]);
                            result = searchEngine.RunClientRequest(searchRequestXml);
                            var replyText = "Success";
                            resultCount = GetResultCount(File.ReadAllText(resultFolder + "\\tcslisting.xml"), ref replyText);
                            defDmql = defDmql.Replace("count_records_sold", Convert.ToString(resultCount));
                            logText = File.ReadAllText(resultFolder + "\\communication.log");
                            string dmqlSearchSoldWithDate = GetParameter(logText, "ListhubSearchString");
                            string dmqlQueryWithDate = GetParameter(dmqlSearchSoldWithDate + "\r\n", "query", "\r\n");
                            string expiredDate = string.IsNullOrWhiteSpace(dmqlQuery) ? dmqlQueryWithDate : dmqlQueryWithDate.Replace(dmqlQuery, "");
                            if (!string.IsNullOrWhiteSpace(expiredDate))
                            {                                
                                if (!string.IsNullOrWhiteSpace(saleDateField) && expiredDate.Contains(saleDateField))
                                    expiredDate = saleDateField;
                                else
                                    expiredDate = GetDateParameter(expiredDate.Replace("(", "(@"), DateTime.Now.Year.ToString(), "(", ")", "=");
                                defDmql = defDmql.Replace("sold_date_format", saleDateFormat);
                            } 
                            else
                                defDmql = defDmql.Replace("sold_date_format", "");
                            
                            if (!logText.Contains(string.Format("({0}) is not searchable", expiredDate)) && replyText.Equals("Success"))
                                defDmql = defDmql.Replace("sold_date_field", expiredDate);
                            else
                            {
                                defDmql = defDmql.Replace("sold_date_field", "");
                                defDmql = defDmql.Replace("sold_date_format", "");
                            }
                        }
                    }
                    
                }
                
                defDmql.Append(photoCollectedYN == true ? "y" : "n").Append(","); //photo_collected_yn
                defDmql.Append(photoType).Append(","); //photo_type
                defDmql.Append("").Append(","); //photo_count_field
                defDmql.Append(photoLastModifiedField).Append(","); //photo_last_modified_field
                defDmql.Append(photoLastModifiedDateFormat).Append(","); //photo_last_modified_format                            

                if (isFailed)
                {
                    dmqlSb.Append(boardInfo.ModuleId).Append(",");
                    dmqlSb.Append(boardInfo.ModuleName).Append(",");
                    dmqlSb.Append(boardInfo.DatasourceId).Append(",");
                    dmqlSb.Append("L,");
                    dmqlSb.Append(resultFolder);
                }
                else
                {
                    dmqlSb.Append(defDmql);
                }
                if (!moduleSet.Contains(ModuleId + defName))
                {
                    dmqlSb.Remove(dmqlSb.Length - 1, 1).Append("\r\n");
                    moduleSet.Add(ModuleId + defName);
                }
            }
            catch (Exception e)
            {
                if (dmqlSb.Length > 0)
                {
                    if (dmqlSb.ToString(dmqlSb.Length - 1, 1).EndsWith(","))
                    {
                        dmqlSb.Remove(dmqlSb.Length - 1, 1).Append("\r\n");
                    }

                }
                Console.WriteLine(e.Message);
            }
        }


        private void GetSampleDataForRDCSingleDef(string defName, string sampleDataFilePath, string logPath, string resultFilePath, string publicStatuses, string conditionCode, string retsQueryParameter, string photoCollectedYN, string retsSearchPhotoQuery)
        {
            try
            {
                var defPath = GetDefFilePath(defName);
                if (!IsRDCDefFileAvailable(defPath))
                    return;

                var searchEngine = new SearchEngine();
                var mlsEngine = new MLSEngine(File.ReadAllText(defPath), null);
                var listingKeyField = mlsEngine.getCmaFields().getField("RecordID").getRecordPosition().Replace("\"", "");
                var lastModifiedField = "";

                var photoType = GetExactParameter(retsSearchPhotoQuery + "\r\n", "type", "\r\n");
                if (string.IsNullOrWhiteSpace(photoType)) photoType = GetExactFirstParameter(retsSearchPhotoQuery + "\r\n", "type", "\r\n"); 
                if (photoType.IndexOf("&") != -1) photoType = photoType.Remove(photoType.IndexOf("&"));

                try
                {
                    lastModifiedField = mlsEngine.getCmaFields().getField("STDFLastMod").getRecordPosition().Replace("\"", "");
                }
                catch (Exception ex1)
                {
                }
                var lastModifiedDateFormat = "MM/dd/yyyyTHH:mm:ss";
                try
                {
                    lastModifiedDateFormat = getDateFormatType(defPath, "ST_LastMod");
                }
                catch (Exception ex1)
                {
                }
                var soldDateField = "";
                try
                {
                    soldDateField = mlsEngine.getCmaFields().getField("CMASALEDATE").getRecordPosition().Replace("\"", "");
                }
                catch (Exception ex1)
                {
                }
                var saleDateFormat = "";
                try
                {
                    saleDateFormat = getDateFormatType(defPath, "ST_SaleDate", "YYYY-MM-DD");
                }
                catch (Exception ex1)
                {
                }

                var photoLastModifiedField = "";
                try
                {
                    photoLastModifiedField = mlsEngine.getCmaFields().getField("STDFPicMod").getRecordPosition().Replace("\"", "");
                }
                catch (Exception ex1)
                {
                }
                var photoLastModifiedDateFormat = "MM/dd/yyyyTHH:mm:ss";
                try
                {
                    photoLastModifiedDateFormat = !string.IsNullOrWhiteSpace(photoLastModifiedField) ? getDateFormatType(defPath, "ST_PicMod") : "";
                }
                catch (Exception ex1)
                {
                }

                var recordLimit = mlsEngine.getDefParser().getValue("Common.MaxRecordsLimit");
                searchEngine.IsDebug = true;
                searchEngine.BoardID = 999999;
                bool isFailed = false;
                var resultFolder = "";
                StringBuilder defDmql = new StringBuilder();
                
                for (int i = 0; i < rdc_status.Length; i++)
                {

                    resultFolder = GetTempResultFolder("rdc");
                    var defPathInTempResultFolder = System.IO.Path.Combine(resultFolder, defName);
                    File.Copy(defPath, defPathInTempResultFolder, true);

                    var searchRequestXml = rdc_status[i].Equals("S") ? RequestXmlHelper.GetRequestXmlRDCForListhubCountSold(defPathInTempResultFolder, Login, rdc_status[i], conditionCode, retsQueryParameter) : 
                        rdc_status[i].Equals("O") ? RequestXmlHelper.GetRequestXmlRDCForListhubCountOffMarket(defPathInTempResultFolder, Login, rdc_status[i], conditionCode, retsQueryParameter) : 
                        RequestXmlHelper.GetRequestXmlRDCForListhubCount(defPathInTempResultFolder, Login, rdc_status[i], conditionCode, retsQueryParameter);


                    string logText = "";
                    bool loginFailed = false;
                    int resultCount = 0;
                    string result = "";
                    int count = -1;
                    var replyText = "";
                    while (++count < 10)
                    {
                        result = searchEngine.RunClientRequest(searchRequestXml);

                        //if (!string.IsNullOrEmpty(rPath))
                        //    File.Copy(resultFolder + "\\tcslisting.xml", rPath + "\\tcslisting.xml", true);

                        replyText = "Success";
                        resultCount = GetResultCount(File.ReadAllText(resultFolder + "\\tcslisting.xml"), ref replyText);

                        logText = File.ReadAllText(resultFolder + "\\communication.log");

                        if (resultCount == 0 || logText.IndexOf("ListhubSearchString") == -1)
                        {
                            if (rdc_status[i] == "A")
                            {
                                string countResult = File.ReadAllText(resultFolder + "\\tcslisting.xml");
                                if (countResult.IndexOf("60310") > -1)
                                    loginFailed = true;
                                File.WriteAllText(exceptFilePath + string.Format("L-{0}-", boardInfo.DatasourceId.Trim()) + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log", logText);
                                if (logText.IndexOf(exceptional_error_messages[0]) > -1 || logText.IndexOf(exceptional_error_messages[1]) > -1)
                                {
                                    Thread.Sleep(300);
                                    loginFailed = false;
                                    continue;
                                }
                            }
                        }
                        break;
                    }

                    if (!GetParameter(logText, "ListhubSearchString").ToLower().Contains("searchtype"))
                        return;

                    //var result = searchEngine.RunClientRequest(searchRequestXml);

                    //var resultCount = GetResultCount(File.ReadAllText(resultFolder + "\\tcslisting.xml"));

                    //string logText = File.ReadAllText(resultFolder + "\\communication.log");

                    //bool loginFailed = false;
                    //if (resultCount == 0 || logText.IndexOf("ListhubSearchString") == -1)
                    //{
                    //    if (rdc_status[i] == "A")
                    //    {
                    //        string countResult = File.ReadAllText(resultFolder + "\\tcslisting.xml");
                    //        if (countResult.IndexOf("60310") > -1)
                    //            loginFailed = true;

                    //        File.WriteAllText(exceptFilePath + string.Format("L-{0}-", boardInfo.DatasourceId.Trim()) + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log", logText);

                    //    }
                    //}

                    //if (resultCount == 0)
                    //{
                    //    badMoudleSb.Append(ModuleId);
                    //    if (rdc_status[i] == "A")
                    //    {
                    //        isFailed = true;
                    //        break;
                    //    }
                    //    else
                    //    {
                    //        if (!moduleSet.Contains(ModuleId + defName))
                    //        {
                    //            dmqlSb.Append("\"").Append(resultFolder).Append("\"").Append(",");
                    //        }
                    //    }
                    //    continue;
                    //}

                    if (string.IsNullOrEmpty(boardInfo.DatasourceId))
                        boardInfo.DatasourceId = "";

                    if (rdc_status[i].Equals("A"))
                    {
                        if (!hasAddedToLogin && logText.IndexOf("Listhub") > 0)
                        {
                            loginSb.Append(boardInfo.ModuleId).Append(",");
                            loginSb.Append(boardInfo.ModuleName).Append(",");
                            loginSb.Append(boardInfo.DatasourceId.Trim()).Append(",");
                            loginSb.Append(boardInfo.LoginType).Append(",");

                            loginSb.Append(GetParameter(logText, "ListhubLoginUrl")).Append(",");
                            loginSb.Append(GetParameter(logText, "ListhubUsername")).Append(",");
                            loginSb.Append(GetParameter(logText, "ListhubPassword")).Append(",");
                            loginSb.Append(GetParameter(logText, "ListhubUserAgent")).Append(",");
                            loginSb.Append(GetParameter(logText, "ListhubUAPassword")).Append(",");
                            loginSb.Append(GetParameter(logText, "ListhubRETSVersion")).Append(",");
                            loginSb.Append(GetParameter(logText, "ListhubHttpVersion")).Append(",");
                            loginSb.Append(GetParameter(logText, "ListhubHttpMethod")).Append(",");
                            loginSb.Append(GetParameter(logText, "ListhubAuthentication")).Append(",");

                            if (loginFailed)
                                loginSb.Append("FailedLogin");
                            else
                                loginSb.Append(" ");
                            loginSb.Append("\r\n");
                            hasAddedToLogin = true;
                        }
                        if (!moduleSet.Contains(DataSourceId + defName))
                        {
                            defDmql.Append(boardInfo.ModuleId).Append(",");
                            defDmql.Append(boardInfo.ModuleName).Append(",");
                            defDmql.Append(boardInfo.DatasourceId).Append(",");
                            defDmql.Append("L,");
                            string dmqlSearch = GetParameter(logText, "ListhubSearchString");
                            defDmql.Append(GetParameter(dmqlSearch, "searchtype", "&")).Append(",");
                            defDmql.Append(GetFirstParameter(dmqlSearch, "Class", "&")).Append(",");
                            defDmql.Append(recordLimit).Append(","); //Search limit
                            defDmql.Append(GetParameter(dmqlSearch, "Format", "&")).Append(",");
                            defDmql.Append(Convert.ToString(resultCount)).Append(","); //Count
                            defDmql.Append("count_records_sold").Append(",");
                            defDmql.Append("count_records_offmarket").Append(",");
                            defDmql.Append(lastModifiedField).Append(","); //Last modified field
                            defDmql.Append(lastModifiedDateFormat).Append(","); //Last modified date format
                            defDmql.Append("sold_date_field").Append(","); //sold date field
                            defDmql.Append("sold_date_format").Append(","); //sold date format
                            defDmql.Append(listingKeyField).Append(","); //Key field
                            string dmqlQuery = GetParameter(dmqlSearch + "\r\n", "query", "\r\n");
                            if (string.IsNullOrWhiteSpace(dmqlQuery)) dmqlQuery = GetFirstParameter(dmqlSearch + "\r\n", "query", "\r\n");
                            dmqlQuery = RemoveParameter(dmqlQuery, lastModifiedField);
                            defDmql.Append("\"").Append(dmqlQuery).Append("\"").Append(",");
                        }
                    }
                    else if (rdc_status[i].Equals("O"))
                    {
                        if (!moduleSet.Contains(DataSourceId + defName))
                        {
                            if (publicStatuses.IndexOf(rdc_status[i]) >= 0)
                            {
                                string dmqlSearch = GetParameter(logText, "ListhubSearchString");
                                string dmqlQuery = GetParameter(dmqlSearch + "\r\n", "query", "\r\n");
                                if (string.IsNullOrWhiteSpace(dmqlQuery)) dmqlQuery = GetFirstParameter(dmqlSearch + "\r\n", "query", "\r\n");
                                dmqlQuery = RemoveParameter(dmqlQuery, lastModifiedField);
                                defDmql.Append("\"").Append(dmqlQuery).Append("\"").Append(",");
                                defDmql = defDmql.Replace("count_records_offmarket", Convert.ToString(resultCount));
                            }
                            else
                            {
                                defDmql = defDmql.Replace("count_records_offmarket", "");
                                defDmql.Append("\"").Append("").Append("\"").Append(",");
                            }
                        }
                    }
                    else if (rdc_status[i].Equals("S"))
                    {
                        if (!moduleSet.Contains(DataSourceId + defName))
                        {
                            if (publicStatuses.IndexOf(rdc_status[i]) >= 0)
                            {
                                string dmqlSearch = GetParameter(logText, "ListhubSearchString");
                                string dmqlQuery = GetParameter(dmqlSearch + "\r\n", "query", "\r\n");
                                if (string.IsNullOrWhiteSpace(dmqlQuery)) dmqlQuery = GetFirstParameter(dmqlSearch + "\r\n", "query", "\r\n");
                                dmqlQuery = RemoveParameter(dmqlQuery, soldDateField);
                                defDmql.Append("\"").Append(dmqlQuery).Append("\"").Append(",");

                                if (!replyText.Equals("Success"))
                                {
                                    resultFolder = GetTempResultFolder("rdc");
                                    defPathInTempResultFolder = System.IO.Path.Combine(resultFolder, defName);
                                    File.Copy(defPath, defPathInTempResultFolder, true);

                                    searchRequestXml = RequestXmlHelper.GetRequestXmlRDCForListhubCountOffMarket(defPathInTempResultFolder, Login, rdc_status[i], conditionCode, retsQueryParameter);
                                    result = searchEngine.RunClientRequest(searchRequestXml);
                                    var lastReplyText = "Success";
                                    resultCount = GetResultCount(File.ReadAllText(resultFolder + "\\tcslisting.xml"), ref lastReplyText);
                                    logText = File.ReadAllText(resultFolder + "\\communication.log");
                                }
                                defDmql = defDmql.Replace("count_records_sold", Convert.ToString(resultCount));                                
                                string dmqlSearchSoldWithDate = GetParameter(logText, "ListhubSearchString");
                                string dmqlQueryWithDate = GetParameter(dmqlSearchSoldWithDate + "\r\n", "query", "\r\n");
                                string expiredDate = string.IsNullOrWhiteSpace(dmqlQuery) ? dmqlQueryWithDate : dmqlQueryWithDate.Replace(dmqlQuery, "");

                                if (!string.IsNullOrWhiteSpace(expiredDate))
                                {
                                    if (!string.IsNullOrWhiteSpace(soldDateField) && expiredDate.Contains(soldDateField))
                                        expiredDate = soldDateField;
                                    else
                                        expiredDate = GetDateParameter(expiredDate.Replace("(", "(@"), DateTime.Now.Year.ToString(), "(", ")", "=");
                                    
                                    if (!string.IsNullOrWhiteSpace(expiredDate))
                                        defDmql = defDmql.Replace("sold_date_format", saleDateFormat);
                                    else
                                        defDmql = defDmql.Replace("sold_date_format", "");
                                    
                                }

                                if (!logText.Contains(string.Format("({0}) is not searchable", expiredDate)) && replyText.Equals("Success"))
                                    defDmql = defDmql.Replace("sold_date_field", expiredDate);
                                else
                                {
                                    defDmql = defDmql.Replace("sold_date_field", "");
                                    defDmql = defDmql.Replace("sold_date_format", "");
                                }
                            }
                            else
                            {
                                defDmql = defDmql.Replace("count_records_sold", "");
                                defDmql.Append("\"").Append("").Append("\"").Append(",");
                                defDmql = defDmql.Replace("sold_date_field", "");
                                defDmql = defDmql.Replace("sold_date_format", "");
                            }
                        }
                    }                    
                }
                defDmql.Append(photoCollectedYN).Append(","); //photo_collected_yn
                defDmql.Append(photoType).Append(","); //photo_type
                defDmql.Append("").Append(","); //photo_count_field
                defDmql.Append(photoLastModifiedField).Append(","); //photo_last_modified_field
                defDmql.Append(photoLastModifiedDateFormat).Append(","); //photo_last_modified_format                            

                if (isFailed)
                {
                    dmqlSb.Append(boardInfo.ModuleId).Append(",");
                    dmqlSb.Append(boardInfo.ModuleName).Append(",");
                    dmqlSb.Append(boardInfo.DatasourceId).Append(",");
                    dmqlSb.Append("L,");
                    dmqlSb.Append(resultFolder);
                }
                else
                {
                    dmqlSb.Append(defDmql);
                }
                if (!moduleSet.Contains(DataSourceId + defName))
                {
                    dmqlSb.Remove(dmqlSb.Length - 1, 1).Append("\r\n");
                    moduleSet.Add(DataSourceId + defName);
                }
            }
            catch (Exception e)
            {
                if (dmqlSb.Length > 0)
                {
                    if (dmqlSb.ToString(dmqlSb.Length - 1, 1).EndsWith(","))
                    {
                        dmqlSb.Remove(dmqlSb.Length - 1, 1).Append("\r\n");
                    }

                }
                Console.WriteLine(e.Message);
            }
        }

        private void GetSampleDataForRDCOtherSingleDef(string defName, string sampleDataFilePath, string logPath, string resultFilePath, string publicStatuses, string conditionCode, string retsQueryParameter, string photoCollectedYN, string retsSearchPhotoQuery)
        {
            try
            {
                var defPath = GetDefFilePath(defName);
                if (!IsRDCDefFileAvailable(defPath))
                    return;

                var searchEngine = new SearchEngine();
                var mlsEngine = new MLSEngine(File.ReadAllText(defPath), null);
                var listingKeyField = mlsEngine.getCmaFields().getField("RecordID").getRecordPosition().Replace("\"", "");
                var lastModifiedField = "";
                
                var photoType = GetExactParameter(retsSearchPhotoQuery + "\r\n", "type", "\r\n");
                if (string.IsNullOrWhiteSpace(photoType)) photoType = GetExactFirstParameter(retsSearchPhotoQuery + "\r\n", "type", "\r\n");
                if (photoType.IndexOf("&") != -1) photoType = photoType.Remove(photoType.IndexOf("&"));

                try
                {
                    lastModifiedField = mlsEngine.getCmaFields().getField("STDFLastMod").getRecordPosition().Replace("\"", "");
                }
                catch (Exception ex1)
                {
                }

                var lastModifiedDateFormat = "MM/dd/yyyyTHH:mm:ss";
                try
                {
                    lastModifiedDateFormat = getDateFormatType(defPath, "ST_LastMod");
                }
                catch (Exception ex1)
                {
                }

                var soldDateField = "";
                try
                {
                    soldDateField = mlsEngine.getCmaFields().getField("CMASALEDATE").getRecordPosition().Replace("\"", "");
                }
                catch (Exception ex1)
                {
                }

                var saleDateFormat = "";
                try
                {
                    saleDateFormat = getDateFormatType(defPath, "ST_SaleDate", "YYYY-MM-DD");
                }
                catch (Exception ex1)
                {
                }

                var photoLastModifiedField = "";
                try
                {
                    photoLastModifiedField = mlsEngine.getCmaFields().getField("STDFPicMod").getRecordPosition().Replace("\"", "");
                }
                catch (Exception ex1)
                {
                }
                var photoLastModifiedDateFormat = "MM/dd/yyyyTHH:mm:ss";
                try
                {
                    photoLastModifiedDateFormat = !string.IsNullOrWhiteSpace(photoLastModifiedField) ? getDateFormatType(defPath, "ST_PicMod") : "";
                }
                catch (Exception ex1)
                {
                }
                //var expiredDateField = "";
                //try
                //{
                //    expiredDateField = mlsEngine.getCmaFields().getField("STDFExpiredDate").getRecordPosition().Replace("\"", "");
                //}
                //catch (Exception ex1)
                //{
                //}

                //if (string.IsNullOrWhiteSpace(expiredDateField))
                //{
                //    try
                //    {
                //        expiredDateField = mlsEngine.getCmaFields().getField("STDFInactiveDate").getRecordPosition().Replace("\"", "");
                //    }
                //    catch (Exception ex1)
                //    {
                //    }
                //}

                //if (string.IsNullOrWhiteSpace(expiredDateField))
                //{
                //    try
                //    {
                //        expiredDateField = mlsEngine.getCmaFields().getField("STDFStatusDate").getRecordPosition().Replace("\"", "");
                //    }
                //    catch (Exception ex1)
                //    {
                //    }
                //}

                var recordLimit = mlsEngine.getDefParser().getValue("Common.MaxRecordsLimit");
                searchEngine.IsDebug = true;
                searchEngine.BoardID = 999999;
                bool isFailed = false;
                var resultFolder = "";
                StringBuilder defDmql = new StringBuilder();
                
                for (int i = 0; i < rdc_other_status.Length; i++)
                {

                    resultFolder = GetTempResultFolder("rdcother");
                    var defPathInTempResultFolder = System.IO.Path.Combine(resultFolder, defName);
                    File.Copy(defPath, defPathInTempResultFolder, true);

                    var searchRequestXml = rdc_other_status[i].Equals("S") ? RequestXmlHelper.GetRequestXmlRDCForListhubCountSold(defPathInTempResultFolder, Login, rdc_other_status[i], conditionCode, retsQueryParameter) : RequestXmlHelper.GetRequestXmlRDCForListhubCountOffMarket(defPathInTempResultFolder, Login, rdc_other_status[i], conditionCode, retsQueryParameter);



                    string logText = "";
                    bool loginFailed = false;
                    int resultCount = 0;
                    string result = "";
                    int count = -1;
                    var replyText = "";
                    while (++count < 10)
                    {
                        result = searchEngine.RunClientRequest(searchRequestXml);

                        //if (!string.IsNullOrEmpty(rPath))
                        //    File.Copy(resultFolder + "\\tcslisting.xml", rPath + "\\tcslisting.xml", true);

                        replyText = "Success"; 
                        resultCount = GetResultCount(File.ReadAllText(resultFolder + "\\tcslisting.xml"), ref replyText);

                        logText = File.ReadAllText(resultFolder + "\\communication.log");

                        if (resultCount == 0 || logText.IndexOf("ListhubSearchString") == -1)
                        {
                            if (rdc_other_status[i] == "S")
                            {
                                string countResult = File.ReadAllText(resultFolder + "\\tcslisting.xml");
                                if (countResult.IndexOf("60310") > -1)
                                    loginFailed = true;
                                File.WriteAllText(exceptFilePath + string.Format("L-{0}-", boardInfo.DatasourceId.Trim()) + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log", logText);
                                if (logText.IndexOf(exceptional_error_messages[0]) > -1 || logText.IndexOf(exceptional_error_messages[1]) > -1)
                                {
                                    Thread.Sleep(300);
                                    loginFailed = false;
                                    continue;
                                }
                            }
                        }
                        break;
                    }

                    if (!GetParameter(logText, "ListhubSearchString").ToLower().Contains("searchtype"))
                        return;

                    //var result = searchEngine.RunClientRequest(searchRequestXml);

                    //var resultCount = GetResultCount(File.ReadAllText(resultFolder + "\\tcslisting.xml"));

                    //string logText = File.ReadAllText(resultFolder + "\\communication.log");

                    //bool loginFailed = false;
                    //if (resultCount == 0 || logText.IndexOf("ListhubSearchString") == -1)
                    //{
                    //    if (rdc_other_status[i] == "S")
                    //    {
                    //        string countResult = File.ReadAllText(resultFolder + "\\tcslisting.xml");
                    //        if (countResult.IndexOf("60310") > -1)
                    //            loginFailed = true;

                    //        File.WriteAllText(exceptFilePath + string.Format("L-{0}-", boardInfo.DatasourceId.Trim()) + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log", logText);

                    //    }
                    //}

                    //if (resultCount == 0)
                    //{
                    //    badMoudleSb.Append(ModuleId);
                    //    if (rdc_status[i] == "A")
                    //    {
                    //        isFailed = true;
                    //        break;
                    //    }
                    //    else
                    //    {
                    //        if (!moduleSet.Contains(ModuleId + defName))
                    //        {
                    //            dmqlSb.Append("\"").Append(resultFolder).Append("\"").Append(",");
                    //        }
                    //    }
                    //    continue;
                    //}

                    if (string.IsNullOrEmpty(boardInfo.DatasourceId))
                        boardInfo.DatasourceId = "";

                    if (!hasAddedToLogin && logText.IndexOf("Listhub") > 0)
                    {
                        loginSb.Append(boardInfo.ModuleId).Append(",");
                        loginSb.Append(boardInfo.ModuleName).Append(",");
                        loginSb.Append(string.IsNullOrWhiteSpace(boardInfo.MainDatasourceId) ? boardInfo.DatasourceId.Trim(): boardInfo.MainDatasourceId.Trim()).Append(",");
                        loginSb.Append(boardInfo.LoginType).Append(",");
                        loginSb.Append(GetParameter(logText, "ListhubLoginUrl")).Append(",");
                        loginSb.Append(GetParameter(logText, "ListhubUsername")).Append(",");
                        loginSb.Append(GetParameter(logText, "ListhubPassword")).Append(",");
                        loginSb.Append(GetParameter(logText, "ListhubUserAgent")).Append(",");
                        loginSb.Append(GetParameter(logText, "ListhubUAPassword")).Append(",");
                        loginSb.Append(GetParameter(logText, "ListhubRETSVersion")).Append(",");
                        loginSb.Append(GetParameter(logText, "ListhubHttpVersion")).Append(",");
                        loginSb.Append(GetParameter(logText, "ListhubHttpMethod")).Append(",");
                        loginSb.Append(GetParameter(logText, "ListhubAuthentication")).Append(",");
                        if (loginFailed)
                            loginSb.Append("FailedLogin");
                        else
                            loginSb.Append(" ");
                        loginSb.Append("\r\n");
                        hasAddedToLogin = true;
                    }

                    if (rdc_other_status[i].Equals("S"))
                    {
                        if (!moduleSet.Contains(DataSourceId + defName))
                        {
                            if (publicStatuses.IndexOf(rdc_other_status[i]) >= 0)
                            {
                                defDmql.Append(boardInfo.ModuleId).Append(",");
                                defDmql.Append(boardInfo.ModuleName).Append(",");
                                defDmql.Append(string.IsNullOrWhiteSpace(boardInfo.MainDatasourceId) ? boardInfo.DatasourceId.Trim() : boardInfo.MainDatasourceId.Trim()).Append(",");
                                defDmql.Append("L,");
                                string dmqlSearch = GetParameter(logText, "ListhubSearchString");

                                if (!replyText.Equals("Success"))
                                {
                                    resultFolder = GetTempResultFolder("rdcother");
                                    defPathInTempResultFolder = System.IO.Path.Combine(resultFolder, defName);
                                    File.Copy(defPath, defPathInTempResultFolder, true);

                                    searchRequestXml = RequestXmlHelper.GetRequestXmlRDCForListhubCountOffMarket(defPathInTempResultFolder, Login, rdc_other_status[i], conditionCode, retsQueryParameter);
                                    result = searchEngine.RunClientRequest(searchRequestXml);
                                    var lastReplyText = "Success";
                                    resultCount = GetResultCount(File.ReadAllText(resultFolder + "\\tcslisting.xml"), ref lastReplyText);
                                    logText = File.ReadAllText(resultFolder + "\\communication.log");
                                }

                                defDmql.Append(GetParameter(dmqlSearch, "searchtype", "&")).Append(",");
                                defDmql.Append(GetFirstParameter(dmqlSearch, "Class", "&")).Append(",");
                                defDmql.Append(recordLimit).Append(","); //Search limit
                                defDmql.Append(GetParameter(dmqlSearch, "Format", "&")).Append(",");
                                defDmql.Append("").Append(",");
                                defDmql.Append(Convert.ToString(resultCount)).Append(","); //Count                            
                                defDmql.Append("count_records_offmarket").Append(",");
                                defDmql.Append(lastModifiedField).Append(","); //Last modified field
                                defDmql.Append(lastModifiedDateFormat).Append(","); //Last modified date format

                                string dmqlQuery = GetParameter(dmqlSearch + "\r\n", "query", "\r\n");
                                if (string.IsNullOrWhiteSpace(dmqlQuery)) dmqlQuery = GetFirstParameter(dmqlSearch + "\r\n", "query", "\r\n");
                                dmqlQuery = RemoveParameter(dmqlQuery, soldDateField);

                                string dmqlSearchSoldWithDate = GetParameter(logText, "ListhubSearchString");
                                string dmqlQueryWithDate = GetParameter(dmqlSearchSoldWithDate + "\r\n", "query", "\r\n");
                                string expiredDate = string.IsNullOrWhiteSpace(dmqlQuery) ? dmqlQueryWithDate : dmqlQueryWithDate.Replace(dmqlQuery, "");

                                if (!string.IsNullOrWhiteSpace(expiredDate))
                                {
                                    if (!string.IsNullOrWhiteSpace(soldDateField) && expiredDate.Contains(soldDateField))
                                        expiredDate = soldDateField;
                                    else
                                        expiredDate = GetDateParameter(expiredDate.Replace("(","(@"), DateTime.Now.Year.ToString(), "(", ")", "=");

                                    if (string.IsNullOrWhiteSpace(expiredDate))
                                        saleDateFormat = String.Empty;
                                }

                                if (!logText.Contains(string.Format("({0}) is not searchable", expiredDate)) && replyText.Equals("Success"))
                                    soldDateField = expiredDate;
                                else
                                {
                                    soldDateField = String.Empty;
                                    saleDateFormat= String.Empty;
                                }

                                defDmql.Append(soldDateField).Append(",");
                                defDmql.Append(saleDateFormat).Append(","); //sold date date format
                                defDmql.Append(listingKeyField).Append(","); //Key field                            
                                defDmql.Append("").Append(","); //dmql_query for active  
                                defDmql.Append("\"").Append(dmqlQuery).Append("\"").Append(",");
                            }
                            else
                            {
                                defDmql = defDmql.Replace("count_records_sold", "");
                                defDmql.Append("\"").Append("").Append("\"").Append(",");
                                defDmql = defDmql.Replace("sold_date_field", "");
                                defDmql = defDmql.Replace("sold_date_format", "");
                            }
                        }
                    }
                    else if (rdc_other_status[i].Equals("O"))
                    {
                        if (!moduleSet.Contains(DataSourceId + defName))
                        {
                            if (publicStatuses.IndexOf(rdc_other_status[i]) >= 0)
                            {
                                string dmqlSearch = GetParameter(logText, "ListhubSearchString");
                                string dmqlQuery = GetParameter(dmqlSearch + "\r\n", "query", "\r\n");
                                if (string.IsNullOrWhiteSpace(dmqlQuery)) dmqlQuery = GetFirstParameter(dmqlSearch + "\r\n", "query", "\r\n");
                                dmqlQuery = RemoveParameter(dmqlQuery, lastModifiedField);
                                defDmql.Append("\"").Append(dmqlQuery).Append("\"").Append(",");
                                defDmql = defDmql.Replace("count_records_offmarket", Convert.ToString(resultCount));
                            }
                            else
                            {
                                defDmql = defDmql.Replace("count_records_offmarket", "");
                                defDmql.Append("\"").Append("").Append("\"").Append(",");
                            }
                        }
                    }
                }

                defDmql.Append(photoCollectedYN).Append(","); //photo_collected_yn
                defDmql.Append(photoType).Append(","); //photo_type
                defDmql.Append("").Append(","); //photo_count_field
                defDmql.Append(photoLastModifiedField).Append(","); //photo_last_modified_field
                defDmql.Append(photoLastModifiedDateFormat).Append(","); //photo_last_modified_format    


                if (isFailed)
                {
                    dmqlSb.Append(boardInfo.ModuleId).Append(",");
                    dmqlSb.Append(boardInfo.ModuleName).Append(",");
                    dmqlSb.Append(boardInfo.DatasourceId).Append(",");
                    dmqlSb.Append("L,");
                    dmqlSb.Append(resultFolder);
                }
                else
                {
                    dmqlSb.Append(defDmql);
                }
                if (!moduleSet.Contains(DataSourceId + defName))
                {
                    dmqlSb.Remove(dmqlSb.Length - 1, 1).Append("\r\n");
                    moduleSet.Add(DataSourceId + defName);
                }
            }
            catch (Exception e)
            {
                if (dmqlSb.Length > 0)
                {
                    if (dmqlSb.ToString(dmqlSb.Length - 1, 1).EndsWith(","))
                    {
                        dmqlSb.Remove(dmqlSb.Length - 1, 1).Append("\r\n");
                    }

                }
                Console.WriteLine(e.Message);
            }
        }

        public void GetNonTCSRETSLoginInfo(Dictionary<string,string> dictDSTranslator)
        {

            using (SystemEntities se = new SystemEntities())
            {
                try
                {
                    var dataSourceConfigResult =
                        se.spDA_RETSLoginInfo_sel().ToList();

                    foreach (var item in dataSourceConfigResult)
                    {
                        loginSb.Append("").Append(",");
                        loginSb.Append(!string.IsNullOrWhiteSpace(item.mls_name) ? item.mls_name.Trim() : "").Append(",");
                        loginSb.Append(!string.IsNullOrWhiteSpace(item.data_source_id) ? (dictDSTranslator.ContainsKey(item.data_source_id.Trim()) ? dictDSTranslator[item.data_source_id.Trim()] : item.data_source_id) : "").Append(",");
                        loginSb.Append(!string.IsNullOrWhiteSpace(item.login_type) ? item.login_type : "").Append(",");
                        loginSb.Append(!string.IsNullOrWhiteSpace(item.rets_url) ? item.rets_url.Trim() : "").Append(",");
                        loginSb.Append(!string.IsNullOrWhiteSpace(item.user_name) ? item.user_name.Trim() : "").Append(",");
                        loginSb.Append(!string.IsNullOrWhiteSpace(item.password) ? item.password.Trim() : "").Append(",");
                        loginSb.Append(!string.IsNullOrWhiteSpace(item.user_agent) ? item.user_agent.Trim() : "").Append(",");
                        loginSb.Append(!string.IsNullOrWhiteSpace(item.user_agent_password) ? item.user_agent_password.Trim() : "").Append(",");
                        loginSb.Append(!string.IsNullOrWhiteSpace(item.rets_version) ? string.Format("RETS/{0}",item.rets_version.Trim()) : "").Append(",");
                        loginSb.Append(!string.IsNullOrWhiteSpace(item.http_version) ? item.http_version.Trim() : "").Append(",");
                        loginSb.Append(!string.IsNullOrWhiteSpace(item.http_method) ? item.http_method.Trim() : "").Append(",");
                        loginSb.Append(!string.IsNullOrWhiteSpace(item.auth_type) ? item.auth_type.Trim() : "").Append(",");
                        loginSb.Append("\r\n");

                        LoginInfoExtra loginInfoExtra = new LoginInfoExtra();
                        loginInfoExtra.ByPassAuthentication = "1";
                        loginInfoExtra.UaPassword = item.user_agent_password == null ? "" : item.user_agent_password;
                        loginInfoExtra.UserAgent = item.user_agent == null ? "" : item.user_agent;
                        loginInfoExtra.Username = item.user_name == null ? "" : item.user_name;
                        loginInfoExtra.Password = item.password == null ? "" : item.password;                        
                        loginInfoExtra.RetsUrl = item.rets_url == null ? "" : item.rets_url;
                        loginInfoExtra.RetsVersion = item.rets_version == null ? "" : string.Format("RETS/{0}", item.rets_version.Trim());
                        loginInfoExtra.HttpUserAgent = item.user_agent == null ? "" : item.user_agent;
                        loginInfoExtra.HttpMethod = item.http_method == null ? "" : item.http_method;
                        loginInfoExtra.HttpVersion = item.http_version == null ? "" : item.http_version;
                        loginDict.Add(item.data_source_id.Trim(), loginInfoExtra);
                    }


                }
                catch (Exception ex)
                {
                }

            }

        }


        public void GetNonTCSRETSListingConfigInfo(Dictionary<string, string> dictDSTranslator)
        {

            using (SystemEntities se = new SystemEntities())
            {
                try
                {
                    se.CommandTimeout = 0; 
                    var dataSourceConfigResult =
                        se.spDA_RETSListingConfig_sel().ToList();

                    foreach (var item in dataSourceConfigResult)
                    {
                        dmqlSb.Append("").Append(",");
                        dmqlSb.Append(!string.IsNullOrWhiteSpace(item.mls_name) ? item.mls_name.Trim() : "").Append(",");
                        dmqlSb.Append(!string.IsNullOrWhiteSpace(item.data_source_id) ? (dictDSTranslator.ContainsKey(item.data_source_id.Trim()) ? dictDSTranslator[item.data_source_id.Trim()] : item.data_source_id) : "").Append(",");
                        dmqlSb.Append(!string.IsNullOrWhiteSpace(item.resource_type) ? item.resource_type : "").Append(",");
                        dmqlSb.Append(!string.IsNullOrWhiteSpace(item.resource) ? item.resource.Trim() : "").Append(",");
                        dmqlSb.Append(!string.IsNullOrWhiteSpace(item.@class) ? item.@class.Trim() : "").Append(",");
                        dmqlSb.Append(item.search_limit == null ? "" : item.search_limit.Trim()).Append(",");
                        dmqlSb.Append(!string.IsNullOrWhiteSpace(item.search_format) ? item.search_format.Trim() : "").Append(",");
                        dmqlSb.Append(!string.IsNullOrWhiteSpace(item.status_active_dqml_query) ? CalculateCountsByClassForListings(item.data_source_id.Trim(), item.@class.Trim(), GetFormattedText(item.status_active_dqml_query.Trim(),false), "A") : item.count_records_active.ToString().Trim()).Append(",");
                        dmqlSb.Append(!string.IsNullOrWhiteSpace(item.status_sold_dqml_query) ? CalculateCountsByClassForListings(item.data_source_id.Trim(), item.@class.Trim(), GetFormattedText(item.status_sold_dqml_query.Trim(),false), "S") : !string.IsNullOrWhiteSpace(item.count_records_sold) ? item.count_records_sold.Trim() : "").Append(",");
                        dmqlSb.Append(!string.IsNullOrWhiteSpace(item.status_offmarket_dqml_query) ? CalculateCountsByClassForListings(item.data_source_id.Trim(), item.@class.Trim(), GetFormattedText(item.status_offmarket_dqml_query.Trim(),false), "O") : !string.IsNullOrWhiteSpace(item.count_records_offmarket) ? item.count_records_offmarket.Trim() : "").Append(",");
                        dmqlSb.Append(item.last_modified_date_field == null ? "" : item.last_modified_date_field.Trim()).Append(",");                        
                        dmqlSb.Append(!string.IsNullOrWhiteSpace(item.sold_date_field) ? item.sold_date_field.Trim() : "").Append(",");
                        dmqlSb.Append(!string.IsNullOrWhiteSpace(item.key_field) ? item.key_field.Trim() : "").Append(",");
                        dmqlSb.Append("\"").Append(!string.IsNullOrWhiteSpace(item.status_active_dqml_query) ? GetFormattedText(item.status_active_dqml_query.Trim()) : "").Append("\"").Append(",");
                        dmqlSb.Append(!string.IsNullOrWhiteSpace(item.status_sold_dqml_query) ? item.status_sold_dqml_query.Trim() : "").Append(",");
                        dmqlSb.Append(!string.IsNullOrWhiteSpace(item.status_offmarket_dqml_query) ? item.status_offmarket_dqml_query.Trim() : "").Append(",");
                        dmqlSb.Append("y").Append(",");
                        dmqlSb.Append(!string.IsNullOrWhiteSpace(item.photo_type) ? item.photo_type.Trim() : "").Append(",");
                        dmqlSb.Append("").Append(",");
                        dmqlSb.Append(!string.IsNullOrWhiteSpace(item.photo_last_modified_field) ? item.photo_last_modified_field.Trim() : "").Append(",");
                        dmqlSb.Append("").Append(",");
                        dmqlSb.Append("\r\n");
                    }


                }
                catch (Exception ex)
                {
                }

            }

        }

        public void GetNonTCSRETSRosterConfigInfo(Dictionary<string, string> dictDSTranslator)
        {

            using (SystemEntities se = new SystemEntities())
            {
                try
                {
                    se.CommandTimeout = 0;
                    var dataSourceConfigResult =
                        se.spDA_RETSRosterConfig_sel().ToList();

                    foreach (var item in dataSourceConfigResult)
                    {
                        dmqlSb.Append("").Append(",");
                        dmqlSb.Append(!string.IsNullOrWhiteSpace(item.mls_name) ? item.mls_name.Trim() : "").Append(",");
                        dmqlSb.Append(!string.IsNullOrWhiteSpace(item.data_source_id) ? (dictDSTranslator.ContainsKey(item.data_source_id.Trim()) ? dictDSTranslator[item.data_source_id.Trim()] : item.data_source_id) : "").Append(",");
                        dmqlSb.Append(!string.IsNullOrWhiteSpace(item.resource) ? item.resource.ToLower().Contains("agent") ? "A" : "O" : "").Append(",");
                        dmqlSb.Append(!string.IsNullOrWhiteSpace(item.resource) ? item.resource.Trim() : "").Append(",");
                        dmqlSb.Append(!string.IsNullOrWhiteSpace(item.@class) ? item.@class.Trim() : "").Append(",");
                        dmqlSb.Append(item.search_limit == null ? "" : item.search_limit.Trim()).Append(",");
                        dmqlSb.Append(!string.IsNullOrWhiteSpace(item.search_format) ? item.search_format.Trim() : "").Append(",");
                        dmqlSb.Append(item.count_records_active.ToString()).Append(",");
                        dmqlSb.Append("").Append(",");
                        dmqlSb.Append("").Append(",");
                        dmqlSb.Append(item.last_modified_date_field == null ? "" : item.last_modified_date_field.Trim()).Append(",");                        
                        dmqlSb.Append("").Append(",");
                        dmqlSb.Append(item.key_field == null ? "" : item.key_field.Trim()).Append(",");                                                
                        dmqlSb.Append("\"").Append(!string.IsNullOrWhiteSpace(item.status_active_dqml_query) ? GetFormattedText(item.status_active_dqml_query.Trim()).Trim() : "").Append("\"").Append(",");
                        dmqlSb.Append("").Append(",");
                        dmqlSb.Append("").Append(",");
                        dmqlSb.Append("\r\n");
                    }


                }
                catch (Exception ex)
                {
                }

            }

        }
                
        private string PrepareTransaction(LoginInfoExtra loginExtra, string searchQuery,  string dataSourceId="", string tcpipType="9", string useRelativePath ="")
        {              
            //Generate request and DEF file
            
            string runningDirectory = string.Format(@"{0}\speedup\", ConfigurationManager.AppSettings["Drive"]);
            //string runningDirectory = CommonUtilities.ProgramPath.Remove(CommonUtilities.ProgramPath.LastIndexOf("bin"));

            string def = ReadFile(System.IO.Path.Combine(runningDirectory, !string.IsNullOrWhiteSpace(dataSourceId) && DATA_SOURCE_NAMES_WITH_COUNT_ISSUES.Contains(dataSourceId) ? "default1.def" : "default.def"));
            def = def.Replace("$RETS_VERSION$", loginExtra.RetsVersion);
            def = def.Replace("$HTTP_VERSION$", loginExtra.HttpVersion);
            def = def.Replace("$LOGIN_URL$", loginExtra.RetsUrl);
            def = def.Replace("$SEARCH_QUERY$", searchQuery);
            def = def.Replace("$RETS_UA_USERAGENT$", loginExtra.UserAgent);
            def = def.Replace("$RETS_UA_PASSWORD$", loginExtra.UaPassword);
            def = def.Replace("$HTTP_USERAGENT$", loginExtra.HttpUserAgent);
            def = def.Replace("$HTTP_METHOD$", loginExtra.HttpMethod);
            def = def.Replace("$TCPIP_TYPE$", tcpipType);
            def = def.Replace("$USE_RELATIVEPATH$", useRelativePath);
            string datetimeFolderName = DateTime.Now.ToString("yyyyMMddhhmmss");
            string resultFolder = System.IO.Path.Combine(System.IO.Path.Combine(runningDirectory, "Result"), datetimeFolderName);
            Directory.CreateDirectory(resultFolder);
            string defPath = System.IO.Path.Combine(resultFolder, INSTANCE_DEF);
            WriteFile(defPath, def);
            StringBuilder sb = new StringBuilder();
            sb.Append("[" + SECTION_SETTING + "]\r\n");
            sb.Append("$USER_NAME$=" + loginExtra.Username).Append("\r\n");
            sb.Append("$PASSWORD$=" + loginExtra.Password).Append("\r\n");
            sb.Append("$RETS_VERSION$=" + loginExtra.RetsVersion).Append("\r\n");
            sb.Append("$HTTP_VERSION$=" + loginExtra.HttpVersion).Append("\r\n");
            sb.Append("$LOGIN_URL$=" + loginExtra.RetsUrl).Append("\r\n");
            sb.Append("$SEARCH_QUERY$=" + searchQuery).Append("\r\n");
            sb.Append("$RETS_UA_USERAGENT$=" + loginExtra.UserAgent).Append("\r\n");
            sb.Append("$RETS_UA_PASSWORD$=" + loginExtra.UaPassword).Append("\r\n");
            sb.Append("$HTTP_USERAGENT$=" + loginExtra.HttpUserAgent).Append("\r\n");
            sb.Append("$HTTP_METHOD$=" + loginExtra.HttpMethod).Append("\r\n");
            sb.Append("$TCPIP_TYPE$=" + tcpipType).Append("\r\n");
            sb.Append("$USE_RELATIVEPATH$=" + useRelativePath).Append("\r\n");
            return defPath;
        }


        private string CalculateCountsByClassForListings(string dataSourceId, string retsClass, string searchQuery, string listingStatus)
        {
            LoginInfoExtra loginInfoExtra = loginDict[dataSourceId.Trim()];
            string defPath = PrepareTransaction(loginInfoExtra, string.Format("?SearchType=Property&Class={0}&Limit=300&QueryType=DMQL2&Format=COMPACT-DECODED&Query={1}", retsClass, searchQuery), dataSourceId);
                
            var searchEngine = new SearchEngine();
            var mlsEngine = new MLSEngine(File.ReadAllText(defPath), null);
                
            var recordLimit = mlsEngine.getDefParser().getValue("Common.MaxRecordsLimit");
            searchEngine.IsDebug = true;
            searchEngine.BoardID = 999999;
            
            var resultFolder = "";
            StringBuilder defDmql = new StringBuilder();
                
            resultFolder = GetTempResultFolder("nontcsrdc");
            var defPathInTempResultFolder = System.IO.Path.Combine(resultFolder, INSTANCE_DEF);
            File.Copy(defPath, defPathInTempResultFolder, true);

            LoginInfo login = new LoginInfo();
            login.ByPassAuthentication = loginInfoExtra.ByPassAuthentication;
            login.UaPassword = loginInfoExtra.UaPassword;
            login.UserAgent = loginInfoExtra.UserAgent;
            login.UserName = loginInfoExtra.Username;
            login.Password = loginInfoExtra.Password;
                        
            var searchRequestXml = listingStatus.Equals("S") ? RequestXmlHelper.GetRequestXmlRDCForListhubCountSold(defPathInTempResultFolder, login, listingStatus) : RequestXmlHelper.GetRequestXmlRDCForListhubCount(defPathInTempResultFolder, login, listingStatus);

            var result = searchEngine.RunClientRequest(searchRequestXml);
            var replyText = "Success";
            var resultCount = GetResultCount(File.ReadAllText(resultFolder + "\\tcslisting.xml"), ref replyText);

            string logText = File.ReadAllText(resultFolder + "\\communication.log");

            bool loginFailed = false;
            if (resultCount == 0 || logText.IndexOf("ListhubSearchString") == -1)
            {                
                string countResult = File.ReadAllText(resultFolder + "\\tcslisting.xml");
                if (countResult.IndexOf("60310") > -1)
                    loginFailed = true;

                File.WriteAllText(exceptFilePath + string.Format("L-{0}-", dataSourceId.Trim()) + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log", logText);
                
            }
            return (!loginFailed) ? resultCount.ToString(): "0";            
        }
                

        private static string ReadFile(string filePath)
        {
            FileStream fw = null;
            StreamReader sr = null;
            string result = "";
            try
            {
                fw = new FileStream(filePath, FileMode.Open, FileAccess.Read);

                // Create a new stream to read from a file
                sr = new StreamReader(fw);

                // Read contents of file into a string
                result = sr.ReadToEnd();
            }
            catch (Exception exc)
            {
                throw new IOException("Cannot read to file - " + filePath + "\r\n" + exc.Message);
            }
            finally
            {
                if (sr != null)
                    sr.Close();
                if (fw != null)
                    fw.Close();
            }
            return result;
        }

        private static void WriteFile(string filePath, string content)
        {
            FileStream fw = null;
            try
            {
                fw = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                byte[] bc = Encoding.ASCII.GetBytes(content);
                fw.Write(bc, 0, bc.Length);
            }
            catch (Exception exc)
            {
                throw new IOException("Cannot write to file - " + filePath + "\r\n" + exc.Message);
            }
            finally
            {
                fw.Close();
            }
            return;
        }

        private string GetTempResultFolder(string subDirectoryFolder="prosoft")
        {
            var datetimeFolderName = DateTime.Now.ToString("yyyyMMddhhmmss");
            string runningDirectory = string.Format(@"{0}\speedup\{1}", ConfigurationManager.AppSettings["Drive"], subDirectoryFolder); //System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string resultFolder = System.IO.Path.Combine(System.IO.Path.Combine(runningDirectory, "Result"), datetimeFolderName);
            Directory.CreateDirectory(resultFolder);
            return resultFolder;
        }

        private string GetDefFilePath(string fileName)
        {
            //return WorkingDirectory + ModuleId + "\\" + fileName;
            return System.IO.Path.Combine(DEF_LOCATION, fileName);
        }

        private string GetExactParameter(string text, string searchFor, string endWith = "\r\n", bool decoded = true)
        {
            int pos1 = text.LastIndexOf(searchFor, StringComparison.CurrentCultureIgnoreCase);
            
            if (pos1 < 0)
                return "";
            
            if (pos1 > 0 && char.IsLetter(text[pos1-1]))
                return "";
            
            if (char.IsLetter(text[pos1 + searchFor.Length]))
                return "";
            
            int pos2 = text.IndexOf(endWith, pos1, StringComparison.CurrentCultureIgnoreCase);
            if (pos2 < 0)
                return "";


            return (decoded == true) ? text.Substring(pos1 + searchFor.Length + 1, pos2 - pos1 - searchFor.Length - 1).Trim().Replace("%2b", "+").Replace("%2B", "+").Replace("%2d", "-").Replace("%2D", "-").Replace("%22", "\"\"").Replace("%2c", ",").Replace("%2C", ",").Replace("%40", "@").Replace("%7c", "|").Replace("%7C", "|").Replace("%3a", ":").Replace("%20", " ") : text.Substring(pos1 + searchFor.Length + 1, pos2 - pos1 - searchFor.Length - 1).Trim();
        }

        private string GetExactFirstParameter(string text, string searchFor, string endWith = "\r\n", bool decoded = true)
        {
            int pos1 = text.IndexOf(searchFor, StringComparison.CurrentCultureIgnoreCase);

            if (pos1 < 0)
                return "";

            if (pos1 > 0 && char.IsLetter(text[pos1 - 1]))
                return "";

            if (char.IsLetter(text[pos1 + searchFor.Length]))
                return "";

            int pos2 = text.IndexOf(endWith, pos1, StringComparison.CurrentCultureIgnoreCase);
            if (pos2 < 0)
                return "";


            return (decoded == true) ? text.Substring(pos1 + searchFor.Length + 1, pos2 - pos1 - searchFor.Length - 1).Trim().Replace("%2b", "+").Replace("%2B", "+").Replace("%2d", "-").Replace("%2D", "-").Replace("%22", "\"\"").Replace("%2c", ",").Replace("%2C", ",").Replace("%40", "@").Replace("%7c", "|").Replace("%7C", "|").Replace("%3a", ":").Replace("%20", " ") : text.Substring(pos1 + searchFor.Length + 1, pos2 - pos1 - searchFor.Length - 1).Trim();
        }

        private string GetParameter(string text, string searchFor, string endWith = "\r\n", bool decoded = true)
        {
            int pos1 = text.LastIndexOf(searchFor, StringComparison.CurrentCultureIgnoreCase);
            if (pos1 < 0)
                return "";
            int pos2 = text.IndexOf(endWith, pos1, StringComparison.CurrentCultureIgnoreCase);
            if(pos2<0)
                return "";

            return (decoded == true) ? text.Substring(pos1 + searchFor.Length + 1, pos2 - pos1 - searchFor.Length - 1).Trim().Replace("%2b", "+").Replace("%2B", "+").Replace("%2d", "-").Replace("%2D", "-").Replace("%22", "\"\"").Replace("%2c", ",").Replace("%2C", ",").Replace("%40", "@").Replace("%7c", "|").Replace("%7C", "|").Replace("%3a", ":").Replace("%20", " ") : text.Substring(pos1 + searchFor.Length + 1, pos2 - pos1 - searchFor.Length - 1).Trim();
        }

        private string GetFirstParameter(string text, string searchFor, string endWith = "\r\n", bool decoded = true)
        {
            int pos1 = text.IndexOf(searchFor, StringComparison.CurrentCultureIgnoreCase);
            if (pos1 < 0)
                return "";
            int pos2 = text.IndexOf(endWith, pos1, StringComparison.CurrentCultureIgnoreCase);
            if (pos2 < 0)
                return "";

            return (decoded == true) ? text.Substring(pos1 + searchFor.Length + 1, pos2 - pos1 - searchFor.Length - 1).Trim().Replace("%2b", "+").Replace("%2B", "+").Replace("%2d", "-").Replace("%2D", "-").Replace("%22", "\"\"").Replace("%2c", ",").Replace("%2C", ",").Replace("%40", "@").Replace("%7c", "|").Replace("%7C", "|").Replace("%3a", ":").Replace("%20", " ") : text.Substring(pos1 + searchFor.Length + 1, pos2 - pos1 - searchFor.Length - 1).Trim();
        }

        private string GetDateParameter(string text, string year, string searchFor, string searchEnd, string endWith = "\r\n", bool decoded = true)
        {
            var partialText = "";
            do
            {
                int pos1 = text.IndexOf(searchFor, StringComparison.CurrentCultureIgnoreCase);
                if (pos1 < 0)
                    return "";
                int pos2 = text.IndexOf(searchEnd, pos1, StringComparison.CurrentCultureIgnoreCase);
                if (pos2 < 0)
                    return "";
                
                partialText = text.Substring(pos1 + searchFor.Length-1, pos2 - pos1 +1);

                if (pos2 - pos1 + 1>= text.Length)
                    return "";
                text = text.Substring(pos2 - pos1  + 1);

            } while (!partialText.Contains(year));

            if (partialText.Contains(year))
                return GetParameter(partialText, searchFor, endWith, decoded);
            else
                return "";
        }

        private string RemoveParameter(string text, string searchFor, string startWith="(", string endWith = ")")
        {
            if (string.IsNullOrWhiteSpace(searchFor)) return text; 
            try
            {
                int pos1 = text.LastIndexOf(searchFor, StringComparison.CurrentCultureIgnoreCase);
                if (pos1 < 0)
                    return text;

                while (pos1 > 0)
                {
                    if (text[--pos1].ToString().Equals(startWith))
                        break;
                }

                int pos2 = -1;

                if (pos1 == 0)
                {
                    pos2 = text.IndexOf(endWith, pos1, StringComparison.CurrentCultureIgnoreCase);

                    if (pos2 < 0) return "";

                    if (pos2 >= text.Length - 1) return text;
                    
                    if (text[++pos2] != ' ') pos2 -= 1;
                    if (text[++pos2] != ',') pos2 -= 1;
                    
                }
                else
                {
                    if (text[--pos1] == ',')
                        pos2 = text.IndexOf(endWith, pos1, StringComparison.CurrentCultureIgnoreCase);
                    else
                        pos1 += 1;
                }

                if (pos2 < 0)
                    return text.Remove(pos1);
                else
                    return text.Remove(pos1, pos2 - pos1 + 1);
            }
            catch (Exception ex)
            { return text;}
        }

        private string GetFormattedText(string text, bool decoded= true, char wildChar =';')
        {
            string result = "";
            if (text.Contains(wildChar))
            {
                string[] arrText = text.Split(wildChar);
                string initialField = "";
                foreach (string itemText in arrText)
                {
                    int pos1 = itemText.IndexOf("(");
                    if (pos1 < 0)
                        continue;
                    int pos2 = itemText.IndexOf("=");
                    if (pos2 < 0)
                        continue;

                    int pos3 = itemText.IndexOf(")");
                    if (pos3 < 0)
                        continue;

                    string field = itemText.Substring(pos1 + 1, pos2 - pos1 - 1).Trim();

                    if (field.Equals(initialField))
                    {
                        string addResult = itemText.Substring(pos2 + 1, pos3 - pos2 - 1);
                        result += "," + (addResult.StartsWith("|") ? addResult.Substring(1) : addResult);
                    }
                    else
                    {
                        initialField = field;
                        result = itemText.Substring(0, pos3);
                    }
                }

                result += ")";
            }
            else
                result = text;

            return (decoded == true) ? result.Trim().Replace("%2b", "+").Replace("%2B", "+").Replace("%2d", "-").Replace("%2D", "-").Replace("%22", "\"\"").Replace("%2c", ",").Replace("%2C", ",").Replace("%40", "@").Replace("%7c", "|").Replace("%7C", "|").Replace("%3a", ":").Replace("%20", " ") : result.Trim();
        }

        private bool HasStringDate(string textString, string regexPattern)
        {
            bool hasDate = false;
            
            DateTime dateTime = new DateTime();
            
            try
            {
                string resultString = Regex.Match(textString, regexPattern, RegexOptions.IgnorePatternWhitespace).Value;

                if (!string.IsNullOrEmpty(resultString))
                {
                    dateTime = DateTime.Parse(resultString);
                    hasDate = true;                    
                }
            }
            catch (Exception ex)
            {

            }
            
            return hasDate;

        }

        private void SplitTextResults(string text, string regexDateFormat, ref Dictionary<string,string> dictInfo)
        {            

            string and_or = string.Empty;
            int i = -1;
            while (++i < text.Length)
            {
                if (text[i] == '(')
                {
                    if (text[i + 1] == '(')
                    {
                        int lastPos = text.LastIndexOf(")");

                        if (lastPos > i)
                        {
                            if (lastPos + 1 == text.Length)
                            {
                                if (text[lastPos - 1] == ')')
                                {
                                    SplitTextResults(text.Substring(++i, lastPos - i + 1), regexDateFormat, ref dictInfo);
                                    break;
                                }
                                else
                                {
                                    int j = lastPos;
                                    do
                                    {
                                        j -= 1;
                                    } while (text[j] != '(');

                                    string dictIndex = "start_datetime_field_name";
                                    if (j > 1)
                                    {
                                        and_or = (text[j-1] == '|') ? "or" : (text[j-1] == ',') ? "and" : "";
                                        if (HasStringDate(text.Substring(1, j - 1), regexDateFormat))
                                        {
                                            dictInfo["datetime_field_operation"] = and_or;
                                            dictIndex = "end_datetime_field_name";
                                            SplitTextResults(text.Substring(1, j - 1), regexDateFormat, ref dictInfo);
                                            break;
                                        }
                                        else
                                            dictInfo["dmql_query"] = text.Substring(0, j - 1);
                                        string lastText = text.Substring(j);
                                        if (HasStringDate(lastText, regexDateFormat) && lastText.Contains("="))
                                        {
                                            dictInfo[dictIndex] = lastText.Split('=')[0].Substring(1);
                                        }

                                    }
                                    else
                                    {
                                        string lastText = text.Substring(j, lastPos);
                                        if (HasStringDate(lastText, regexDateFormat))
                                            dictInfo[dictIndex] = lastText.Split('=')[0].Substring(1);
                                        else
                                            dictInfo["dmql_query"] = lastText;
                                    }


                                }
                                break;
                            }
                            else
                            {
                                SplitTextResults(text.Substring(++i, lastPos + 1), regexDateFormat, ref dictInfo);
                                break;
                            }

                        }
                    }
                    else
                    {
                        int firstPos = text.IndexOf(")");
                        if (firstPos > i)
                        {
                            string firstText = text.Substring(i, firstPos - i + 1);
                            if (HasStringDate(firstText, regexDateFormat))
                            {
                                string dictIndex = string.IsNullOrWhiteSpace(dictInfo["start_datetime_field_name"]) ? "start_datetime_field_name" : "end_datetime_field_name";
                                dictInfo[dictIndex] = firstText.Split('=')[0].Substring(1);
                            }
                            else
                            {
                                dictInfo["dmql_query"] += (!string.IsNullOrEmpty(and_or) ? (and_or=="or") ? "|" : ",": "") + firstText;
                            }
                            if (firstPos + 1 == text.Length)
                                break;
                            else
                            {                                
                                SplitTextResults(text.Substring(firstPos+1), regexDateFormat, ref dictInfo);
                                break;
                            }
                        }

                    }

                }
                else
                {   
                    and_or = (text[i] == '|') ? "or" : (text[i] == ',') ? "and" : "";
                    if (!string.IsNullOrEmpty(and_or) && !string.IsNullOrWhiteSpace(dictInfo["start_datetime_field_name"]))
                    {
                        dictInfo["datetime_field_operation"] = and_or;
                    }    
                }

                
            }

        }

        private int GetResultCount(string tcsResult,  ref string replyText)
        {
            var listingCount = 0;
            try
            {
                if (tcsResult.IndexOf("ReplyText=\"Success\"", StringComparison.CurrentCultureIgnoreCase) >
                    -1)
                {
                    var xelement = XElement.Parse(tcsResult);
                    var listingsNode = xelement.Descendants("Listings").SingleOrDefault();
                    if (listingsNode != null)
                        int.TryParse(listingsNode.Attribute("TotalCount").Value, out listingCount);
                }
                else
                    replyText = "Failed";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return listingCount;
        }

        private string getDateFormatType(string defPath, string fieldDateSearch, string defaultDateFormat = "yyyy-MM-ddTHH:mm:ss")
        {
            var def = new IniFile(defPath);

            string dateFieldName = def.Read(fieldDateSearch, "Standard_Search") ?? "";
            var dateFormat = def.Read("SDateFormat", "field_" + dateFieldName) ?? "";
            return (string.IsNullOrWhiteSpace(dateFormat) ? defaultDateFormat: dateFormat);
        }

        private bool IsRDCDefFileAvailable(string defPath)
        {
            var def = new IniFile(defPath);
            var isOrcaNotAvailable = def.Read("DEFNotAvailableto", "Common");
            return !(!string.IsNullOrWhiteSpace(isOrcaNotAvailable) && isOrcaNotAvailable.ToUpper().Contains("ORCA"));
        }

        private bool IsProSoftDefFileAvailable(string defPath)
        {
            var def = new IniFile(defPath);
            var isProSoftNotAvailable = def.Read("DEFNotAvailableTo", "Common");
            return !(!string.IsNullOrWhiteSpace(isProSoftNotAvailable) && 
                (isProSoftNotAvailable.ToUpper().Contains("TMK") || isProSoftNotAvailable.ToUpper().Contains("TPO")));
        }
    }
}
