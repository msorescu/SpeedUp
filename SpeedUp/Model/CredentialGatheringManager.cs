using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using Core.Log;
using System.Collections.Generic;
using System.Data.Objects;

namespace SpeedUp.Model
{
    class CredentialGatheringManager
    {
        public BoardCredentials getCredentials(BoardCredentials BoardCreds, StreamWriter streamWriterReport)
        {

            TCSEntities tcsDb = null;
            TCSEntitiesProduction tcsproddb = null;
            tcsDb = new TCSEntities();
            tcsproddb = new TCSEntitiesProduction();
            try
            {
                var helper = new PsaHelper();
                var connections = helper.GetDefConnections(BoardCreds.ModuleId);
                bool credsfound = false;
                char[] delimiters = new char[] { ',' };

                string DefFilePath = "";                
                
                string mid = Convert.ToString(BoardCreds.ModuleId);
                BoardCreds.DatasourceId = helper.GetDataSourceId(mid);
                
                foreach (var connection in connections)
                {
                    if (credsfound == true) continue;
                    if (connection.connection_name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)) continue;
                    DefFilePath = connection.definition_file;
                    //BoardId = connection.board_id ?? 0;
                    var iniReader = new IniFile(connection.definition_file);
                    var tcpIp = iniReader.Read("TcpIp", "Common");
                    //var HTTPUserAgent = iniReader.Read("SecList", "HttpUserAgent");
                    ////var RetsUAUserAgent = iniReader.Read("SecList", "RetsUAUserAgent");
                    ////var RetsUAPwd = iniReader.Read("SecList", "RetsUAPwd");
                    //var SecList = iniReader.GetSectionContent("SecList");
                    //var mainscript = iniReader.GetSectionContent("MainScript");

                    //string RetsUAUserAgent = ParseSecList(SecList,"RetsUAUserAgent");
                    //string RetsUAPwd = ParseSecList(SecList, "RetsUAPwd");

                    BoardCreds.UserAgent = "";
                    BoardCreds.UserAgentPass = ""; 

                    if (tcpIp == "14")
                    {
                        //Check DEF
                        BoardCreds.LoginType = "Master";
                        BoardCreds.Username = "fakeuser";
                        BoardCreds.Password = "fakepassword";
                        BoardCreds.IsValidCredential = true;
                        BoardCreds.isRets = true;
                        return BoardCreds;

                    }
                    else if (tcpIp == "9" || tcpIp == "11" || tcpIp == "13")
                    {
                        BoardCreds.LoginType = "AgentCredentials";
                        BoardCreds.isRets = true;
                    }
                    else
                    {
                        BoardCreds.isRets = false;
                        BoardCreds.IsValidCredential = false;
                        return BoardCreds;
                    }
                    //Check DB
                    credsfound = true;
                    //BoardCreds.RETSURL = getURL(mainscript);

                    try
                    {


                        var AssociatedBoards = from boards in tcsproddb.cma_mls_boards
                                               where (boards.module_id == BoardCreds.ModuleId)
                                               select boards.board_id;



                        foreach (var Board in AssociatedBoards)
                        {
                            BoardCreds.BoardId = Board;
                            DateTime dt = DateTime.Now.AddDays(-30);
                            var UniqueUsers = from users in tcsproddb.tcs_request_log
                                              join errors in tcsproddb.tcs_error_log on users.request_id equals errors.request_id into fg
                                              from fgi in fg.DefaultIfEmpty()
                                              where (users.client_name == "TMK" || users.client_name == "TPO" || users.client_name == "TMKListingAlert")
                                                   && (users.board_id == Board) && (users.when_created > dt) && (fgi.error_code == null)
                                              orderby users.when_created descending
                                              group users by users.mls_user_name into uTable
                                              select new { USER = uTable.Key, LOCATION = uTable.Max(x => x.location_path) };
                            int i = 0;
                            foreach (var user in UniqueUsers)
                            {
                                if ((user.USER != "") && (user.LOCATION != ""))
                                {
                                    if (BoardCreds.ModuleId == 2360)
                                    {
                                        if (user.USER.IndexOf("fl.") > -1)
                                            continue;
                                    }

                                    try
                                    {
                                        System.IO.StreamReader log;
                                        log = new System.IO.StreamReader(user.LOCATION);
                                        string copiedlog = log.ReadToEnd();
                                        log.Close();
                                        int startsub = copiedlog.IndexOf("<Login>") + 7;
                                        int endsub = copiedlog.IndexOf("</Login>") - startsub;

                                        string logininfo = copiedlog.Substring(startsub, endsub);
                                        logininfo = logininfo.Replace("<password>", ",");
                                        logininfo = logininfo.Replace("</password>", ",");

                                        string[] creds = logininfo.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

                                        if (creds.Length > 0)
                                        {
                                            BoardCreds.Username = creds[0];
                                        }
                                        if (creds.Length > 1)
                                        {
                                            BoardCreds.Password = creds[1];
                                        }

                                        writetofile(BoardCreds, streamWriterReport);
                                        BoardCreds.IsValidCredential = true;
                                        i++;
                                        if (i > 0)
                                            break;
                                    }
                                    catch
                                    {
                                    }




                                }
                            }

                        }

                        if (String.IsNullOrEmpty(BoardCreds.Username))
                        {
                            var query = (from users in tcsDb.CleanLogins1
                                         where (users.module_id.Equals(mid))
                                         select new { Username = users.user_name, Password = users.password });
                            var agentCredential = query.FirstOrDefault();
                            if (agentCredential == null)
                            {
                                query = (from users in tcsDb.tcs_clean_loggins
                                         where (users.module_id.Equals(BoardCreds.ModuleId))
                                         select new { Username = users.user_name, Password = users.password });
                                agentCredential = query.FirstOrDefault();
                            }

                            BoardCreds.Username = agentCredential.Username;
                            BoardCreds.Password = agentCredential.Password;
                            BoardCreds.IsValidCredential = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }


                }

            }
            catch (Exception ex)
            {
            }
            finally
            {
                tcsDb.Dispose();
                tcsproddb.Dispose();
            }
            return BoardCreds;
        }

