using System;
using System.Collections.Generic;
using System.Data.Objects;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SpeedUp.Model
{
    public class TigerLeadManager
    {
        public void GetLoginfromDB(ModuleLoginContainer compareResult, StreamWriter streamWriterReport)
        {
            var LoginInfo = new ModuleLoginContainer();
            try
            {
                bool loginInfoExistsInTCS = false;
                using (var tcsDb = new TCSEntities())
                {
                    short moduleId = Convert.ToInt16(compareResult.ModuleId);
                    if (tcsDb.tcs_rets_connection_info.Any(x => x.module_id == moduleId))
                    {
                        loginInfoExistsInTCS = true;
                        var qe = tcsDb.tcs_rets_connection_info.Where(x => x.module_id == moduleId).FirstOrDefault();
                        compareResult.LoginURL = qe.login_url;
                        compareResult.LoginUserName = qe.user_name;
                        compareResult.LoginPassword = qe.password;
                        compareResult.UserAgent = qe.user_agent;
                        compareResult.UserAgentPW = qe.ua_password;
                    }
                }
                if (!loginInfoExistsInTCS)
                {
                    using (SystemEntities se = new SystemEntities())
                    {
                        se.Connection.Open();
                        ObjectResult<spDA_RETSConnectionInfo_sel_Result> loginTrace =
                            se.spDA_RETSConnectionInfo_sel(compareResult.RdcCode, false);
                        foreach (var connInfo in loginTrace)
                        {
                            compareResult.LoginURL = connInfo.RETSLoginURL;
                            compareResult.LoginUserName = connInfo.RETSUserName;
                            compareResult.LoginPassword = connInfo.RETSPassword;
                            compareResult.UserAgent = connInfo.RETSUserAgent;
                            compareResult.UserAgentPW = connInfo.RETSUserAgentPassword;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to access DataAgg db\r\n" + ex.Message + ex.Source + ex.StackTrace);
            }

            

        }

        public TigerLeadReportRow GetClassesfromDB(TigerLeadReportRow Row, TigerLeadReportBoardCollection TLCLasses)
        {
            int MoudleInt = Convert.ToInt32(Row.ModuleID);
            List<cma_mls_board_connections> connections = null;
            using (var tcsDb = new TCSEntitiesProduction())
            {
                var board = tcsDb.cma_mls_boards.FirstOrDefault(x => x.module_id == MoudleInt && x.board_status_id == 1);
                if (board != null)
                {
                    var boardId = board.board_id;
                    connections = tcsDb.cma_mls_board_connections.Where(x => x.board_id == boardId).ToList();
                }
            }

            List<TigerLeadReportListingTypeEntry> CollectedClass =  new List<TigerLeadReportListingTypeEntry>();
            if (TLCLasses.DoesBoardExist(Row.TigerLeadCode))
            {
                CollectedClass = TLCLasses.ReturnClasses(Row.TigerLeadCode);
            }
            else if (TLCLasses.DoesBoardExist(Row.TigerLeadCodeOld))
            {
                CollectedClass = TLCLasses.ReturnClasses(Row.TigerLeadCodeOld);
            }

            List<string> matches = new List<string>();

            foreach (var connection in connections)
            {
                bool matchfound = false;

                var iniReader = new IniFile(connection.definition_file);
                var mainscriptclass = GetMainscriptClass(iniReader.GetSectionContent("MainScript"));

                foreach (var Entry in CollectedClass)
                {

                    if ((Entry.ListingType == connection.connection_type) ||
                        (Entry.ListingTable.ToLower().Contains("_" + mainscriptclass.ToLower() + "_")) || 
                        ((Entry.ListingType == "agent") && (connection.connection_type.ToLower().Contains("agent"))) ||
                        ((Entry.ListingType == "office") && (connection.connection_type.ToLower().Contains("office")))
                        )
                    {
                        matches.Add(Entry.ListingType);
                        matchfound = true;

                        var defNotAvailable = iniReader.Read("DEFNotAvailableTo", "Common") ?? "";

                        if (defNotAvailable.Equals("ORCA", StringComparison.OrdinalIgnoreCase))
                        {
                            if (Row.TPOnlyMatch=="")
                            {
                                Row.TPOnlyMatch = connection.connection_type;
                                
                            }
                            else
                            {
                                Row.TPOnlyMatch = Row.TPOnlyMatch + ":" + connection.connection_type;
                            }
                            Row.TPOnlyMatchCount++;
                        }
                        else
                        {
                            if (Row.ORCAReadyMatch == "")
                            {
                                Row.ORCAReadyMatch = connection.connection_type;
                            }
                            else
                            {
                                Row.ORCAReadyMatch = Row.ORCAReadyMatch + ":" + connection.connection_type;
                            }
                            Row.ORCAReadyMatchCount++;
                        }
                        //check if updated for orca
                        //if udpated for orca add to match orca,  if no add to match with TP only
                    }
                }

                if (matchfound == false)
                {
                    //check if updated for orca
                    //if udpated for orca add to extra orca,  if no add to match with TP extra


                    var defNotAvailable = iniReader.Read("DEFNotAvailableTo", "Common") ?? "";

                    if (defNotAvailable.Equals("ORCA", StringComparison.OrdinalIgnoreCase))
                    {
                        if (Row.TPOnlyExtra == "")
                        {
                            Row.TPOnlyExtra = connection.connection_type;
                        }
                        else
                        {
                            Row.TPOnlyExtra = Row.TPOnlyExtra + ":" + connection.connection_type;
                        }
                        Row.TPOnlyExtraCount++;
                    }
                    else
                    {
                        if (Row.ORCAReadyExtra == "")
                        {
                            Row.ORCAReadyExtra = connection.connection_type;
                        }
                        else
                        {
                            Row.ORCAReadyExtra = Row.ORCAReadyExtra + ":" + connection.connection_type;
                        }
                        Row.ORCAReadyExtraCount++;
                    }
                }
            }

            foreach (var Entry in CollectedClass)
            {
                if (!matches.Contains(Entry.ListingType))
                {
                    if (Row.TigerLeadOnly == "")
                    {
                        Row.TigerLeadOnly = Entry.ListingType;
                    }
                    else
                    {
                        Row.TigerLeadOnly = Row.TigerLeadOnly + ":" + Entry.ListingType;
                    }
                    Row.TigerLeadOnlyCount++;
                }
            }



            return Row;
        }

        private string GetMainscriptClass(string Mainscript)
        {
            var className = "";
            try
            {
                var posClass = Mainscript.LastIndexOf("Class=", StringComparison.CurrentCultureIgnoreCase);
                var posClassEnd = Mainscript.IndexOf('&', posClass);
                var posClassEnd2 = Mainscript.IndexOf('"', posClass);

                if (posClassEnd2 < posClassEnd)
                {
                    posClassEnd = posClassEnd2;
                }
                className = Mainscript.Substring(posClass + 6, posClassEnd - posClass - 6);
                
            }catch(ArgumentOutOfRangeException)
            {
                
            }

            return className;


        }

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

            result.TotalClasses++;
        }

        public void WriteToReport(StreamWriter sw, List<TigerLeadReportRow> result)
        {
            foreach (var Row in result)
            {
                var sb = new StringBuilder();
                sb.Append("\"").Append(Row.TigerLeadCode).Append("\",");
                sb.Append(Row.TigerLeadName).Append(",");
                sb.Append(Row.RDCID).Append(",");
                sb.Append(Row.ModuleID).Append(",");
                sb.Append(Row.Notes).Append(",");
                sb.Append(Row.ORCAReadyMatchCount).Append(",");
                sb.Append(Row.TPOnlyMatchCount).Append(",");
                sb.Append(Row.ORCAReadyExtraCount).Append(",");
                sb.Append(Row.TPOnlyExtraCount).Append(",");
                sb.Append(Row.TigerLeadOnlyCount).Append(",");
                sb.Append(Row.ORCAReadyMatch).Append(",");
                sb.Append(Row.TPOnlyMatch).Append(",");
                sb.Append(Row.ORCAReadyExtra).Append(",");
                sb.Append(Row.TPOnlyExtra).Append(",");
                sb.Append(Row.TigerLeadOnly).Append(",");
                sb.Append(Row.TigerLeadCodeOld).Append(",");
                sb.Append(Row.TigerLeadNameOld).Append(",");
                sw.WriteLine(sb.ToString());
            }
        }

    }


}
