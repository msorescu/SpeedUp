using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Core.Log;
using SpeedUp.Helper;
using Tcs.Mls;
using Util = SpeedUp.Helper.Util;

namespace SpeedUp.Model
{
    public class LLGapManager
    {
        private const string SearchRequest = "<TCService ClientName=\"ORCA\"><Function>GetMLSPlusStandardListings</Function><Login>{0}</Login><Board BoardId=\"{1}\" ClassName=\"{2}\"/><Search {3}=\"{4}\" BypassARAuthentication=\"{5}\" Count=\"0\" RecordsLimit=\"10\" SearchLayout=\"Advanced search\"></Search><StandardSearch></StandardSearch></TCService>";
        private const string SearchTMKCountRequest = "<TCService ClientName=\"ORCA\"><Function>GetMLSPlusStandardListings</Function><Login>{0}</Login><Board BoardId=\"{1}\" ClassName=\"{2}\"/><Search {3}=\"{4}\" BypassARAuthentication=\"{5}\" Count=\"1\" SearchLayout=\"Advanced search\"></Search><StandardSearch></StandardSearch></TCService>";

        private const string SearchOfficeRequest =
            "<TCService ClientName=\"ORCA\"><Function>GetOfficeRoster</Function><Login>{0}</Login><Board BoardId=\"{1}\" MetadataVersion=\"0\"/><Search BypassARAuthentication=\"1\" Count=\"0\" RecordsLimit=\"200\" ReturnDataAggListingsXML=\"0\"/></TCService>";

        private const string SearchAgentRequest =
            "<TCService ClientName=\"ORCA\"><Function>GetAgentRoster</Function><Login>{0}</Login><Board BoardId=\"{1}\" MetadataVersion=\"0\"/><Search BypassARAuthentication=\"1\" Count=\"0\" RecordsLimit=\"200\" ReturnDataAggListingsXML=\"0\"/></TCService>";

        private const string SearchRequestListingCount = "<TCService ClientName=\"ORCA\"><Function>GetDataAggListings</Function><Login>{0}</Login><Board ModuleId=\"{1}\" MetadataVersion=\"0\"/><Search BypassARAuthentication=\"1\" ST_PublicListingStatus=\"A\" Count=\"1\" DACount=\"0\" ST_LastMod=\"{2}\"/></TCService>";

        private const string LoginNodeText = "<password>{0}</password><password>{1}</password><UserAgent>{2}</UserAgent><RetsUAPwd>{3}</RetsUAPwd>";
        //private const string SearchRequest = "<TCService ClientName=\"ORCA\"><Function>GetMLSPlusStandardListings</Function><Login><password>{0}</password><password>{1}</password><UserAgent>{2}</UserAgent><RetsUAPwd>{3}</RetsUAPwd></Login><Board BoardId=\"{4}\" ClassName=\"{5}\"/><Search {6}=\"{7}\" {8}=\"{9}\" BypassARAuthentication=\"1\" Count=\"0\" RecordsLimit=\"10\" SearchLayout=\"Advanced search\"></Search><StandardSearch></StandardSearch></TCService>";
        private LoginInfo _loginTrace;
        List<String> ListingSys = new List<string>();
        List<String> ListingRLN = new List<string>();
        List<String> ListingMap = new List<string>();
        List<String> OfficeSys = new List<string>();
        List<String> OfficeRLN = new List<string>();
        List<String> OfficeMap = new List<string>();
        List<String> AgentSys = new List<string>();
        List<String> AgentRLN = new List<string>();
        List<String> AgentMap = new List<string>();
        private List<String> AdditionalLatSys = new List<string>();
        private List<String> AdditionalLongSys = new List<string>();