        public BoardCredentials getRosterCredentials(BoardCredentials BoardCreds, StreamWriter streamWriterReport)
        {            
            try
            {
                var helper = new PsaHelper();
                char[] delimiters = new char[] { ',' };
                string mid = Convert.ToString(BoardCreds.ModuleId);
                BoardCreds.DatasourceId = helper.GetDataSourceId(mid);
                
                string retsLoginURL = "";
                LoginInfo loginInfo = helper.GetDataAggTraceInfo(BoardCreds.DatasourceId, ref retsLoginURL);


                string DefFilePath = "";
                string loginType = "";

                var connections = helper.GetDefConnections(BoardCreds.ModuleId);
                if (connections != null)
                {
                    foreach (var connection in connections)
                    {
                        if (string.IsNullOrWhiteSpace(retsLoginURL) == false && string.IsNullOrWhiteSpace(loginType) == false) break;
                        if (!connection.connection_name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)) continue;
                        DefFilePath = connection.definition_file;

                        var iniReader = new IniFile(connection.definition_file);
                        var tcpIp = iniReader.Read("TcpIp", "Common");

                        if (tcpIp == "14")
                        {
                            //Check DEF
                            loginType = "Master";
                            BoardCreds.isRets = true;
                        }
                        else if (tcpIp == "9" || tcpIp == "11" || tcpIp == "13")
                        {
                            loginType = "AgentCredentials";
                            BoardCreds.isRets = true;
                        }
                        else
                        {
                            BoardCreds.isRets = false;
                        }
                        if (string.IsNullOrWhiteSpace(retsLoginURL) == true)
                        {
                            var mainscript = iniReader.GetSectionContent("MainScript");
                            retsLoginURL = getURL(mainscript);
                        }
                    }
                }

                BoardCreds.IsValidCredential = false;
                if (loginInfo != null)
                {
                    BoardCreds.IsValidCredential = true;
                    BoardCreds.RETSURL = retsLoginURL;
                    BoardCreds.LoginType = loginType;
                    BoardCreds.Username = loginInfo.UserName;
                    BoardCreds.Password = loginInfo.Password;
                    BoardCreds.UserAgent = loginInfo.UserAgent;
                    BoardCreds.UserAgentPass = loginInfo.UaPassword;

                    writetofile(BoardCreds, streamWriterReport);
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            
            return BoardCreds;
        }

        public BoardCredentials getOpenHouseCredentials(BoardCredentials BoardCreds, StreamWriter streamWriterReport)
        {
            
            try
            {
                var helper = new PsaHelper();
                char[] delimiters = new char[] { ',' };
                string dsid = BoardCreds.DatasourceId;
                BoardCreds.ModuleId = helper.GetModuleId(dsid);
                
                string retsLoginURL = "";
                LoginInfo loginInfo = helper.GetDataAggTraceInfoExtra(BoardCreds.DatasourceId, BoardCreds.ModuleId, ref retsLoginURL, "TCS", "REALOpenHouse");

                string DefFilePath = "";
                string loginType = "";

                var connections = helper.GetDefConnections(BoardCreds.ModuleId);
                if (connections != null)
                {
                    foreach (var connection in connections)
                    {
                        if (string.IsNullOrWhiteSpace(retsLoginURL) == false && string.IsNullOrWhiteSpace(loginType) == false) break;
                        if ((!connection.connection_name.EndsWith("oh.sql", StringComparison.OrdinalIgnoreCase)) &&
                            (!connection.connection_name.EndsWith("oh .sql", StringComparison.OrdinalIgnoreCase))) continue;
                        DefFilePath = connection.definition_file;

                        var iniReader = new IniFile(connection.definition_file);
                        var tcpIp = iniReader.Read("TcpIp", "Common");

                        if (tcpIp == "14")
                        {
                            //Check DEF
                            loginType = "Master";
                            BoardCreds.isRets = true;
                        }
                        else if (tcpIp == "9" || tcpIp == "11" || tcpIp == "13")
                        {
                            loginType = "AgentCredentials";
                            BoardCreds.isRets = true;
                        }
                        else
                        {
                            BoardCreds.isRets = false;
                        }
                        if (string.IsNullOrWhiteSpace(retsLoginURL) == true)
                        {
                            var mainscript = iniReader.GetSectionContent("MainScript");
                            retsLoginURL = getURL(mainscript);
                        }
                    }
                }

                BoardCreds.IsValidCredential = false;
                if (loginInfo != null)
                {
                    BoardCreds.IsValidCredential = true;
                    BoardCreds.RETSURL = retsLoginURL;
                    BoardCreds.LoginType = loginType;
                    BoardCreds.Username = loginInfo.UserName;
                    BoardCreds.Password = loginInfo.Password;
                    BoardCreds.UserAgent = loginInfo.UserAgent;
                    BoardCreds.UserAgentPass = loginInfo.UaPassword;

                    writetofile(BoardCreds, streamWriterReport);
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            
            return BoardCreds;
        }

        public BoardCredentials getRDCRosterCredentials(BoardCredentials BoardCreds, StreamWriter streamWriterReport)
        {            
            try
            {
                var helper = new PsaHelper();
                char[] delimiters = new char[] { ',' };
                string dsid = BoardCreds.DatasourceId;
                BoardCreds.ModuleId = helper.GetModuleId(dsid);
                                                
                string retsLoginURL = "";
                LoginInfo loginInfo = helper.GetDataAggTraceInfoExtra(BoardCreds.DatasourceId, BoardCreds.ModuleId, ref retsLoginURL, "TCS", "REALRoster");


                string DefFilePath = "";
                string loginType = "";

                var connections = helper.GetDefConnections(BoardCreds.ModuleId);
                if (connections != null)
                {
                    foreach (var connection in connections)
                    {
                        if (string.IsNullOrWhiteSpace(retsLoginURL) == false && string.IsNullOrWhiteSpace(loginType) == false) break;
                        if (!connection.connection_name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)) continue;
                        DefFilePath = connection.definition_file;

                        var iniReader = new IniFile(connection.definition_file);
                        var tcpIp = iniReader.Read("TcpIp", "Common");

                        if (tcpIp == "14")
                        {
                            //Check DEF
                            loginType = "Master";
                            BoardCreds.isRets = true;
                        }
                        else if (tcpIp == "9" || tcpIp == "11" || tcpIp == "13")
                        {
                            loginType = "AgentCredentials";
                            BoardCreds.isRets = true;
                        }
                        else
                        {
                            BoardCreds.isRets = false;
                        }
                        if (string.IsNullOrWhiteSpace(retsLoginURL) == true)
                        {
                            var mainscript = iniReader.GetSectionContent("MainScript");
                            retsLoginURL = getURL(mainscript);
                        }
                    }
                }

                BoardCreds.IsValidCredential = false;
                if (loginInfo != null)
                {
                    BoardCreds.IsValidCredential = true;
                    BoardCreds.RETSURL = retsLoginURL;
                    BoardCreds.LoginType = loginType;
                    BoardCreds.Username = loginInfo.UserName;
                    BoardCreds.Password = loginInfo.Password;
                    BoardCreds.UserAgent = loginInfo.UserAgent;
                    BoardCreds.UserAgentPass = loginInfo.UaPassword;

                    writetofile(BoardCreds, streamWriterReport);
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return BoardCreds;
        }

        public BoardCredentials getRDCCredentials(BoardCredentials BoardCreds, StreamWriter streamWriterReport)
        {
            try
            {
                var helper = new PsaHelper();
                char[] delimiters = new char[] { ',' };
                string dsid = BoardCreds.DatasourceId;
                BoardCreds.ModuleId = helper.GetModuleId(dsid);
                                
                string retsLoginURL = "";
                LoginInfo loginInfo = helper.GetDataAggTraceInfoExtra(BoardCreds.DatasourceId, BoardCreds.ModuleId, ref retsLoginURL, "TCS", "REALListing");
                                
                string DefFilePath = "";
                string loginType = "";

                var connections = helper.GetDefConnections(BoardCreds.ModuleId);
                if (connections != null)
                {
                    foreach (var connection in connections)
                    {
                        if (string.IsNullOrWhiteSpace(retsLoginURL) == false && string.IsNullOrWhiteSpace(loginType) == false) break;
                        if (!connection.connection_name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)) continue;
                        DefFilePath = connection.definition_file;

                        var iniReader = new IniFile(connection.definition_file);
                        var tcpIp = iniReader.Read("TcpIp", "Common");

                        if (tcpIp == "14")
                        {
                            //Check DEF
                            loginType = "Master";
                            BoardCreds.isRets = true;
                        }
                        else if (tcpIp == "9" || tcpIp == "11" || tcpIp == "13")
                        {
                            loginType = "AgentCredentials";
                            BoardCreds.isRets = true;
                        }
                        else
                        {
                            BoardCreds.isRets = false;
                        }
                        if (string.IsNullOrWhiteSpace(retsLoginURL) == true)
                        {
                            var mainscript = iniReader.GetSectionContent("MainScript");
                            retsLoginURL = getURL(mainscript);
                        }
                    }
                }
                                               
                BoardCreds.IsValidCredential = false;
                if (loginInfo != null)
                {
                    BoardCreds.IsValidCredential = true;                    
                    BoardCreds.RETSURL = retsLoginURL;
                    BoardCreds.LoginType = loginType;
                    BoardCreds.Username = loginInfo.UserName;
                    BoardCreds.Password = loginInfo.Password;
                    BoardCreds.UserAgent = loginInfo.UserAgent;
                    BoardCreds.UserAgentPass = loginInfo.UaPassword;   
                    
                    writetofile(BoardCreds, streamWriterReport);                    
                }                
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            
            return BoardCreds;
        }

        public BoardCredentials getRDCOtherCredentials(BoardCredentials BoardCreds, StreamWriter streamWriterReport)
        {   
            try
            {
                var helper = new PsaHelper();
                char[] delimiters = new char[] { ',' };                               
                string dsid = BoardCreds.DatasourceId;
                BoardCreds.ModuleId = helper.GetOtherModuleId(dsid);

                string retsLoginURL = "";
                LoginInfo loginInfo = helper.GetDataAggTraceInfoExtra(BoardCreds.DatasourceId, BoardCreds.ModuleId, ref retsLoginURL, "TCS", "REALListing");

                string DefFilePath = "";
                string loginType = "";

                var connections = helper.GetDefConnections(BoardCreds.ModuleId);
                if (connections != null)
                {
                    foreach (var connection in connections)
                    {
                        if (string.IsNullOrWhiteSpace(retsLoginURL) == false && string.IsNullOrWhiteSpace(loginType) == false) break;
                        if (!connection.connection_name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)) continue;
                        DefFilePath = connection.definition_file;

                        var iniReader = new IniFile(connection.definition_file);
                        var tcpIp = iniReader.Read("TcpIp", "Common");

                        if (tcpIp == "14")
                        {
                            //Check DEF
                            loginType = "Master";
                            BoardCreds.isRets = true;
                        }
                        else if (tcpIp == "9" || tcpIp == "11" || tcpIp == "13")
                        {
                            loginType = "AgentCredentials";
                            BoardCreds.isRets = true;
                        }
                        else
                        {
                            BoardCreds.isRets = false;
                        }
                        if (string.IsNullOrWhiteSpace(retsLoginURL) == true)
                        {
                            var mainscript = iniReader.GetSectionContent("MainScript");
                            retsLoginURL = getURL(mainscript);
                        }
                    }
                }

                BoardCreds.IsValidCredential = false;
                if (loginInfo != null)
                {
                    BoardCreds.IsValidCredential = true;
                    BoardCreds.RETSURL = retsLoginURL;
                    BoardCreds.LoginType = loginType;
                    BoardCreds.Username = loginInfo.UserName;
                    BoardCreds.Password = loginInfo.Password;
                    BoardCreds.UserAgent = loginInfo.UserAgent;
                    BoardCreds.UserAgentPass = loginInfo.UaPassword;

                    writetofile(BoardCreds, streamWriterReport);
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return BoardCreds;
        }

        private void writetofile(BoardCredentials BoardCreds, StreamWriter sw)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(BoardCreds.ModuleId).Append(",");
            //sb.Append("\"").Append(BoardCreds.BoardId).Append("\",");
            sb.Append(BoardCreds.ModuleName).Append(",");
            sb.Append(BoardCreds.LoginType).Append(",");
            sb.Append("\"").Append(BoardCreds.RETSURL).Append("\",");
            sb.Append("\"").Append(BoardCreds.Username).Append("\",");
            sb.Append("\"").Append(BoardCreds.Password).Append("\",");
            sb.Append("\"").Append(BoardCreds.UserAgent).Append("\",");
            sb.Append("\"").Append(BoardCreds.UserAgentPass).Append("\",");

            sw.WriteLine(sb.ToString());

        }

        private string getURL(string mainscript)
        {
            List<string> result = mainscript.Split('\n').ToList();

            foreach(string line in result)
            {
                if ((!line.StartsWith(";")) && (line.Contains("transmit")) && (line.Contains("http://")))
                {
                    int start =line.IndexOf("\"");
                    int end = line.LastIndexOf("\"");
                    string partline = line.Substring(start, (end - start));
                    end = partline.IndexOf("^M");
                    if (end == -1)
                    {
                        return partline;
                    }
                    return partline.Substring(0, end).Replace("\"","");
                }
            }

            return "";
        }

        private string getName(string mainscript)
        {
            List<string> result = mainscript.Split('\n').ToList();

            foreach (string line in result)
            {
                if ((!line.StartsWith(";")) && (line.Contains("transmit")) && (line.Contains("username=")))
                {
                    int start = line.IndexOf("username=") + 9 ;
                    string partline = line.Substring(start, (line.Length - start));
                    int end = partline.IndexOf("&password=");
                    if (end == -1)
                    {
                        int lineend = partline.IndexOf("^M\"");
                        if (lineend == -1)
                        {
                            return "";
                        }
                        return partline.Substring(0, lineend);
                    }
                    return partline.Substring(0, end);
                }
            }
            return "";
        }

        private string getPass(string mainscript)
        {
            List<string> result = mainscript.Split('\n').ToList();

            foreach (string line in result)
            {
                if ((!line.StartsWith(";")) && (line.Contains("transmit")) && (line.Contains("password=")))
                {
                    int start = line.IndexOf("password=") + 9;
                    string partline = line.Substring(start, (line.Length - start));
                    int end = partline.IndexOf("&username=");
                    if (end == -1)
                    {
                        int lineend = partline.IndexOf("^M\"");
                        if (lineend == -1)
                        {
                            return "";
                        }
                        return partline.Substring(0, lineend);
                    }
                    return partline.Substring(0, end);
                }
            }
            return "";
        }

        private System.String decodePassword(System.String password)
        {
            string result = password;
            try
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder(password);
                password = SupportClass.ReverseString(sb).ToString();
                //password = password + "=";
                result = net.toppro.components.mls.engine.MLSUtil.base64Decode(password);
            }
            catch (Exception exc)
            {
            }
            return result;
        }

        private string ParseSecList(string SecList, string item)
        {
            List<string> result = SecList.Split('\n').ToList();
            foreach (string line in result)
            {
                if (line.StartsWith(item))
                {
                    return line.Substring(item.Length + 1).Replace("\r","");
                }
            }
            return "";
        }

        private string GetLast(string source, int last)
        {
            return last >= source.Length ? source : source.Substring(source.Length - last);
        }

    }
}