        public void CompareTcsRdcActivePendingStatus(LLGapCompareResults compareResult, StreamWriter streamWriterReport,
            List<String> ListingSysIn, List<String> ListingRLNIn, List<String> ListingMapIn, List<String> OfficeSysIn, List<String> OfficeRLNIn, List<String> OfficeMapIn, List<String> AgentSysIn, List<String> AgentRLNIn, List<String> AgentMapIn)
        {
            ListingSys = ListingSysIn;
            ListingRLN = ListingRLNIn;
            ListingMap = ListingMapIn;
            OfficeSys = OfficeSysIn;
            OfficeRLN = OfficeRLNIn;
            OfficeMap = OfficeMapIn;
            AgentSys = AgentSysIn;
            AgentRLN = AgentRLNIn;
            AgentMap = AgentMapIn;

            try
            {
                var helper = new LLGapHelper();
                var connections = helper.GetDefConnections(compareResult.ModuleId);
                var resultByPropertyClass = new LLGapCompareResults
                {
                    ModuleId = compareResult.ModuleId,
                    ModuleName = compareResult.ModuleName,
                    RdcCode = compareResult.RdcCode
                };

                compareResult.TotalClasses = 0;
                compareResult.TotalLatLongMapped = 0;
                var index = 0;
                foreach (var connection in connections)
                {
                    index++;
                    Console.WriteLine("class " + index + " of " + connections.Count);
                    _loginTrace = null;
                    if (connection.connection_name.EndsWith("oh.sql", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    //{
                    //    if (!connection.connection_name.EndsWith("da.sql", StringComparison.OrdinalIgnoreCase)) continue;
                    }
                    if (connection.connection_name.EndsWith("ag.sql", StringComparison.OrdinalIgnoreCase))
                    {
                        resultByPropertyClass.IsAgent = true;
                        resultByPropertyClass.IsOffice = false;
                        //{
                        //    if (!connection.connection_name.EndsWith("da.sql", StringComparison.OrdinalIgnoreCase)) continue;
                    }
                    else if (connection.connection_name.EndsWith("of.sql", StringComparison.OrdinalIgnoreCase))
                    {
                        resultByPropertyClass.IsAgent = false;
                        resultByPropertyClass.IsOffice = true;
                        //{
                        //    if (!connection.connection_name.EndsWith("da.sql", StringComparison.OrdinalIgnoreCase)) continue;
                    }
                    else
                    {
                        resultByPropertyClass.IsAgent = false;
                        resultByPropertyClass.IsOffice = false;
                    }





                        resultByPropertyClass.PropertyClass = connection.connection_type;
                        resultByPropertyClass.ConnectionName = connection.connection_name;
                        resultByPropertyClass.DefFilePath = connection.definition_file;
                        resultByPropertyClass.BoardId = connection.board_id ?? 0;

                    helper.AnalyzeDef(ref resultByPropertyClass);
                    if (resultByPropertyClass.IsNotAvailableForOrca || resultByPropertyClass.IsOpenHouseOnlyModule)
                        continue;

                    if(!File.Exists(Path.Combine(string.Format(@"{0}\TCDEF\TCDEF_a.1_Main\",ConfigurationManager.AppSettings["Drive"]),compareResult.ModuleId.ToString(),"metadata.xml")))
                    {
                        DownloadMetadata(ref resultByPropertyClass);
                    }

                    if (File.Exists(Path.Combine(string.Format(@"{0}\TCDEF\TCDEF_a.1_Main\",ConfigurationManager.AppSettings["Drive"]), compareResult.ModuleId.ToString(), "metadata.xml")))
                    {
                        var iniReader = new IniFile(connection.definition_file);
                        var mainscriptclass = GetMainscriptClass(iniReader.GetSectionContent("MainScript"));
                        possibleLatLongInMeta(mainscriptclass,
                                                          Path.Combine(string.Format(@"{0}\TCDEF\TCDEF_a.1_Main\",ConfigurationManager.AppSettings["Drive"]),
                                                                       compareResult.ModuleId.ToString(),
                                                                       "metadata.xml"), ref resultByPropertyClass);

                        var filepathorig =
                            Path.Combine(string.Format(@"{0}\TCDEF\TCDEF_a.1_Main\",ConfigurationManager.AppSettings["Drive"]), compareResult.ModuleId.ToString(), resultByPropertyClass.ConnectionName);
                        //var newwork = new WorkNow();
                        var filePath = filepathorig.Substring(0, filepathorig.Length - 4);
                        filePath = filePath + "_grid.dat";
                        if (!File.Exists(filePath))
                        {
                            DownloadSampleData(ref resultByPropertyClass);
                        }

                        if (File.Exists(filePath))
                        {
                            possibleURLinData(filePath, ref resultByPropertyClass);

                            //if (!LatLonginData)
                            //{
                            //    if (string.IsNullOrEmpty(resultByPropertyClass.RawDataInClasses))
                            //    {
                            //        resultByPropertyClass.RawDataInClasses = "N";
                            //    }
                            //    else if (resultByPropertyClass.RawDataInClasses == "Y")
                            //    {
                            //        resultByPropertyClass.RawDataInClasses = "P";
                            //    }
                            //}
                            //else
                            //{
                            //    if (string.IsNullOrEmpty(resultByPropertyClass.RawDataInClasses))
                            //    {
                            //        resultByPropertyClass.RawDataInClasses = "Y";
                            //    }
                            //    else if (resultByPropertyClass.RawDataInClasses == "N")
                            //    {
                            //        resultByPropertyClass.RawDataInClasses = "P";
                            //    }
                            //}

                        }
                        else
                        {
                            //no data
                        }
                        //AdditionalLatSys.Clear();
                        //AdditionalLongSys.Clear();

                    }

                    //var iniReader = new IniFile(connection.definition_file);
                    //var mainscriptclass = GetMainscriptClass(iniReader.GetSectionContent("MainScript"));

                    

                   // CheckRdcAccountAccess(ref resultByPropertyClass);
                    if (resultByPropertyClass.IsDataSourceRemoved)
                        continue;

                    //GetTraceLogin(ref compareResult);
                    //if (_loginTrace != null)
                    //    GetListCount(ref resultByPropertyClass);

                    //break;
                }

                ReportAnalysis(ref resultByPropertyClass);
                helper.WriteToLine(streamWriterReport, resultByPropertyClass);
            }
            catch (Exception ex)
            {
                Log.Error(typeof(LLGapManager), "Module ID = " + compareResult.ModuleId + "\r\n" + ex.Message + ex.StackTrace);
            }
        }

        private void ReportAnalysis(ref LLGapCompareResults result)
        {
            ///////////////
            if(result.STDFListingBrokerageURLCount == 0)
            {
                result.STDFListingBrokerageURLStatus = result.STDFListingBrokerageURLStatus + " No Data";
            }
            else
            {
                if (result.STDFListingBrokerageURLStatus == null)
                {
                    result.URLMappingOpp = "Yes";
                }
                result.STDFListingBrokerageURLStatus = result.STDFListingBrokerageURLStatus + " With Data";
            }
            if (result.STDFListingDetailsPageURLCount == 0)
            {
                result.STDFListingDetailsPageURLStatus = result.STDFListingDetailsPageURLStatus + "No Data";
            }
            else
            {
                if (result.STDFListingDetailsPageURLStatus == null)
                {
                    result.URLMappingOpp = "Yes";
                }
                result.STDFListingDetailsPageURLStatus = result.STDFListingDetailsPageURLStatus + " With Data";
            }
            if (result.STDFListingFranchiseURLCount == 0)
            {
                result.STDFListingFranchiseURLStatus = result.STDFListingFranchiseURLStatus + " No Data";
            }
            else
            {
                if (result.STDFListingFranchiseURLStatus == null)
                {
                    result.URLMappingOpp = "Yes";
                }
                result.STDFListingFranchiseURLStatus = result.STDFListingFranchiseURLStatus + " With Data";
            }
            if (result.STDFListingOfficeURLCount == 0)
            {
                result.STDFListingOfficeURLStatus = result.STDFListingOfficeURLStatus + " No Data";
            }
            else
            {
                if (result.STDFListingOfficeURLStatus == null)
                {
                    result.URLMappingOpp = "Yes";
                }
                result.STDFListingOfficeURLStatus = result.STDFListingOfficeURLStatus + " With Data";
            }


            ///////////////////
            if (result.AG_BrokerageURLCount == 0)
            {
                result.AG_BrokerageURLStatus = result.AG_BrokerageURLStatus + " No Data";
            }
            else
            {
                if (result.AG_BrokerageURLStatus == null)
                {
                    result.URLMappingOpp = "Yes";
                }
                result.AG_BrokerageURLStatus = result.AG_BrokerageURLStatus + " With Data";
            }
            if (result.AG_FranchiseURLCount == 0)
            {
                result.AG_FranchiseURLStatus = result.AG_FranchiseURLStatus + " No Data";
            }
            else
            {
                if (result.AG_FranchiseURLStatus == null)
                {
                    result.URLMappingOpp = "Yes";
                }
                result.AG_FranchiseURLStatus = result.AG_FranchiseURLStatus + " With Data";
            }
            if (result.AG_OfficeURLCount == 0)
            {
                result.AG_OfficeURLStatus = result.AG_OfficeURLStatus + " No Data";
            }
            else
            {
                if (result.AG_OfficeURLStatus == null)
                {
                    result.URLMappingOpp = "Yes";
                }
                result.AG_OfficeURLStatus = result.AG_OfficeURLStatus + " With Data";
            }

            //////////////////
            if (result.OF_BrokerageURLCount == 0)
            {
                result.OF_BrokerageURLStatus = result.OF_BrokerageURLStatus + " No Data";
            }
            else
            {
                if (result.OF_BrokerageURLStatus == null)
                {
                    result.URLMappingOpp = "Yes";
                }
                result.OF_BrokerageURLStatus = result.OF_BrokerageURLStatus + " With Data";
            }
            if (result.OF_FranchiseURLCount == 0)
            {
                result.OF_FranchiseURLStatus = result.OF_FranchiseURLStatus + " No Data";
            }
            else
            {
                if (result.OF_FranchiseURLStatus == null)
                {
                    result.URLMappingOpp = "Yes";
                }
                result.OF_FranchiseURLStatus = result.OF_FranchiseURLStatus + " With Data";
            }
            if (result.OF_WebsiteURLCount == 0)
            {
                result.OF_WebsiteURLStatus = result.OF_WebsiteURLStatus + " No Data";
            }
            else
            {
                if (result.OF_WebsiteURLStatus == null)
                {
                    result.URLMappingOpp = "Yes";
                }
                result.OF_WebsiteURLStatus = result.OF_WebsiteURLStatus + " With Data";
            }
        }



        private void GetListCount(ref LLGapCompareResults result)
        {
            try
            {
                var request = string.Format(SearchRequestListingCount, GetLoginText(_loginTrace), result.ModuleId,
                                            DateTime.Now.AddYears(-10).ToString("MM/dd/yyyyTHH:mm:ss") + "-" +
                                            DateTime.Now.ToString("MM/dd/yyyyTHH:mm:ss"));
                result.QaListingCount = LLGapHelper.GetListingCount(request, ConfigurationManager.AppSettings["TcsQaUrl"]);
                result.ProdutionListingCount = LLGapHelper.GetListingCount(request, "");

                if (result.QaListingCount > 0 && result.ProdutionListingCount > 0)
                {
                    result.IncreaseInActiveCounts = ((int)(0.5f + 100f * (result.QaListingCount - result.ProdutionListingCount) / result.ProdutionListingCount)).ToString(CultureInfo.InvariantCulture) + "%";
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
                Log.Error(typeof(LLGapManager), "Failed to get list count for Module ID: " + result.ModuleId + ex.Message + ex.StackTrace);
            }
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

            }
            catch (ArgumentOutOfRangeException)
            {

            }

            return className;

        }

        private void possibleLatLongInMeta(string classname, string metadatapath,ref LLGapCompareResults result)
        {
            List<MlsMetadataField> LatCandidates = new List<MlsMetadataField>();
            List<MlsMetadataField> LongCandidates = new List<MlsMetadataField>();
            Metadata ClassDefinition = new Metadata(metadatapath,classname);
            List<MlsMetadataField> ClassFields = ClassDefinition.GetAllFieldsByClassName(classname);
            List<String> RLNs = new List<String>();
            List<String> SysNames = new List<String>();
            List<String> toRemove = new List<String>();

            foreach (var mlsMetadataField in ClassFields)
            {
                RLNs.Add(mlsMetadataField.LongName);
                SysNames.Add(mlsMetadataField.SystemName);
            }

            if (result.IsAgent)
            {
                if(AgentRLN.Count > 0)
                {
                    int index = 0;

                    foreach (var fieldRLN in AgentRLN)
                    {
                        index++;
                        if(!RLNs.Contains(fieldRLN))
                        {
                            if (AgentMap[index - 1] == "AG_OfficeURL")
                            {
                                result.AG_OfficeURLStatus = "No Metadata";
                            }
                            if (AgentMap[index - 1] == "AG_BrokerageURL")
                            {
                                result.AG_BrokerageURLStatus = "No Metadata";
                            }
                            if (AgentMap[index - 1] == " AG_FranchiseURL")
                            {
                                result.AG_FranchiseURLStatus = "No Metadata";
                            }

                            toRemove.Add(fieldRLN);
                        }

                    }
                    AgentRLN.RemoveAll(x => toRemove.Equals(x));
                    AgentMap.RemoveAll(x => toRemove.Equals(x));
                    toRemove.Clear();

                }
                else if (AgentSys.Count > 0)
                {
                    int index = 0;
                    foreach (var fieldSys in AgentSys)
                    {
                        index++;
                        if (!SysNames.Contains(fieldSys))
                        {
                            if (AgentMap[index - 1] == "AG_OfficeURL")
                            {
                                result.AG_OfficeURLStatus = "No Metadata";
                            }
                            if (AgentMap[index - 1] == "AG_BrokerageURL")
                            {
                                result.AG_BrokerageURLStatus = "No Metadata";
                            }
                            if (AgentMap[index - 1] == " AG_FranchiseURL")
                            {
                                result.AG_FranchiseURLStatus = "No Metadata";
                            }

                            toRemove.Add(fieldSys);
                        }

                    }
                    AgentSys.RemoveAll(x => toRemove.Equals(x));
                    AgentMap.RemoveAll(x => toRemove.Equals(x));
                    toRemove.Clear();
                }
            }
            else if (result.IsOffice)
            {
                if (OfficeRLN.Count > 0)
                {
                    int index = 0;
                    foreach (var fieldRLN in OfficeRLN)
                    {
                        index++;
                        if (!RLNs.Contains(fieldRLN))
                        {
                            if (OfficeMap[index - 1] == "OF_WebsiteURL")
                            {
                                result.OF_WebsiteURLStatus = "No Metadata";
                            }
                            if (OfficeMap[index - 1] == "OF_BrokerageURL")
                            {
                                result.OF_BrokerageURLStatus = "No Metadata";
                            }
                            if (OfficeMap[index - 1] == " OF_FranchiseURL")
                            {
                                result.OF_FranchiseURLStatus = "No Metadata";
                            }

                            toRemove.Add(fieldRLN);

                        }

                    }
                    OfficeRLN.RemoveAll(x => toRemove.Equals(x));
                    OfficeMap.RemoveAll(x => toRemove.Equals(x));
                    toRemove.Clear();

                }
                else if (OfficeSys.Count > 0)
                {
                    int index = 0;
                    foreach (var fieldSys in OfficeSys)
                    {
                        index++;
                        if (!SysNames.Contains(fieldSys))
                        {
                            if (OfficeMap[index - 1] == "OF_WebsiteURL")
                            {
                                result.OF_WebsiteURLStatus = "No Metadata";
                            }
                            if (OfficeMap[index - 1] == "OF_BrokerageURL")
                            {
                                result.OF_BrokerageURLStatus = "No Metadata";
                            }
                            if (OfficeMap[index - 1] == " OF_FranchiseURL")
                            {
                                result.OF_FranchiseURLStatus = "No Metadata";
                            }

                            toRemove.Add(fieldSys);

                        }


                    }
                    OfficeSys.RemoveAll(x => toRemove.Equals(x));
                    OfficeMap.RemoveAll(x => toRemove.Equals(x));
                    toRemove.Clear();
                }
            }
            else
            {
                if (ListingRLN.Count > 0)
                {
                    int index = 0;
                    foreach (var fieldRLN in ListingRLN)
                    {
                        index++;
                        if (!RLNs.Contains(fieldRLN))
                        {
                            if (ListingMap[index - 1] == "STDFListingDetailsPageURL")
                            {
                                result.STDFListingDetailsPageURLStatus = "No Metadata";
                            }
                            if (ListingMap[index - 1] == "STDFListingOfficeURL")
                            {
                                result.STDFListingOfficeURLStatus = "No Metadata";
                            }
                            if (ListingMap[index - 1] == "STDFListingBrokerageURL")
                            {
                                result.STDFListingBrokerageURLStatus = "No Metadata";
                            }
                            if (ListingMap[index - 1] == "STDFListingFranchiseURL")
                            {
                                result.STDFListingFranchiseURLStatus = "No Metadata";
                            }
                            toRemove.Add(fieldRLN);

                        }


                    }
                    ListingRLN.RemoveAll(x => toRemove.Equals(x));
                    ListingMap.RemoveAll(x => toRemove.Equals(x));
                    toRemove.Clear();

                }
                else if (ListingSys.Count > 0)
                {
                    int index = 0;
                    foreach (var fieldSys in ListingSys)
                    {
                        index++;
                        if (!SysNames.Contains(fieldSys))
                        {
                            if (ListingMap[index - 1] == "STDFListingDetailsPageURL")
                            {
                                result.STDFListingDetailsPageURLStatus = "No Metadata";
                            }
                            if (ListingMap[index - 1] == "STDFListingOfficeURL")
                            {
                                result.STDFListingOfficeURLStatus = "No Metadata";
                            }
                            if (ListingMap[index - 1] == "STDFListingBrokerageURL")
                            {
                                result.STDFListingBrokerageURLStatus = "No Metadata";
                            }
                            if (ListingMap[index - 1] == "STDFListingFranchiseURL")
                            {
                                result.STDFListingFranchiseURLStatus = "No Metadata";
                            }

                            toRemove.Add(fieldSys);

                        }


                    }

                    ListingSys.RemoveAll(x => toRemove.Equals(x));
                    ListingMap.RemoveAll(x => toRemove.Equals(x));
                    toRemove.Clear();
                }
            }

            /*
            foreach (var mlsMetadataField in ClassFields)
            {
                if(LatRLN.Contains(mlsMetadataField.LongName))
                {
                    LatCandidates.Add(mlsMetadataField);
                    AdditionalLatSys.Add(mlsMetadataField.SystemName);
                }
                else if(LatSys.Contains(mlsMetadataField.SystemName))
                {
                    LatCandidates.Add(mlsMetadataField);
                }
                if (LongRLN.Contains(mlsMetadataField.LongName))
                {
                    LongCandidates.Add(mlsMetadataField);
                    AdditionalLongSys.Add(mlsMetadataField.SystemName);
                }
                else if(LongSys.Contains(mlsMetadataField.SystemName))
                {
                    LongCandidates.Add(mlsMetadataField);
                }
            }*/

            /*
            if ((LongCandidates.Count > 0)&&(LatCandidates.Count >0))
            {
                if (string.IsNullOrEmpty(result.CandidateDEFs))
                {
                    result.CandidateDEFs = result.PropertyClass;
                }
                else
                {
                    result.CandidateDEFs = result.CandidateDEFs + "," + result.PropertyClass;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(result.NonCandidateDEFs))
                {
                    result.NonCandidateDEFs = result.PropertyClass;
                }
                else
                {
                    result.NonCandidateDEFs = result.NonCandidateDEFs + "," + result.PropertyClass;
                }
            }*/

        }

        private bool possibleLatLonginData(string datapath,ref LLGapCompareResults result )
        {

            //var MergedLatSys = AdditionalLatSys.Union(LatSys).ToList();
            //var MergedLongSys = AdditionalLongSys.Union(LongSys).ToList();
            var MergedLatSys = AdditionalLatSys.Union(AgentRLN).ToList();
            var MergedLongSys = AdditionalLongSys.Union(AgentMap).ToList();
            var reader = new StreamReader(File.OpenRead(datapath));
            var PossibleLatPositions = new List<int>();
            var PossibleLongPositions = new List<int>();
            bool LatYes = false;
            bool LongYes = false;
            bool LatLongYes = false;

            int rowcount = 0;
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (rowcount < 10)
                {

                    var values = line.Split('\t');
                    if(values[0] == "<COLUMNS>")
                    {
                        int count = 0;
                        foreach (var header in values)
                        {
                            if (MergedLatSys.Contains(header))
                            {
                                PossibleLatPositions.Add(count);
                            }else if (MergedLongSys.Contains(header))
                            {
                                PossibleLongPositions.Add(count);
                            }
                            count++;
                        }
                    }

                    if (!LatLongYes)
                    {
                        if (values[0] == "<DATA>")
                        {
                            if (!LatYes)
                            {
                                foreach (var pos in PossibleLatPositions)
                                {
                                    if (pos < values.Length)
                                    {
                                        if (values[pos] != "")
                                        {
                                            LatYes = true;
                                        }
                                    }
                                }
                            }
                            if (!LongYes)
                            {
                                foreach (var pos in PossibleLongPositions)
                                {
                                    if (pos < values.Length)
                                    {
                                        if (values[pos] != "")
                                        {
                                            LongYes = true;
                                        }
                                    }
                                }
                            }
                            
                        }
                        if (LongYes&&LatYes)
                        {
                            return true;
                        }
                    }
                    rowcount++;
                }

            }

            return false;

        }

        private void possibleURLinData(string datapath, ref LLGapCompareResults result)
        {
            var reader = new StreamReader(File.OpenRead(datapath));
            var PossibleLatPositions = new List<int>();
            var PossibleLongPositions = new List<int>();
            int indexOfficeURL = -1;
            int indexBrokerUrl = -1;
            int indexFranchise = -1;
            int indexLDP = -1;
            bool LatYes = false;
            bool LongYes = false;
            bool LatLongYes = false;
            bool startrecordcount = false;
            int recordcount = 0;
            int hitcountOfficeURL = 0;
            int hitcountBrokerUrl = 0;
            int hitcountFranchise = 0;
            int hitcountLDP = 0;

            int rowcount = 0;
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (rowcount < 200)
                {

                    var values = line.Split('\t');
                    if (values[0].Trim() == "<COLUMNS>")
                    {
                        int count = 0;
                        foreach (var header in values)
                        {
                            if (result.IsAgent)
                            {
                                if (AgentSys.Contains(header))
                                {
                                    int temp = AgentSys.IndexOf(header);
                                    if (AgentMap[temp] == "AG_OfficeURL")
                                    {
                                        indexOfficeURL = count;
                                    }
                                    if (AgentMap[temp] == "AG_BrokerageURL")
                                    {
                                        indexBrokerUrl = count;
                                    }
                                    if (AgentMap[temp] == "AG_FranchiseURL")
                                    {
                                        indexFranchise = count;
                                    }
                                }
                            }else if (result.IsOffice)
                            {
                                if (OfficeSys.Contains(header))
                                {
                                    int temp = OfficeSys.IndexOf(header);
                                    if (OfficeMap[temp] == "OF_WebsiteURL")
                                    {
                                        indexOfficeURL = count;
                                    }
                                    if (OfficeMap[temp] == "OF_BrokerageURL")
                                    {
                                        indexBrokerUrl = count;
                                    }
                                    if (OfficeMap[temp] == "OF_FranchiseURL")
                                    {
                                        indexFranchise = count;
                                    }
                                }
                            }else
                                if (ListingSys.Contains(header))
                                {
                                    if (ListingSys.Contains(header))
                                    {
                                        int temp = ListingSys.IndexOf(header);
                                        if (ListingMap[temp] == "STDFListingDetailsPageURL")
                                        {
                                            indexLDP = count;
                                        }
                                        if (ListingMap[temp] == "STDFListingOfficeURL")
                                        {
                                            indexOfficeURL = count;
                                        }
                                        if (ListingMap[temp] == "STDFListingBrokerageURL")
                                        {
                                            indexBrokerUrl = count;
                                        }
                                        if (ListingMap[temp] == "STDFListingFranchiseURL")
                                        {
                                            indexFranchise = count;
                                        }
                                    }
                                }
                            count++;
                        }
                        startrecordcount = true;

                    }

                    //if (!LatLongYes)
                    //{
                    //    if (values[0] == "<DATA>")
                    //    {
                    //        if (!LatYes)
                    //        {
                    //            foreach (var pos in PossibleLatPositions)
                    //            {
                    //                if (pos < values.Length)
                    //                {
                    //                    if (values[pos] != "")
                    //                    {
                    //                        LatYes = true;
                    //                    }
                    //                }
                    //            }
                    //        }
                    //        if (!LongYes)
                    //        {
                    //            foreach (var pos in PossibleLongPositions)
                    //            {
                    //                if (pos < values.Length)
                    //                {
                    //                    if (values[pos] != "")
                    //                    {
                    //                        LongYes = true;
                    //                    }
                    //                }
                    //            }
                    //        }

                    //    }
                    //    if (LongYes && LatYes)
                    //    {
                    //    }
                    //}
                    if(startrecordcount)
                    {
                        if (values[0].Trim() == "<DATA>")
                        {
                            if(indexBrokerUrl != -1)
                            {
                                if (values[indexBrokerUrl] != "")
                                {
                                    if (result.IsAgent)
                                    {
                                        result.AG_BrokerageURLCount++;
                                    }else if (result.IsOffice)
                                    {
                                        result.OF_BrokerageURLCount++;
                                    }else
                                    {
                                        result.STDFListingBrokerageURLCount++;
                                    }
                                }
                            }
                            if (indexFranchise != -1)
                            {
                                if (values[indexFranchise] != "")
                                {
                                    if (result.IsAgent)
                                    {
                                        result.AG_FranchiseURLCount++;
                                    }
                                    else if (result.IsOffice)
                                    {
                                        result.OF_FranchiseURLCount++;
                                    }
                                    else
                                    {
                                        result.STDFListingFranchiseURLCount++;
                                    }
                                }
                            }
                            if (indexLDP != -1)
                            {
                                if (values[indexLDP] != "")
                                {
                                    if (result.IsAgent)
                                    {
                                        
                                    }
                                    else if (result.IsOffice)
                                    {
                                        
                                    }
                                    else
                                    {
                                        result.STDFListingDetailsPageURLCount++;
                                    }
                                }
                            }
                            if (indexOfficeURL != -1)
                            {
                                if (values[indexOfficeURL] != "")
                                {
                                    if (result.IsAgent)
                                    {
                                        result.AG_OfficeURLCount++;
                                    }
                                    else if (result.IsOffice)
                                    {
                                        result.OF_WebsiteURLCount++;
                                    }
                                    else
                                    {
                                        result.STDFListingOfficeURLCount++;
                                    }
                                }
                            }

                                recordcount++;

                        }
                        

                    }
                    rowcount++;
                }

            }

            if (result.IsAgent)
            {
                result.AgentCount = recordcount;
            }
            else if (result.IsOffice)
            {
                result.OfficeCount = recordcount;
            }
            else
            {
                result.ListingCount = result.ListingCount + recordcount;
            }

            if (!ListingMap.Contains("STDFListingDetailsPageURL"))
            {
                result.STDFListingDetailsPageURLCount = -1;
                result.STDFListingDetailsPageURLStatus = "No Template";
            }
            if (!ListingMap.Contains("STDFListingOfficeURL"))
            {
                result.STDFListingOfficeURLCount = -1;
                result.STDFListingOfficeURLStatus = "No Template";
            }
            if (!ListingMap.Contains("STDFListingBrokerageURL"))
            {
                result.STDFListingBrokerageURLCount = -1;
                result.STDFListingBrokerageURLStatus = "No Template";
            }
            if (!ListingMap.Contains("STDFListingFranchiseURL"))
            {
                result.STDFListingFranchiseURLCount = -1;
                result.STDFListingFranchiseURLStatus = "No Template";
            }

            if (!OfficeMap.Contains("OF_WebsiteURL"))
            {
                result.OF_WebsiteURLCount = -1;
                result.OF_WebsiteURLStatus = "No Template";
            }
            if (!OfficeMap.Contains("OF_BrokerageURL"))
            {
                result.OF_BrokerageURLCount = -1;
                result.OF_BrokerageURLStatus = "No Template";
            }
            if (!OfficeMap.Contains("OF_FranchiseURL"))
            {
                result.OF_FranchiseURLCount = -1;
                result.OF_FranchiseURLStatus = "No Template";
            }

            if (!AgentMap.Contains("AG_OfficeURL"))
            {
                result.AG_OfficeURLCount = -1;
                result.AG_OfficeURLStatus = "No Template";
            }
            if (!AgentMap.Contains("AG_BrokerageURL"))
            {
                result.AG_BrokerageURLCount = -1;
                result.AG_BrokerageURLStatus = "No Template";
            }
            if (!AgentMap.Contains("AG_FranchiseURL"))
            {
                result.AG_FranchiseURLCount = -1;
                result.AG_FranchiseURLStatus = "No Template";
            }

        }

        private void DownloadMetadata(ref LLGapCompareResults compareResult)
        {
            try
            {
                GetTraceLogin(ref compareResult);
                if (compareResult.IsDataSourceRemoved)
                    return;

                Util.DownloadMlsMetadata(string.Format(@"{0}\TCDEF\TCDEF_a.1_Main\",ConfigurationManager.AppSettings["Drive"]), compareResult.ModuleId.ToString(), compareResult.DefFilePath,
                                         _loginTrace.UserName, _loginTrace.Password, (String.IsNullOrEmpty(_loginTrace.UserAgent)) ? "" : _loginTrace.UserAgent,
                                         (String.IsNullOrEmpty(_loginTrace.UaPassword)) ? "" : _loginTrace.UaPassword);
            }
                        catch (Exception ex)
                        {
                            Log.Error(typeof(LLGapManager), ex.Message + ex.StackTrace);
                        }
        }

        private void DownloadSampleData(ref LLGapCompareResults result)
        {
            try
            {
                var filepathorig =
                    Path.Combine(string.Format(@"{0}\TCDEF\TCDEF_a.1_Main\",ConfigurationManager.AppSettings["Drive"]), result.ModuleId.ToString(), result.ConnectionName);
                //var newwork = new WorkNow();
                var filePath = filepathorig.Substring(0, filepathorig.Length - 4);
                //newwork.GetSampleDataForSingleDef(result.DefFilePath,
                //    filePath + "_grid.dat",
                //    filePath + "_comm.log",
                //    filePath + "_tcs.xml");


                var searchEngine = new SearchEngine();
                searchEngine.IsDebug = true;
                searchEngine.BoardID = 999999;

                var defPath = filepathorig;
                var resultFolder = GetTempResultFolder();
                var defPathInTempResultFolder = System.IO.Path.Combine(resultFolder, result.ConnectionName);
                File.Copy(defPath, defPathInTempResultFolder, true);

                GetTraceLogin(ref result);

                if ((String.IsNullOrEmpty(_loginTrace.UserAgent)))
                {
                    _loginTrace.UserAgent = "";
                }
                if ((String.IsNullOrEmpty(_loginTrace.UaPassword)))
                {
                    _loginTrace.UaPassword = "";
                }
                var searchRequestXml = RequestXmlHelper.GetRequestXmlForSampleData(defPathInTempResultFolder,_loginTrace,"", false);
                var result2 = searchEngine.RunClientRequest(searchRequestXml);

                var fileList = new DirectoryInfo(resultFolder).GetFiles("*.dat", SearchOption.AllDirectories);

                fileList.First().CopyTo(filePath + "_grid.dat", true);

                var commnicationFile = new DirectoryInfo(resultFolder).GetFiles("*.log", SearchOption.AllDirectories);
                {
                    var fileName = commnicationFile.First().FullName;
                    File.Copy(fileName, filePath + "_comm.log", true);
                }
            }
            catch (Exception ex)
            {
                Log.Error(typeof(LLGapManager), ex.Message + ex.StackTrace);
            }
        }

        private string GetTempResultFolder()
        {
            var datetimeFolderName = DateTime.Now.ToString("yyyyMMddhhmmss");
            string runningDirectory = string.Format(@"{0}\speedup",ConfigurationManager.AppSettings["Drive"]); //System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string resultFolder = System.IO.Path.Combine(System.IO.Path.Combine(runningDirectory, "Result"), datetimeFolderName);
            Directory.CreateDirectory(resultFolder);
            return resultFolder;
        }

        private void CheckRdcAccountAccess(ref LLGapCompareResults compareResult)
        {
            try
            {
                GetTraceLogin(ref compareResult);
                if (compareResult.IsDataSourceRemoved)
                    return;

                if (string.IsNullOrEmpty(compareResult.PendingGap))
                {
                //    compareResult.TestResult = LLGapConstants.NoGapExisted;
                //    return;
                }

                if (_loginTrace != null)
                {
                    var helper = new LLGapHelper();

                    var activestatusparsed = compareResult.RdcActive.Split(new[] {','},
                                                                          StringSplitOptions.RemoveEmptyEntries);

                    var firstactivestatus = activestatusparsed[0];

                        var returnCode = SearchByTmkRequest(compareResult, firstactivestatus, _loginTrace);
                        if (returnCode == Enums.CheckAccountReturnCode.RequestFailed60310)
                        {
                            returnCode = SearchByTmkRequest(compareResult, firstactivestatus, _loginTrace);
                            if (returnCode == Enums.CheckAccountReturnCode.RequestFailed60310)
                                returnCode = SearchByTmkRequest(compareResult, firstactivestatus, _loginTrace);
                        }
                        ProcessSearchResult(returnCode, ref compareResult, firstactivestatus);
                    
                    helper.AnlyzeAccountAccessResult(ref compareResult);
                    if (!string.IsNullOrEmpty(compareResult.StatusRdcAccountReturnNoRecord))
                    {
                        foreach (
                            var item in
                                (compareResult.StatusRdcAccountReturnNoRecord).Split(
                                     new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Distinct())
                        {
                            //CheckTpAccountAccess(ref compareResult, item);
                        }
                        compareResult.StatusHasNoListing = LLGapHelper.FindGap(
                            compareResult.TpHasAccessStatusInRemainGap, compareResult.StatusRdcAccountReturnNoRecord);
                    }
                    else
                    {
                        compareResult.IsTpAccountLoginSuccess = true;
                    }
                }
                else
                {
                    compareResult.TestResult = LLGapConstants.ResultError + " -- Cannot retrieve RDC credential";
                }
            }
            catch (Exception ex)
            {
                Log.Error(typeof(LLGapManager), ex.Message + ex.StackTrace);
            }
        }

        private void CheckTpAccountAccess(ref LLGapCompareResults compareResult, string status)
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
                    if (loginInfo != null && !string.IsNullOrEmpty(loginInfo.UserName))
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

        private Enums.CheckAccountReturnCode SearchByTmkRequest(LLGapCompareResults compareResult, string status, LoginInfo loginInfo)
        {
            string statusName;
            string statusValue;
            string lastModidfyFieldName;
            string lastMoidfyFieldValue;
            var helper = new LLGapHelper();
            LLGapHelper.GetStatusInfo(compareResult, status, out statusName, out statusValue, out lastModidfyFieldName, out lastMoidfyFieldValue);

            if (!string.IsNullOrEmpty(statusValue))
            {
                var request = string.Format(SearchRequest, GetLoginText(loginInfo),
                                            compareResult.BoardId.ToString(CultureInfo.InvariantCulture),
                                            LLGapHelper.EncodeXml(compareResult.PropertyClass), statusName, statusValue, loginInfo.ByPassAuthentication);
                //, lastModidfyFieldName, DateTime.Now.AddYears(-10).ToString("MM/dd/yyyyTHH/mm/ss") + "-" + DateTime.Now.ToString("MM/dd/yyyyTHH/mm/ss"));
                return helper.CheckRdcAccountLatLongValues(request);
            }

            return Enums.CheckAccountReturnCode.NoRecordFound;
        }

        private int SearchByTmkCountRequest(LLGapCompareResults compareResult, string status, LoginInfo loginInfo)
        {
            try
            {
                string statusName;
                string statusValue;
                string lastModidfyFieldName;
                string lastMoidfyFieldValue;
                var helper = new LLGapHelper();
                LLGapHelper.GetStatusInfo(compareResult, status, out statusName, out statusValue, out lastModidfyFieldName,
                                        out lastMoidfyFieldValue);

                if (!string.IsNullOrEmpty(compareResult.CurrentRdcPendingStatus))
                {
                    var request = string.Format(SearchTMKCountRequest, GetLoginText(loginInfo),
                                                compareResult.BoardId.ToString(CultureInfo.InvariantCulture),
                                                LLGapHelper.EncodeXml(compareResult.PropertyClass), statusName,
                                                statusValue, loginInfo.ByPassAuthentication);
                    return LLGapHelper.GetListingCount(request, "");
                }
            }
            catch (Exception ex)
            {
                Log.Error(typeof(LLGapManager), ex.Message);
            }

            return 0;
        }
        private void ProcessSearchResult(Enums.CheckAccountReturnCode code, ref LLGapCompareResults compareResult, string status)
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

                    if (!string.IsNullOrEmpty(compareResult.LatLongDataExists))
                    {
                        if (compareResult.LatLongDataExists == "N")
                        {
                            compareResult.LatLongDataExists = "P";
                        }
                    }
                    else
                    {
                        compareResult.LatLongDataExists = "Y";
                    }
                    break;
                case Enums.CheckAccountReturnCode.RequestFailed:
                    compareResult.TestResult = LLGapConstants.ResultError;
                    return;
                case Enums.CheckAccountReturnCode.RequestFailed60310:
                    compareResult.TestResult = LLGapConstants.ResultError + "Authentication Error";
                    return;
                case Enums.CheckAccountReturnCode.RequestFailed60320:
                    compareResult.TestResult = LLGapConstants.ResultError + "Search Failed";
                    return;
                case Enums.CheckAccountReturnCode.NoRecordFound:
                    if (!string.IsNullOrEmpty(compareResult.StatusRdcAccountReturnNoRecord))
                        compareResult.StatusRdcAccountReturnNoRecord += "," + status;
                    else
                    {
                        compareResult.StatusRdcAccountReturnNoRecord = status;
                    }

                    if (!string.IsNullOrEmpty(compareResult.LatLongDataExists))
                    {
                        if (compareResult.LatLongDataExists == "Y")
                        {
                            compareResult.LatLongDataExists = "P";
                        }
                    }
                    else
                    {
                        compareResult.LatLongDataExists = "";
                    }
                    break;
            }
        }

        private void GetTraceLogin(ref LLGapCompareResults compareResult)
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
                    Log.Error(typeof(LLGapManager), "Cannot retrieve log in info from Trace for Module:" + compareResult.ModuleId + ex.Message + ex.StackTrace);
                }
            }
        }

        private string GetLoginText(LoginInfo loginInfo)
        {
            var result = string.Format(LoginNodeText, LLGapHelper.EncodeXml(loginInfo.UserName),
                                       LLGapHelper.EncodeXml(loginInfo.Password),
                                       loginInfo.UserAgent, LLGapHelper.EncodeXml(loginInfo.UaPassword));
            return result;
        }
    }
}
