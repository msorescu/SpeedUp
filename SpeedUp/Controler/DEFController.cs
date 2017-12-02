using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Objects;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using SpeedUp.Helper;
using SpeedUp.Model;
using SpeedUp.TCSWebService;
using Tcs.Mls;
using Util = SpeedUp.Helper.Util;

namespace SpeedUp.Controler
{
    public class DefController
    {
        //Notice we only use the interfaces. This makes the test more 
        //robust to changes in the system.
        private readonly ObservableCollection<TraceField> _traceFieldCollection = new ObservableCollection<TraceField>();
        private readonly Metadata metadata;
        private SearchFieldCollection<SearchField> SearchFieldList;
        private string _classname;
        private bool _defLoaded;
        private string _defLocation = "";
        private int _firstLoadDef = 0;
        private int _firstLoadMeta = 0;
        private bool _initialload = true;
        private bool _metaLoaded;
        private string _metadatalocation = "";
        private string _sharedpath = "";
        private string _traceLocation = "";

        //The UsersController depends on abstractions(interfaces).
        //easier to change the behavior of a concrete class. 
        //Instead of creating concrete objects in UsersController class, 
        //we pass the objects to the constructor of UsersController
        public DefController(string defPath, Metadata metaData, string traceFolderPath)
        {
            IsRap = false;
            metadata = metaData;
            _defLocation = defPath;
            _traceLocation = traceFolderPath;
            LoadDef();
            //Sharedpath = Settings.Default.LastSharedPath;
        }

        protected bool IsRap { get; set; }

        protected bool OverwriteHelpPanel
        {
            get { return true; }
        }

        protected bool HelpPanelAllCapsCheckBoxState
        {
            get { return false; }
        }

        protected bool PredictiveHelpPanelCheck
        {
            get { return true; }
        }

        public DEF DefFile { get; set; }

        public void LoadTraceFile()
        {
            if (!string.IsNullOrEmpty(_traceLocation))
            {
                var di = new DirectoryInfo(_traceLocation);

                foreach (
                    var fi in
                        di.GetFiles()
                          .Where(fi => fi.FullName.ToLower().EndsWith("_" + DefFile.getClassName().ToLower() + ".txt")))
                {
                    _traceLocation = fi.FullName;
                    LoadTrace();
                    break;
                }
            }
        }

        public void LoadDef()
        {
            //Redundency check, make sure DEFlocation is not blank
            if (_defLocation != "")
            {
                DefFile = new DEF(_defLocation);
                SearchFieldList = new SearchFieldCollection<SearchField>(DefFile.getOrigSearchFields());
                _classname = DefFile.getClassName();

                var splitlocation = _defLocation.Split(new[] {'\\'});

                _metaLoaded = metadata != null;
            }
            _defLoaded = true;
        }

        private void LoadTrace()
        {
            var content = File.ReadAllText(_traceLocation);
            var traces = File.ReadAllText(_traceLocation).Split('\n');
            var traceLongName = "";
            for (var i = 0; i < traces.Length; i++)
            {
                if (string.IsNullOrEmpty(traces[i].Trim())) continue;

                var ts = traces[i].Split('\t');
                string headerName;
                string traceName;
                string traceNote;
                if (ts.Length > 4)
                {
                    headerName = ts[0].Trim(new[] {'\n', '\r', ' '});
                    traceName = ts[2].Trim(new[] {'\n', '\r', ' '});
                    traceLongName = ts[1].Trim(new[] {'\n', '\r', ' '});

                    traceNote = ts[3].Trim(new[] {'\n', '\r', ' '}) + " " + ts[4].Trim(new[] {'\n', '\r', ' '});
                    traceNote = traceNote.Trim(new[] {'\n', '\r', ' '});
                }
                else if (ts.Length > 1)
                {
                    traces[i] = traces[i].Replace('\t', ' ');
                    traceNote = traces[i].Trim(new[] {'\n', '\r', ' '});
                    var index = traceNote.IndexOf(' ');
                    if (index > -1)
                    {
                        headerName = traceNote.Substring(0, index);
                        traceNote = traceNote.Substring(index + 1).Trim();
                        index = traceNote.IndexOf('=');
                        if (index > -1)
                        {
                            traceName = traceNote.Substring(0, index);
                            traceNote = traceNote.Substring(index + 1).Trim();
                        }
                        else
                        {
                            index = traceNote.ToLower().IndexOf(" true ", StringComparison.Ordinal);
                            if (index > -1)
                            {
                                traceName = traceNote.Substring(0, index);
                                traceNote = traceNote.Substring(index + 6).Trim();
                            }
                            else
                            {
                                index = traceNote.ToLower().IndexOf(" false ", StringComparison.Ordinal);
                                if (index > -1)
                                {
                                    traceName = traceNote.Substring(0, index);
                                    traceNote = traceNote.Substring(index + 7).Trim();
                                }
                                else
                                {
                                    traceNote = traceNote.Trim(new[] {' ', '\t'});
                                    index = traceNote.ToLower().IndexOf(" ", StringComparison.Ordinal);
                                    if (index > -1)
                                    {
                                        traceName = traceNote.Substring(0, index);
                                        traceNote = traceNote.Substring(index + 1).Trim();
                                    }
                                    else
                                    {
                                        traceName = traceNote.Trim();
                                        traceNote = "";
                                    }
                                }
                            }
                        }
                    }
                    else
                        continue;
                }
                else
                    continue;

                var tf = new TraceField();

                tf.SystemName = headerName;
                tf.LongName = traceLongName;
                tf.Note = traceNote;
                tf.TraceName = traceName;
                _traceFieldCollection.Add(tf);
            }
        }

        public void GenerateSearchSections()
        {
            var searchFieldSection = new List<string>();
            var standardFieldSection = new List<string>();
            var tcsSearchFieldSection = new List<string>();
            var fieldSections = new Dictionary<string, List<string>>();
            var searchFieldScripts = new Dictionary<string, List<List<string>>>();
            foreach (var item in DefFile.getOrigSearchFields())
            {
                if (_defLocation.ToLower().EndsWith(".def"))
                {
                    var stdName = item.getSTDFldMapping();
                    if (!string.IsNullOrEmpty(stdName))
                    {
                        var q =
                            DEF.STDFSearchFieldNameKeepForRdcConvertList.Where(x => x.ToLower() == stdName.ToLower());
                        if (q.Any())
                        {
                            switch (stdName.ToLower())
                            {
                                case "st_lastmod":
                                case "st_picmod":
                                    tcsSearchFieldSection.Add(item.getFldName());
                                    break;
                                default:
                                    searchFieldSection.Add(item.getFldName());
                                    break;
                            }
                            standardFieldSection.Add(stdName + "=" + item.getFldName());
                            fieldSections.Add(item.getFldName(), item.getFldDef());
                            if (item.getFldScript().Count > 0)
                                searchFieldScripts.Add(item.getFldName(),
                                                       new List<List<string>> {item.getFldScript()[0]});
                        }
                    }
                }
                else
                {
                    if(item.getFldDef().Any())
                        fieldSections.Add(item.getFldName(), item.getFldDef());
                    if (item.getFldScript().Count > 0)
                        searchFieldScripts.Add(item.getFldName(), new List<List<string>> {item.getFldScript()[0]});
                }
            }

            var temp = standardFieldSection.Aggregate("", (current, t) => current + (t + "\r\n"));
            DefFile.SectionContentDict["Standard_Search"] = temp.Trim();
            temp = "";
            var k = 0;
            for (k = 0; k < tcsSearchFieldSection.Count; k++)
                temp += tcsSearchFieldSection[k] + "=__CUST__" + (1000 + k) + "\r\n";
            var isSearchType2 = DefFile.IsSearchType2();
            
            if (isSearchType2)
            {
                temp += "ReferencedSearchKey" + "=__CUST__" + (1000 + k) + "\r\n";
            }
            DefFile.SectionContentDict["Fields_TCS"] = temp;
            temp = "";
            for (var i = 0; i < searchFieldSection.Count; i++)
                temp += searchFieldSection[i] + "=__CUST__" + i + "\r\n";
            DefFile.SectionContentDict["Fields"] = temp;
            temp = "";
            foreach (var item in fieldSections)
            {
                temp += "[field_" + item.Key + "]" + "\r\n";
                temp = item.Value.Aggregate(temp, (current, t) => current + (t + "\r\n"));
                temp += "\r\n";
            }
            if (isSearchType2)
            {
                temp += "[field_" + "ReferencedSearchKey]" + "\r\n";
                temp +=  "Delimiter=,\r\n";
                temp += "\r\n";
            }
            DefFile.SectionContentDict["SearchFieldList"] = temp;
            temp = "";

            foreach (var item in searchFieldScripts)
            {
                foreach (var t in item.Value)
                {
                    foreach (var s in t)
                    {
                        temp += s + "\r\n";
                    }
                }
            }
            var mainScript = DefFile.SectionContentDict["MainScript"];
            var queryPos = mainScript.IndexOf("Query=", 0,
                                              StringComparison.CurrentCultureIgnoreCase);
            var startPos = mainScript.IndexOf("ifval \"\\Fields", queryPos,
                                              StringComparison.CurrentCultureIgnoreCase);
            if (startPos > 0)
            {
                mainScript = mainScript.Substring(0, startPos).Trim();
                mainScript = RemoveAgentRosterLogin(mainScript);
            }
            if (DefFile.FileName.ToLower().EndsWith(".def"))
            {
                var endPos = mainScript.IndexOf("query=", 0, StringComparison.CurrentCultureIgnoreCase);
                mainScript = mainScript.Substring(0, endPos + 6) + "\"";
                mainScript = SetPropertyClass(mainScript);
            }
            if (DefFile.FileName.ToLower().EndsWith(".sql"))
            {
                if (!string.IsNullOrEmpty(DefFile.ResourceName))
                {
                    mainScript = SetPropertyClass(mainScript);
                    mainScript = SetResourceName(mainScript);
                }
            }

            DefFile.SectionContentDict["MainScript"] = mainScript + "\r\n" + temp + "LABEL \"ENDSCRIPT\"";
        }

        private string SetPropertyClass(string mainScript)
        {
            var start = mainScript.IndexOf("Class=", 0, StringComparison.CurrentCultureIgnoreCase);
            var end = mainScript.IndexOf("&", start, StringComparison.CurrentCultureIgnoreCase);
            var endQuote = mainScript.IndexOf("\"", start, StringComparison.CurrentCultureIgnoreCase);
            if (endQuote > -1 && endQuote < end)
                end = endQuote;
            mainScript = mainScript.Substring(0, start + 6) + DefFile.ClassName + mainScript.Substring(end);
            return mainScript;
        }

        private string SetResourceName(string mainScript)
        {
            var start = mainScript.IndexOf("Searchtype=", 0, StringComparison.CurrentCultureIgnoreCase);
            var end = mainScript.IndexOf("&", start, StringComparison.CurrentCultureIgnoreCase);
            var endQuote = mainScript.IndexOf("\"", start, StringComparison.CurrentCultureIgnoreCase);
            if (endQuote > -1 && endQuote < end)
                end = endQuote;
            mainScript = mainScript.Substring(0, start + 11) + DefFile.ResourceName + mainScript.Substring(end);
            return mainScript;
        }

        private string RemoveAgentRosterLogin(string script)
        {
            var result = "";
            if (string.IsNullOrEmpty(script))
                return "";
            const string loginString = "transmit \"username=\"\r\n" + "transmit \"\\SecList.Name\\\"\r\n" +
                                       "transmit \"&password=\"\r\n" + "transmit \"\\SecList.Pw\\^M";
            var questionMark = script.LastIndexOf("?", StringComparison.CurrentCulture);
            var firstPos = script.IndexOf("^M", 0, StringComparison.CurrentCultureIgnoreCase);
            var lastPos = script.LastIndexOf("^M", questionMark, StringComparison.CurrentCultureIgnoreCase);

            result = script.Substring(0, firstPos + 2) + "\"\r\n" + loginString + script.Substring(lastPos + 2);
            return result;
        }

        public void GenerateSecListSeciton()
        {
            var content = DefFile.SectionContentDict["SecList"];
            if (content.IndexOf("Pw=__PASS2__", 0, StringComparison.CurrentCultureIgnoreCase) < 0)
            {
                var pos = content.LastIndexOf("__");
                content = "Name=__PASS1__\r\nPw=__PASS2__" + content.Substring(pos + 2);
                DefFile.SectionContentDict["SecList"] = content;
            }
        }

        public void GeneratePictureScriptSection()
        {
            DefFile.SectionContentDict["PicScript"] = RemoveAgentRosterLogin(DefFile.SectionContentDict["PicScript"]);
        }

        public void SaveDefFile()
        {
            DefFile.SaveDefFile();
        }

        public void UpdateMetadataVersion()
        {
            if (metadata != null)
            {
                var iniFile = new IniFile(_defLocation);
                iniFile.Write("MetaDataversion", metadata.MetaDataVersion, "Common");
            }
        }

        public void UpdateAllSearchField()
        {
            foreach (var item in SearchFieldList.Fields)
            {
                var stdfSearchName = item.getSTDFldMapping();
                var newSystemName = item.getSystemName();
                switch (stdfSearchName.ToLower())
                {
                    case "st_lastmod":
                        newSystemName = DefFile.GetSystemName("STDFLastMod");
                        break;
                    case "st_picmod":
                        newSystemName = DefFile.GetSystemName("STDFPicMod");
                        break;
                    case "st_mlsno":
                        newSystemName = DefFile.GetSystemName("RecordID");
                        break;
                    case "st_status":
                        newSystemName = DefFile.GetSystemName("CMAIdentifier");
                        break;
                    case "st_listdate":
                        newSystemName = DefFile.GetSystemName("CMAListingDate");
                        break;
                    case "st_listpr":
                        newSystemName = DefFile.GetSystemName("CMAListingPrice");
                        break;
                }
                if (string.IsNullOrEmpty(newSystemName))
                {
                    newSystemName = item.getSystemName();
                }
                else
                {
                    item.setSystemName(newSystemName);
                }
                if (!string.IsNullOrEmpty(item.getSystemName()))
                {
                    if (metadata.GetLookUpValues(newSystemName, DefFile.ClassName).Any())
                    {
                        UpdateSearchFieldPicklist(item.getFldName(), newSystemName, "L", false, IsRap, true, false, 0);
                    }
                    else
                    {
                        UpdateSearchField(item.getFldName(), newSystemName,
                                          metadata.GetDataType(newSystemName, DefFile.ClassName), false, IsRap, 0, item);
                    }
                }
            }
        }

        public void GenerateMlsRecordExSection()
        {
            var sb = new StringBuilder();
            foreach (var field in DefFile.MlsResultFieldCollection)
            {
                if(!field.HeaderName.StartsWith("\\"))
                {
                    sb.Append(
                    "@\"" + field.HeaderName + "\" \"FldName=" + field.DefName + ", InpLength=" + field.InpLength +
                    ", CutBy=" + field.CutBy + ", FldType=" + field.FieldType + "\"\r\n");
                }
                else
                {
                    sb.Append(
                    field.HeaderName + " \"FldName=" + field.DefName + ", InpLength=" + field.InpLength +
                    ", CutBy=" + field.CutBy + ", FldType=" + field.FieldType + "\"\r\n");
                }
            }

            DefFile.SectionContentDict["MLSRecordsEx"] = sb.ToString().Trim();
        }

        public void GenerateCategorySections()
        {
            var masterCategories = SharepointXlsHelper.GetMasterCategories();
            var sb = new StringBuilder();
            foreach (var item in masterCategories.Values)
            {
                var category = item.Split('|')[0];
                var query =
                    DefFile.MlsResultFieldCollection.Where(x => x.Category!= null && x.Category.ToLower() == category.ToLower())
                           .Select(x => new {x.DefName})
                           .Distinct()
                           .ToList();
                var categorizedFields = "";
                foreach (var fd in query)
                {
                    categorizedFields += fd.DefName + "\r\n";
                }

                if (!string.IsNullOrEmpty(categorizedFields))
                    sb.Append("[ResultFieldGroup_" + category + "]\r\n" + categorizedFields.Trim() + "\r\n\r\n");
            }

            if (sb.Length > 0)
                DefFile.SectionContentDict["ResultFieldGroupList"] = sb.ToString();
        }

        public void GenerateResultFieldSections()
        {
            var sb = new StringBuilder();

            foreach (var item in DefFile.MlsResultFieldCollection)
            {
                if (item.DefName.Equals("ReferencingKey", StringComparison.CurrentCultureIgnoreCase))
                    continue;
                sb.Append("[CMAfield_" + item.DefName + "]\r\n");
                if (item.IsStdf == "N")
                    sb.Append("DisplayName=" + item.DisplayName + "\r\n");
                if(!string.IsNullOrEmpty(item.LongName))
                    sb.Append("RETSLongName=" + item.LongName + "\r\n");
                if (!string.IsNullOrEmpty(item.ColumnCaption))
                    sb.Append("ColCaption=" + item.ColumnCaption + "\r\n");
                if (!string.IsNullOrEmpty(item.Delimeter))
                    sb.Append("Delimiter=" + item.Delimeter + "\r\n");

                sb.Append("\r\n");
            }

            if (sb.Length > 0)
                DefFile.SectionContentDict["CmaFieldList"] = sb.ToString().Trim();
        }


        public void GenerateNonStandardResultField()
        {
            using (var ts = new MlsSoapClient())
            {
                foreach (var item in _traceFieldCollection)
                {
                    var mapped = false;
                    foreach (var x in DefFile.MlsResultFieldCollection)
                    {
                        if (x.SystemName.ToLower() == item.SystemName.ToLower())
                        {
                            mapped = true;
                            break;
                        }
                    }
                    if (!mapped)
                    {
                        var mf = new MlsResultField();
                        var first =
                            metadata.MlsMetadataFieldCollection.FirstOrDefault(
                                x => x.SystemName.ToLower() == item.SystemName.ToLower());
                        if (first != null)
                        {
                            mf.LongName = first.LongName;
                            mf.SystemName = first.SystemName;
                            mf.DisplayName = first.LongName;
                            mf.IsStdf = "N";
                            mf.FieldType = "S";
                            mf.InpLength = "1000";
                            mf.CutBy = @"\09";
                            mf.HeaderName = first.SystemName;
                            mf.DefName = GetUniqueDefName(Util.GetValidName(first.LongName));
                            var cate = ts.GetCategoryNameByPropertyClass(mf.LongName,
                                                                            DefFile.GetStandardClassesName());
                            if (!string.IsNullOrEmpty(cate))
                                mf.Category = cate;
                            else
                            {
                                mf.Category = "";
                            }
                            DefFile.MlsResultFieldCollection.Add(mf);
                        }
                    }
                }
            }
        }

        private string GetUniqueDefName(string getValidName)
        {
            for (var i = 1; i < 20; i++)
            {
                if (DefFile.MlsResultFieldCollection.Any(x => x.DefName.ToLower() == getValidName.ToLower()))
                    getValidName = getValidName + i;
                else
                {
                    break;
                }
            }
            return getValidName;
        }

        public void GenerateStandardResultField()
        {
            var xDoc = new XmlDocument();
            xDoc.LoadXml("<TCService />");

            xDoc.DocumentElement.SetAttribute("Vendor", DefFile.VendorName);
            xDoc.DocumentElement.SetAttribute("PropertyClass", DefFile.ClassName);
            xDoc.DocumentElement.SetAttribute("DefFileName", DefFile.FileName);


            var stdFields = xDoc.CreateElement("TCSStandardFields");
            xDoc.DocumentElement.AppendChild(stdFields);

            var defNameToIndex = new Dictionary<string, int>();

            //if (DefFile.FileName.ToLower().EndsWith(".sql"))
            //{
            //    XmlElement nfRecordId = xDoc.CreateElement("Field");
            //    nfRecordId.SetAttribute("Name", TCSStandardResultFields.GetDEFName(TCSStandardResultFields.STDF_RECORDID));
            //    if (!defNameToIndex.ContainsKey(TCSStandardResultFields.GetDEFName(TCSStandardResultFields.STDF_RECORDID)))
            //        defNameToIndex.Add(TCSStandardResultFields.GetDEFName(TCSStandardResultFields.STDF_RECORDID), TCSStandardResultFields.STDF_RECORDID);
            //    stdFields.AppendChild(nfRecordId);
            //}
            if (DefFile.FileName.ToLower().EndsWith("ag.sql"))
            {
                var fieldList = TCSStandardResultFields.GetAgentRosterResultFields();
                foreach (var item in fieldList)
                {
                    var nf = xDoc.CreateElement("Field");
                    nf.SetAttribute("Name", TCSStandardResultFields.GetDEFName(item));
                    if (!defNameToIndex.ContainsKey(TCSStandardResultFields.GetDEFName(item)))
                        defNameToIndex.Add(TCSStandardResultFields.GetDEFName(item), item);
                    stdFields.AppendChild(nf);
                }
            }
            else if (DefFile.FileName.ToLower().EndsWith("of.sql"))
            {
                var fieldList = TCSStandardResultFields.GetOfficeRosterResultFields();

                foreach (var item in fieldList)
                {
                    var nf = xDoc.CreateElement("Field");
                    nf.SetAttribute("Name", TCSStandardResultFields.GetDEFName(item));
                    if (!defNameToIndex.ContainsKey(TCSStandardResultFields.GetDEFName(item)))
                        defNameToIndex.Add(TCSStandardResultFields.GetDEFName(item), item);
                    stdFields.AppendChild(nf);
                }
            }
            else if (DefFile.FileName.ToLower().EndsWith("oh.sql"))
            {
                var fieldList = TCSStandardResultFields.GetOpenHouseResultFields();

                foreach (var item in fieldList)
                {
                    var nf = xDoc.CreateElement("Field");
                    nf.SetAttribute("Name", TCSStandardResultFields.GetDEFName(item));
                    if (!defNameToIndex.ContainsKey(TCSStandardResultFields.GetDEFName(item)))
                        defNameToIndex.Add(TCSStandardResultFields.GetDEFName(item), item);
                    stdFields.AppendChild(nf);
                }
            }
            else
            {
                var fieldList = TCSStandardResultFields.GetDataAggResultFields();

                foreach (var item in fieldList)
                {
                    var nf = xDoc.CreateElement("Field");
                    nf.SetAttribute("Name", TCSStandardResultFields.GetDEFName(item));
                    if (!defNameToIndex.ContainsKey(TCSStandardResultFields.GetDEFName(item)))
                        defNameToIndex.Add(TCSStandardResultFields.GetDEFName(item), item);
                    stdFields.AppendChild(nf);
                }
            }

            var retsFields = xDoc.CreateElement("RETSMetaDataFields");
            if (xDoc.DocumentElement != null) xDoc.DocumentElement.AppendChild(retsFields);
            var retsFieldList = metadata.GetAllFieldsByClassName(DefFile.ClassName);
            foreach (var retsField in retsFieldList)
            {
                if (string.IsNullOrEmpty(retsField.SystemName))
                    continue;
                var nf = xDoc.CreateElement("Field");
                nf.SetAttribute("SystemName", retsField.SystemName);
                nf.SetAttribute("StandardName", retsField.StandardName);
                nf.SetAttribute("LongName", retsField.LongName);
                nf.SetAttribute("DataType", retsField.DataType);
                nf.SetAttribute("Length", retsField.MaximumLength);
                nf.SetAttribute("value", "");
                retsFields.AppendChild(nf);
            }

            var request = xDoc.OuterXml;
            var mls = new MlsSoapClient();
            var mappingResult = mls.GetTCSStandardFieldMapping(request);

            var xResp = new XmlDocument();
            try
            {
                xResp.LoadXml(mappingResult);
            }
            catch
            {
                xResp.LoadXml("<TCResult />");
            }

            var nList = xResp.DocumentElement.SelectNodes("//Field");
            var fieldHashSet = new HashSet<string>();
            var standardFieldHashSet = new HashSet<string>();
            foreach (XmlNode node in nList)
            {
                var stdName = ((XmlElement) node).GetAttribute("TCSStandardName");
                var sysName = ((XmlElement) node).GetAttribute("RETSSystemName");
                var longName = ((XmlElement)node).GetAttribute("RETSLongName");

                if (stdName.Equals("CMASuiteNo", StringComparison.CurrentCultureIgnoreCase) && !standardFieldHashSet.Contains("cmastreetname"))
                {

                    if (standardFieldHashSet.Contains("stdfstreetnameparsed"))
                    {
                        var streetName = "";
                        if (standardFieldHashSet.Contains("stdfstreetdirprefix"))
                            streetName += "STDFStreetDirPrefix,";

                        streetName += "STDFStreetNameParsed,";
                        if (standardFieldHashSet.Contains("stdfstreettype"))
                            streetName += "STDFStreetType,";
                        if (standardFieldHashSet.Contains("stdfstreetdirsuffix"))
                            streetName += "STDFStreetDirSuffix,";

                        if (streetName.EndsWith(","))
                            streetName = streetName.Substring(0, streetName.Length - 1);
                        var fieldRecordId = new MlsResultField
                            {
                                DefName = "CMAStreetName",
                                SystemName = "\\"+ streetName +"\\",
                                InpLength = "1000",
                                CutBy = @"\09",
                                HeaderName = "\\STDFStreetDirPrefix,STDFStreetNameParsed,STDFStreetType,STDFStreetDirSuffix\\",
                                Delimeter = "~",
                                FieldType= "S",
                                ColumnCaption = "Street Name"
                            };
                        DefFile.MlsResultFieldCollection.Add(fieldRecordId);
                    }
                    else
                    {
                        var fieldRecordId = new MlsResultField
                            {
                                DefName = "CMAStreetName",
                                SystemName = "\\\\",
                                InpLength = "1000",
                                CutBy = @"\09",
                                HeaderName = "\\\\",
                                Delimeter = "~",
                                FieldType = "S",
                                ColumnCaption = "Street Name"
                            };
                        DefFile.MlsResultFieldCollection.Add(fieldRecordId);
                    }
                }

                if (string.IsNullOrWhiteSpace(sysName))
                    continue;

                if (fieldHashSet.Contains(sysName.ToLower()))
                    continue;

                var field = new MlsResultField();
                if (stdName.Equals("CMAGarage", StringComparison.CurrentCultureIgnoreCase))
                {
                    string a = sysName +  " ";
                }
                var queryLongNameMatch = retsFieldList.Where(x => x.LongName.ToLower() == longName.ToLower());
                if (queryLongNameMatch.Any())
                {
                    var isFound = false;
                    foreach (var item in queryLongNameMatch)
                    {
                        if (_traceFieldCollection.Where(x => x.SystemName.ToLower() == item.SystemName.ToLower()).Any())
                        { 
                            sysName = item.SystemName;
                            isFound = true;
                            break;
                        }
                    }
                    if (!isFound && _defLocation.ToLower().EndsWith(".def"))
                    {
                        TCSEntities tcs = new TCSEntities();
                        ObjectResult<PR_tcs_standard_result_fields_GetLongname_Result> getLongNameResult =
                            tcs.PR_tcs_standard_result_fields_GetLongname(stdName, DefFile.VendorName, 0);

                        foreach (var item in getLongNameResult)
                        {
                            var retsLongName = item.rets_long_name;
                            var queryLongName = retsFieldList.Where(x => x.LongName.ToLower() == retsLongName.ToLower());
                            if (queryLongName.Any())
                            {
                                foreach (var fd in queryLongName)
                                {
                                    if (
                                        _traceFieldCollection.Any(x => x.SystemName.ToLower() == fd.SystemName.ToLower()))
                                    {
                                        sysName = fd.SystemName;
                                        isFound = true;
                                        break;
                                    }
                                }
                            }
                            if (isFound)
                                break;
                        }
                    }
                }
               

                field.DefName = stdName;
                field.CutBy = @"\09";
                field.Category = StandardFieldDataDictionary.Instance.GetDefaultCategory(stdName);
                if (string.IsNullOrEmpty(field.Category))
                    field.Category = "";
                field.IsStdf = "Y";
                field.HeaderName = sysName;

                var first = retsFieldList.FirstOrDefault(x => x.SystemName.ToLower() == sysName.ToLower());
                if (first != null)
                {
                    field.FieldType =
                        StaticRules.GetDEFFieldTypeByDD(
                            TCSStandardResultFields.GetDataType(defNameToIndex[field.DefName]));
                    field.LongName = first.LongName;
                    field.SystemName = first.SystemName;
                }
                if (field.FieldType.ToLower().Equals("d"))
                    field.InpLength = "10";
                else if (field.FieldType.ToLower().Equals("dt"))
                    field.InpLength = "23";
                else if (field.DefName.ToLower() == "stdfagentremarks" || field.DefName.ToLower() == "notes")
                    field.InpLength = "5000";
                else if (field.DefName.ToLower() == "stdfdisplaylistingonrdc")
                    field.InpLength = "1";
                else
                    field.InpLength = "1000";
                DefFile.MlsResultFieldCollection.Add(field);
                if (field.DefName.ToLower() == "ag_agentid" || field.DefName.ToLower() == "of_officeid" ||
                    field.DefName.ToLower() == "oh_openhouseid")
                {
                    var fieldRecordId = new MlsResultField();
                    fieldRecordId.DefName = "RecordID";
                    fieldRecordId.SystemName = field.SystemName;
                    fieldRecordId.InpLength = field.InpLength;
                    fieldRecordId.CutBy = field.CutBy;
                    fieldRecordId.HeaderName = field.HeaderName;
                    DefFile.MlsResultFieldCollection.Add(fieldRecordId);
                }
                if (field.DefName.ToLower() == "recordid" && DefFile.IsSearchType2())
                {
                    var fieldRefId = new MlsResultField();
                    fieldRefId.DefName = "ReferencingKey";
                    fieldRefId.SystemName = field.SystemName;
                    fieldRefId.InpLength = field.InpLength;
                    fieldRefId.CutBy = field.CutBy;
                    fieldRefId.HeaderName = field.HeaderName;
                    fieldRefId.LongName = field.LongName;
                    DefFile.MlsResultFieldCollection.Add(fieldRefId);
                }
                fieldHashSet.Add(sysName.ToLower());
                standardFieldHashSet.Add(stdName.ToLower());
            }
        }

        public void GenerateSortingSection()
        {
            var activeStaus = "";
            var offMarketStatus = "";
            var firstOrDefault =
                DefFile.MlsResultFieldCollection.FirstOrDefault(x => x.DefName.ToLower() == "cmaidentifier");
            var statusSN = "";
            if (firstOrDefault !=
                null)
            {
                statusSN = firstOrDefault.SystemName;
            }
            if (string.IsNullOrEmpty(statusSN))
                return;
            var lookupValues = metadata.GetLookUpValues(statusSN, DefFile.ClassName);
            foreach (var item in lookupValues)
            {
                if (item.LongValueCol.ToLower().StartsWith("ac"))
                    activeStaus = ToTitleCase(item.LongValueCol);
                else
                {
                    offMarketStatus += ToTitleCase(item.LongValueCol) + ",";
                }
            }
            if (offMarketStatus.EndsWith(","))
            {
                offMarketStatus.Substring(0, offMarketStatus.Length - 1);
            }
            if (activeStaus.Length == 0)
            {
                activeStaus = offMarketStatus;
                offMarketStatus = "";
            }
            var publicSortingSecton = "Active=" + activeStaus + "\r\nSold=\r\nOffMarketOrOther=" + offMarketStatus;
            var sortingSection = "SortField=CMAIdentifier\r\nActive=" + activeStaus +
                                    "\r\nSold=\r\nPending=\r\nExpired=\r\nFormat=L";
            DefFile.SectionContentDict["Sorting"] = sortingSection;
            DefFile.SectionContentDict["SortingPublicListingStatus"] = publicSortingSecton;
        }

        public void UpdateSearchFieldSystemName(string searchFieldName, string systemName)
        {
            SearchFieldList.updateSystemNamefor(searchFieldName, systemName);
        }

        public void UpdateSearchFieldPicklist(string searchFieldName, string systemName, string picklistFormat,
                                              bool alphasort, bool rapStyle, bool casing, bool conCat, int scriptIndex)
        {
            //DEFfile.updateSearchFldwithPicklist(SearchFieldName, metadata.GetLookUpValues(SystemName), PicklistFormat, alphasort,RapStyle, ScriptIndex);

            //Strip the lines from the field def, which start with value, format, helppanel,charsexcluded,  delimiter, and control type. Replace with new lines
            //completey erase the previous script, replace with newly constructed one
            var newDef = new List<string>();
            var newScript = new List<string>();
            var parsedCaption = metadata.GetRetsLongName(systemName, DefFile.ClassName);
            var rawLookupValues = metadata.GetLookUpValues(systemName, DefFile.ClassName);
            var uniqueLookupValues = RemoveDuplicates(rawLookupValues);
            var lookupValues = rawLookupValues;
            var hpExists = false;

            if (conCat)
            {
                lookupValues = uniqueLookupValues;
            }

            //Get the current Definition of the field
            //CurrentDef = SearchFieldList[saveposition].getFldDef();
            List<string> currentDef = SearchFieldList.getFldDEFfor(searchFieldName);

            //Copy over only those lines which do not start with the following
            for (var j = 0; j < currentDef.Count; j++)
            {
                if (currentDef[j].StartsWith("Caption"))
                {
                    const string removeString = "Caption=";

                    var index = currentDef[j].IndexOf(removeString);
                    parsedCaption = (index < 0) ? currentDef[j] : currentDef[j].Remove(index, removeString.Length);
                }
                else if ((!currentDef[j].StartsWith("Value")) &&
                         (!currentDef[j].StartsWith("Delimiter")) && (!currentDef[j].StartsWith("Format")) &&
                         (currentDef[j] != "") &&
                         (!currentDef[j].StartsWith("ControlType")) && (!currentDef[j].StartsWith("CharsExcluded")))
                {
                    if ((currentDef[j].StartsWith("HelpPanel")))
                    {
                        if ((OverwriteHelpPanel))
                        {
                            hpExists = false;
                        }
                        else
                        {
                            newDef.Add(currentDef[j]);
                            hpExists = true;
                        }
                    }
                    else
                    {
                        newDef.Add(currentDef[j]);
                    }
                }
            }


            //Add the following lines to the definition

            newDef.Add("Caption=" + parsedCaption);
            newDef.Add("ControlType=2");
            newDef.Add("Format=" + picklistFormat);
            newDef.Add("Delimiter=,");
            if (!hpExists)
            {
                newDef.Add("HelpPanel=Select the " + parsedCaption + " value(s) from the picklist.");
            }
            newScript.Add("ifval \"\\Fields." + searchFieldName + "\\\"");
            if (!rapStyle)
            {
                newScript.Add("  ifval \"\\First\\\"");
                newScript.Add("     transmit \",\"");
                newScript.Add("  endiv \"\"");
                newScript.Add("  set \"First=Y\"");
            }
            else
            {
                newScript.Add("     transmit \",\"");
            }


            //Body of script based on picklist format
            if (picklistFormat == "L")
            {
                newScript.Add("  SetField \"\\Fields." + searchFieldName + "\\,FIRST\"");
                newScript.Add("  transmit \"(" + systemName + "=|\"");
                newScript.Add("  Label \"Next" + searchFieldName + "\"");
            }
            else if (picklistFormat == "S")
            {
                newScript.Add("  SetField \"\\Fields." + searchFieldName + "\\,FIRST\"");
                newScript.Add("  transmit \"(" + systemName + "=|\"");
                newScript.Add("  Label \"Next" + searchFieldName + "\"");
                newScript.Add("  transval \"\\Fields." + searchFieldName + "\\{ },@\"");
                newScript.Add("  SetField \"\\Fields." + searchFieldName + "\\,NEXT\"");
                newScript.Add("  ifval \"\\Fields." + searchFieldName + "\\,@\"");
                newScript.Add("     transmit \",\"");
                newScript.Add("     goto \"Next" + searchFieldName + "\"");
                newScript.Add("  endiv \"\"");
                newScript.Add("  transmit \")\"");
                newScript.Add("endiv \"\"");
            }
            else if (picklistFormat == "S - L")
            {
                newScript.Add("  SetField \"\\Fields." + searchFieldName + "\\,FIRST\"");
                newScript.Add("  transmit \"(" + systemName + "=|\"");
                newScript.Add("  Label \"Next" + searchFieldName + "\"");
                newScript.Add("  transval \"\\Fields." + searchFieldName + "\\{ },@:(' - ',1)\"");
                newScript.Add("  SetField \"\\Fields." + searchFieldName + "\\,NEXT\"");
                newScript.Add("  ifval \"\\Fields." + searchFieldName + "\\,@\"");
                newScript.Add("     transmit \",\"");
                newScript.Add("     goto \"Next" + searchFieldName + "\"");
                newScript.Add("  endiv \"\"");
                newScript.Add("  transmit \")\"");
                newScript.Add("endiv \"\"");
            }
            else if (picklistFormat == "L - S")
            {
                newScript.Add("  SetField \"\\Fields." + searchFieldName + "\\,FIRST\"");
                newScript.Add("  transmit \"(" + systemName + "=|\"");
                newScript.Add("  Label \"Next" + searchFieldName + "\"");
                newScript.Add("  transval \"\\Fields." + searchFieldName + "\\{ },@:(' - ',2)\"");
                newScript.Add("  SetField \"\\Fields." + searchFieldName + "\\,NEXT\"");
                newScript.Add("  ifval \"\\Fields." + searchFieldName + "\\,@\"");
                newScript.Add("     transmit \",\"");
                newScript.Add("     goto \"Next" + searchFieldName + "\"");
                newScript.Add("  endiv \"\"");
                newScript.Add("  transmit \")\"");
                newScript.Add("endiv \"\"");
            }


            //Add the 'Value' lines to the field definition
            var valuecounter = 1;
            var formattedstr = "";

            if (alphasort)
            {
                var SortedAlphabetSortedValues =
                    lookupValues.OrderBy(entry => entry.LongValueCol);

                foreach (var LookupEntry in SortedAlphabetSortedValues)
                {
                    if (picklistFormat == "L")
                    {
                        if (casing)
                        {
                            formattedstr =
                                replaceSpecialChar("Value" + valuecounter + "=" +
                                                   ToTitleCase(LookupEntry.LongValueCol.Trim()));
                        }
                        else
                        {
                            formattedstr =
                                replaceSpecialChar("Value" + valuecounter + "=" + LookupEntry.LongValueCol.Trim());
                        }
                        newDef.Add(formattedstr);
                    }
                    else if (picklistFormat == "S")
                    {
                        formattedstr = ("Value" + valuecounter + "=" + LookupEntry.ValueCol);
                        newDef.Add(formattedstr);
                    }
                    else if (picklistFormat == "S - L")
                    {
                        if (casing)
                        {
                            formattedstr = ("Value" + valuecounter + "=" + LookupEntry.ValueCol + " - " +
                                            replaceSpecialChar(ToTitleCase(LookupEntry.LongValueCol.Trim())));
                        }
                        else
                        {
                            formattedstr = ("Value" + valuecounter + "=" + LookupEntry.ValueCol + " - " +
                                            replaceSpecialChar(LookupEntry.LongValueCol.Trim()));
                        }

                        newDef.Add(formattedstr);
                    }
                    else if (picklistFormat == "L - S")
                    {
                        if (casing)
                        {
                            formattedstr = ("Value" + valuecounter + "=" +
                                            replaceSpecialChar(ToTitleCase(LookupEntry.LongValueCol.Trim())) + " - " +
                                            LookupEntry.ValueCol);
                        }
                        else
                        {
                            formattedstr = ("Value" + valuecounter + "=" +
                                            replaceSpecialChar(LookupEntry.LongValueCol.Trim()) + " - " +
                                            LookupEntry.ValueCol);
                        }
                        newDef.Add(formattedstr);
                    }


                    valuecounter++;
                }
            }
            else
            {
                foreach (var LookupEntry in lookupValues)
                {
                    if (picklistFormat == "L")
                    {
                        if (casing)
                        {
                            formattedstr =
                                replaceSpecialChar("Value" + valuecounter + "=" +
                                                   ToTitleCase(LookupEntry.LongValueCol.Trim()));
                        }
                        else
                        {
                            formattedstr =
                                replaceSpecialChar("Value" + valuecounter + "=" + LookupEntry.LongValueCol.Trim());
                        }
                        newDef.Add(formattedstr);
                    }
                    else if (picklistFormat == "S")
                    {
                        formattedstr = ("Value" + valuecounter + "=" + LookupEntry.ValueCol);
                        newDef.Add(formattedstr);
                    }
                    else if (picklistFormat == "S - L")
                    {
                        if (casing)
                        {
                            formattedstr = ("Value" + valuecounter + "=" + LookupEntry.ValueCol + " - " +
                                            replaceSpecialChar(ToTitleCase(LookupEntry.LongValueCol.Trim())));
                        }
                        else
                        {
                            formattedstr = ("Value" + valuecounter + "=" + LookupEntry.ValueCol + " - " +
                                            replaceSpecialChar(LookupEntry.LongValueCol.Trim()));
                        }
                        newDef.Add(formattedstr);
                    }
                    else if (picklistFormat == "L - S")
                    {
                        if (casing)
                        {
                            formattedstr = ("Value" + valuecounter + "=" +
                                            replaceSpecialChar(ToTitleCase(LookupEntry.LongValueCol.Trim())) + " - " +
                                            LookupEntry.ValueCol);
                        }
                        else
                        {
                            formattedstr = ("Value" + valuecounter + "=" +
                                            replaceSpecialChar(LookupEntry.LongValueCol.Trim()) + " - " +
                                            LookupEntry.ValueCol);
                        }
                        newDef.Add(formattedstr);
                    }


                    valuecounter++;
                }
            }

            if (picklistFormat == "L")
            {
                //Sort the lookup values
                var SortedLookupValues =
                    lookupValues.OrderByDescending(entry => entry.LongValueCol.Length);

                foreach (var SortedLookupEntry in SortedLookupValues)
                {
                    if (casing)
                    {
                        formattedstr = replaceSpecialChar(ToTitleCase(SortedLookupEntry.LongValueCol.Trim()));
                    }
                    else
                    {
                        formattedstr = replaceSpecialChar(SortedLookupEntry.LongValueCol.Trim());
                    }
                    //formattedstr = replaceSpecialChar(SortedLookupEntry.LongValueCol);
                    newScript.Add("  Ifinc \"\\Fields." + searchFieldName + "\\,@," + formattedstr + "\"");
                    formattedstr = SortedLookupEntry.ValueCol;
                    newScript.Add("     transmit \"" + formattedstr + "\"");
                    newScript.Add("     goto \"End" + searchFieldName + "\"");
                    newScript.Add("  endiv \"\"");
                }

                newScript.Add("  transval \"\\Fields." + searchFieldName + "\\,@\"");
                newScript.Add("  Label \"End" + searchFieldName + "\"");
                newScript.Add("  SetField \"\\Fields." + searchFieldName + "\\,NEXT\"");
                newScript.Add("  ifval \"\\Fields." + searchFieldName + "\\,@\"");
                newScript.Add("     transmit \",\"");
                newScript.Add("     goto \"Next" + searchFieldName + "\"");
                newScript.Add("  endiv \"\"");
                newScript.Add("  transmit \")\"");
                newScript.Add("endiv \"\"");
            }

            SearchFieldList.updateTextFldDeffor(searchFieldName, newDef);
            SearchFieldList.updateTextFldScriptfor(searchFieldName, newScript, scriptIndex);
        }

        public void UpdateSearchField(string SearchFieldName, string SystemName, string DataType, bool PartialSearch,
                                      bool RapStyle, int ScriptIndex, SearchField sField)
        {
            //Strip the lines from the field def, which start with value, format, helppanel,charsexcluded,  delimiter, and control type. Replace with new lines
            //completey erase the previous script, replace with newly constructed one
            var NewDef = new List<string>();
            var CurrentDef = new List<string>();
            var NewScript = new List<string>();
            var RETSLongName = metadata.GetRetsLongName(SystemName, DefFile.ClassName);
            var StaticRETSLongName = metadata.GetRetsLongName(SystemName, DefFile.ClassName);
            var stdfFieldName = sField.getSTDFldMapping().ToLower();
            var isMlsNo = stdfFieldName.Equals("st_mlsno");
            var HPExists = false;

            if (isMlsNo)
            {
                var mlsNoScript = string.Join("\r\n", sField.getFldScript()[0].ToArray());
                bool IsRequiredField = false;
                if (mlsNoScript.IndexOf("else", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
                {
                    if (mlsNoScript.IndexOf("0%2B", 0, StringComparison.CurrentCultureIgnoreCase) > -1 ||
                        mlsNoScript.IndexOf("1%2B", 0, StringComparison.CurrentCultureIgnoreCase) > -1 ||
                        mlsNoScript.IndexOf("0+", 0, StringComparison.CurrentCultureIgnoreCase) > -1 ||
                        mlsNoScript.IndexOf("1+", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
                    {
                        IsRequiredField = true;
                    }
                }
                NewDef.Add("Caption=MLS Number(s)");
                NewDef.Add("Delimiter=,");
                NewDef.Add("HelpPanel=Enter the MLS Numbers. Separate each with a comma. eg: 13874,13606,13796");
                if (DefFile.IsSearchType2())
                {
                    NewScript.Add("ifval \"\\Fields.ReferencedSearchKey\\\"");
                    NewScript.Add("  transmit \"(" + SearchFieldList.getSystemNamefor(SearchFieldName) + "=\"");
                    NewScript.Add("  transval \"\\Fields.ReferencedSearchKey\\{ }\"");
                    NewScript.Add("  transmit \")\"");
                    NewScript.Add("  goto \"ENDSCRIPT\"");
                    NewScript.Add("endiv \"\"");
                }
                NewScript.Add("ifval \"\\Fields." + SearchFieldName + "\\\"");
                NewScript.Add("  transmit \"(" + SearchFieldList.getSystemNamefor(SearchFieldName) + "=\"");
                NewScript.Add("  transval \"\\Fields." + SearchFieldName + "\\{ }\"");
                NewScript.Add("  transmit \")\"");
                NewScript.Add("  goto \"ENDSCRIPT\"");
                if (IsRequiredField)
                {
                    NewScript.Add("else \"\"");
                    NewScript.Add("  transmit \"(" + SearchFieldList.getSystemNamefor(SearchFieldName) + "=\"");
                    NewScript.Add("  transmit \"1%2B\"");
                    NewScript.Add("  transmit \"),\"");
                }
                NewScript.Add("endiv \"\"");
            }
            else
            {
                if ((DataType == "Tiny") || (DataType == "Small") || (DataType == "Long"))
                {
                    DataType = "Int";
                }

                //Get the current Definition of the field
                CurrentDef = SearchFieldList.getFldDEFfor(SearchFieldName);

                //Copy over only those lines which do not start with the following
                for (var j = 0; j < CurrentDef.Count; j++)
                {
                    if (CurrentDef[j].StartsWith("["))
                    {
                        break;
                    }

                    if (CurrentDef[j].StartsWith("Caption"))
                    {
                        var removeString = "Caption=";

                        var index = CurrentDef[j].IndexOf(removeString);
                        RETSLongName = (index < 0) ? CurrentDef[j] : CurrentDef[j].Remove(index, removeString.Length);
                    }
                    else if ((!CurrentDef[j].StartsWith("Value")) && (!CurrentDef[j].StartsWith("SDateFormat")) &&
                             (!CurrentDef[j].StartsWith("Delimiter")) && (!CurrentDef[j].StartsWith("Format")) &&
                             (CurrentDef[j] != "") &&
                             (!CurrentDef[j].StartsWith("ControlType")) && (!CurrentDef[j].StartsWith("CharsExcluded")))
                    {
                        if ((CurrentDef[j].StartsWith("HelpPanel")))
                        {
                            if ((OverwriteHelpPanel))
                            {
                                HPExists = false;
                            }
                            else
                            {
                                NewDef.Add(CurrentDef[j]);
                                HPExists = true;
                            }
                        }
                        else
                        {
                            NewDef.Add(CurrentDef[j]);
                        }
                    }
                }

                NewScript.Add("ifval \"\\Fields." + SearchFieldName + "\\\"");

                if (!RapStyle)
                {
                    NewScript.Add("  ifval \"\\First\\\"");
                    NewScript.Add("     transmit \",\"");
                    NewScript.Add("  endiv \"\"");
                    NewScript.Add("  set \"First=Y\"");
                }
                else
                {
                    NewScript.Add("     transmit \",\"");
                }

                if (DataType == "Character")
                {
                    NewDef.Add("Caption=" + RETSLongName);
                    NewDef.Add("Delimiter=,");
                    if (PartialSearch)
                    {
                        //NewDef.Add("HelpPanel=Enter partial or complete " + RETSLongName + "(s).  Separate each with a comma." + PredictiveValues(StaticRETSLongName, 'P'));
                        if (!HPExists)
                        {
                            if (PredictiveHelpPanelCheck)
                            {
                                if (HelpPanelAllCapsCheckBoxState)
                                {
                                    NewDef.Add("HelpPanel=Enter complete or partial " + RETSLongName +
                                               "(s). Separate each with a comma (eg: " +
                                               PredictiveValues(StaticRETSLongName, 'P').ToUpper());
                                }
                                else
                                {
                                    NewDef.Add("HelpPanel=Enter complete or partial " + RETSLongName +
                                               "(s). Separate each with a comma (eg: " +
                                               PredictiveValues(StaticRETSLongName, 'P'));
                                }
                            }
                            else
                            {
                                NewDef.Add("HelpPanel=Enter complete or partial " + RETSLongName +
                                           "(s). Separate each with a comma (eg: )");
                            }
                        }

                        NewScript.Add("  transmit \"(" + SearchFieldList.getSystemNamefor(SearchFieldName) + "=*\"");
                        NewScript.Add("  SetField \"\\Fields." + SearchFieldName + "\\,FIRST\"");
                        NewScript.Add("  Label \"Next" + SearchFieldName + "\"");
                        NewScript.Add("  ifval \"\\Fields." + SearchFieldName + "\\,@\"");
                        NewScript.Add("     transval \"\\Fields." + SearchFieldName + "\\{ },@\"");
                        NewScript.Add("     SetField \"\\Fields." + SearchFieldName + "\\,NEXT\"");
                        NewScript.Add("     ifval \"\\Fields." + SearchFieldName + "\\,@\"");
                        NewScript.Add("         transmit \"*,*\"");
                        NewScript.Add("         goto \"Next" + SearchFieldName + "\"");
                        NewScript.Add("     endiv \"\"");
                        NewScript.Add("  endiv \"\"");
                        NewScript.Add("  transmit \"*)\"");
                        NewScript.Add("endiv \"\"");
                    }
                    else
                    {
                        if (!HPExists)
                        {
                            if (PredictiveHelpPanelCheck)
                            {
                                if (HelpPanelAllCapsCheckBoxState)
                                {
                                    NewDef.Add("HelpPanel=Enter complete  " + RETSLongName +
                                               "(s). Separate each with a comma (eg: " +
                                               PredictiveValues(StaticRETSLongName, 'C').ToUpper());
                                }
                                else
                                {
                                    NewDef.Add("HelpPanel=Enter complete " + RETSLongName +
                                               "(s). Separate each with a comma (eg: " +
                                               PredictiveValues(StaticRETSLongName, 'C'));
                                }
                            }
                            else
                            {
                                NewDef.Add("HelpPanel=Enter complete" + RETSLongName +
                                           "(s). Separate each with a comma (eg: ,)");
                            }
                        }
                        NewScript.Add("  transmit \"(" + SearchFieldList.getSystemNamefor(SearchFieldName) + "=\"");
                        NewScript.Add("  transval \"\\Fields." + SearchFieldName + "\\{ }\"");
                        NewScript.Add("  transmit \")\"");
                        NewScript.Add("endiv \"\"");
                    }
                }
                else if (DataType == "Int")
                {
                    NewDef.Add("Caption=" + RETSLongName);
                    NewDef.Add("Delimiter=,");
                    if (!HPExists)
                    {
                        if (PredictiveHelpPanelCheck)
                        {
                            NewDef.Add("HelpPanel=Enter the " + RETSLongName + PredictiveValues(StaticRETSLongName, 'I'));
                        }
                        else
                        {
                            NewDef.Add("HelpPanel=Enter the " + RETSLongName +
                                       "range (eg: ) or minimum value (eg: ) or maximum value (eg: ) or exact value (eg: )");
                        }
                    }
                    NewScript.Add("  transmit \"(" + SearchFieldList.getSystemNamefor(SearchFieldName) + "=\"");
                    NewScript.Add("  transval \"\\Fields." + SearchFieldName + "\\{ }\"");
                    NewScript.Add("  transmit \")\"");
                    NewScript.Add("endiv \"\"");
                }
                else if (DataType == "Decimal")
                {
                    NewDef.Add("Caption=" + RETSLongName);
                    NewDef.Add("Delimiter=,");
                    if (!HPExists)
                    {
                        if (PredictiveHelpPanelCheck)
                        {
                            NewDef.Add("HelpPanel=Enter the " + RETSLongName + PredictiveValues(StaticRETSLongName, 'D'));
                        }
                        else
                        {
                            NewDef.Add("HelpPanel=Enter the " + RETSLongName +
                                       "range (eg: ) or minimum value (eg: ) or maximum value (eg: ) or exact value (eg: )");
                        }
                    }
                    NewScript.Add("  transmit \"(" + SearchFieldList.getSystemNamefor(SearchFieldName) + "=\"");
                    NewScript.Add("  transval \"\\Fields." + SearchFieldName + "\\{ }\"");
                    NewScript.Add("  transmit \")\"");
                    NewScript.Add("endiv \"\"");
                }
                else if (DataType == "DateTime")
                {
                    NewDef.Add("Caption=" + RETSLongName);
                    NewDef.Add("ControlType=9");
                    NewDef.Add("SDateFormat=yyyy-MM-ddTHH:mm:ss");
                    NewDef.Add("Delimiter=-");
                    if (!HPExists)
                    {
                        NewDef.Add("HelpPanel=Select the Start Date and End Date.");
                    }
                    NewScript.Add("  transmit \"(" + SearchFieldList.getSystemNamefor(SearchFieldName) + "=\"");
                    NewScript.Add("  transval \"\\Fields." + SearchFieldName + "\\{ }\"");
                    NewScript.Add("  transmit \")\"");
                    NewScript.Add("endiv \"\"");
                }
                else if (DataType == "Date")
                {
                    NewDef.Add("Caption=" + RETSLongName);
                    NewDef.Add("ControlType=9");
                    NewDef.Add("SDateFormat=YYYY-MM-DD");
                    NewDef.Add("Delimiter=-");
                    if (!HPExists)
                    {
                        NewDef.Add("HelpPanel=Select the Start Date and End Date.");
                    }
                    NewScript.Add("  transmit \"(" + SearchFieldList.getSystemNamefor(SearchFieldName) + "=\"");
                    NewScript.Add("  transval \"\\Fields." + SearchFieldName + "\\{ }\"");
                    NewScript.Add("  transmit \")\"");
                    NewScript.Add("endiv \"\"");
                }
                else if (DataType == "Boolean")
                {
                    NewDef.Add("Caption=" + RETSLongName);
                    NewDef.Add("Delimiter=,");
                    NewDef.Add("ControlType=3");
                    NewDef.Add("Format=L");
                    if (!HPExists)
                    {
                        NewDef.Add(
                            "HelpPanel=Select 'Y' or 'N' from the drop down list.  Leave blank to search by both.");
                    }
                    NewDef.Add("Value1=Y");
                    NewDef.Add("Value2=N");
                    NewScript.Add("  transmit \"(" + SearchFieldList.getSystemNamefor(SearchFieldName) + "=\"");
                    NewScript.Add("  ifInField \"\\Fields." + SearchFieldName + "\\,Y\"");
                    NewScript.Add("     transmit \"1)\"");
                    NewScript.Add("  else \"\"");
                    NewScript.Add("     transmit \"0)\"");
                    NewScript.Add("endiv \"\"");
                }
            }

            //Set the definition of field and the scripts with the new instances
            SearchFieldList.updateTextFldDeffor(SearchFieldName, NewDef);
            SearchFieldList.updateTextFldScriptfor(SearchFieldName, NewScript, ScriptIndex);
        }

        public string LoadFldDefText(string FldNameIn)
        {
            //return String.Join(Environment.NewLine, DEFfile.getTextFldDef(FldNameIn));

            return String.Join(Environment.NewLine, SearchFieldList.getFldDEFfor(FldNameIn));
        }

        public string LoadFldScriptText(string FldNameIn, int index)
        {
            //return String.Join(Environment.NewLine, DEFfile.getTextFldScript(FldNameIn, index));

            return String.Join(Environment.NewLine, SearchFieldList.getFldScriptAtfor(FldNameIn, index));
        }

        public void DeleteSearchField(string FldNameIn)
        {
            //DEFfile.deleteSearchField(FldNameIn);

            SearchFieldList.deleteField(FldNameIn);
            //this.view.UpdateSearchFieldGrid(DEFfile.getDataTable());
        }

        public void ChangeOrder(string FldNameIn, string type)
        {
            var ChangeMade = SearchFieldList.EditFieldOrder(FldNameIn, type);
        }

        public void UpdateSearchFieldName(string oldname, string newname)
        {
            SearchFieldList.updateNamefor(oldname, newname);
            //DEFfile.updateSearchFldName(oldname, newname);
            //this.view.UpdateSearchFieldGrid(DEFfile.getDataTable());
        }

        public void AddSearchField(string NewName, string MetadataSystemName, bool PartialSearch, bool RapStyle,
                                   string DataType, string LookUpValue, string PicklistFormat, bool alphasort,
                                   bool Casing, bool ConCat)
        {
            var temp = new SearchField();

            //if newsearchname is not blank or already used, than use that, else don't do anything
            if ((!SearchFieldList.doesfieldExist(NewName)) && (NewName != ""))
            {
                temp.setFLDName(NewName);
                temp.setSystemName(MetadataSystemName);
                SearchFieldList.addField(temp);

                //Generate the Script and Def
                if (LookUpValue != "")
                {
                    UpdateSearchFieldPicklist(NewName, MetadataSystemName, PicklistFormat, alphasort, RapStyle, Casing,
                                              ConCat, 0);
                }
                else
                {
                    UpdateSearchField(NewName, MetadataSystemName, DataType, PartialSearch, RapStyle, 0, temp);
                }
            }

            //DEFfile.addSearchField(NewName, MetadataSystemName);
            //this.view.UpdateSearchFieldGrid(DEFfile.getDataTable());
        }

        public string UpdateNonStdFieldMapping(string SearchName, string NonStdName)
        {
            var outstring = SearchFieldList.updateNonSTDFldMapfor(SearchName, NonStdName);
            //this.view.UpdateSearchFieldGrid(SearchFieldList.getValuesTable());
            return outstring;
        }

        public void UpdateStdFieldMapping(string SearchName, string StdFldMapping)
        {
            SearchFieldList.updateSTDFldMapfor(SearchName, StdFldMapping);
        }

        public string UpdateTCSFieldMapping(string SearchName, string TCSFldName)
        {
            var outstring = SearchFieldList.updateTCSFldMapfor(SearchName, TCSFldName);
            return outstring;
        }

        public void SaveFiles()
        {
            if (_defLocation != "")
            {
                DefFile.SaveDefChanges(SearchFieldList.exportSearchFields(), SearchFieldList.exportDelSearchFields());
                SearchFieldList.clearDeleted();
                DefFile = new DEF(_defLocation);
                _classname = DefFile.getClassName();
            }
        }

        //public void SetTxtFldDef(string SearchNameIn)
        //{
        //    if (initialload == false)
        //    {
        //        SearchFieldList.updateTextFldDeffor(SearchNameIn, this.view.GetFieldDEFTextBox());
        //        //DEFfile.setTextFldDef(SearchNameIn, this.view.GetFieldDEFTextBox());
        //    }
        //}

        //public void SetTxtFldScript(string SearchNameIn, int index)
        //{
        //    if (initialload == false)
        //    {
        //        SearchFieldList.updateTextFldScriptfor(SearchNameIn, this.view.GetScriptTextBox(index), index);
        //        DEFfile.setTextFldScript(SearchNameIn, this.view.GetScriptTextBox(index), index);
        //    }
        //}

        public bool DoesFieldFollowPattern(string SearchNameIn)
        {
            return SearchFieldList.getSearchFieldPattern(SearchNameIn);
            //return DEFfile.getSearchFieldFollowPattern(SearchNameIn);
        }

        private string DEFfilename()
        {
            var splitlocation = _defLocation.Split(new[] {'\\'});

            return splitlocation[splitlocation.Length - 1];
            //return DEFfile.getSearchFieldFollowPattern(SearchNameIn);
        }

        //public string MetadataSepFilePath()
        //{
        //    string[] splitlocation = Metadatalocation.Split(new Char[] { '\\' });
        //    string builder = "";
        //    for (int i = 0; i <(splitlocation.Length-1); i++ )
        //    {
        //        if (builder == "")
        //        {
        //            builder = splitlocation[i];
        //        }
        //        else
        //        {
        //            builder = builder + "\\" + splitlocation[i];
        //        }
        //    }
        //    return builder;
        //    //return DEFfile.getSearchFieldFollowPattern(SearchNameIn);
        //}

        //public string DEFSepFilePath()
        //{
        //    string[] splitlocation = DEFlocation.Split(new Char[] { '\\' });
        //    string builder = "";
        //    for (int i = 0; i < (splitlocation.Length - 1); i++)
        //    {
        //        if (builder == "")
        //        {
        //            builder = splitlocation[i];
        //        }
        //        else
        //        {
        //            builder = builder + "\\" + splitlocation[i];
        //        }
        //    }
        //    return builder;
        //    //return DEFfile.getSearchFieldFollowPattern(SearchNameIn);
        //}


        //public string SharedFilePath()
        //{
        //    string[] splitlocation = Settings.Default.LastSharedPath.Split(new Char[] { '\\' });
        //    string builder = "";
        //    for (int i = 0; i < (splitlocation.Length - 1); i++)
        //    {
        //        if (builder == "")
        //        {
        //            builder = splitlocation[i];
        //        }
        //        else
        //        {
        //            builder = builder + "\\" + splitlocation[i];
        //        }
        //    }
        //    return builder;
        //    //return DEFfile.getSearchFieldFollowPattern(SearchNameIn);
        //}

        public void RemoveMinSearch()
        {
            SearchFieldList.updateRemoveMinSearchFieldReq();
        }

        public void clearAllFiles()
        {
            _classname = "";
            _metadatalocation = "";
            _defLocation = "";
            _defLoaded = false;
            _metaLoaded = false;
            _initialload = true;
        }

        public bool GetInitialload()
        {
            return _initialload;
        }

        public bool hasValue(string MetaSysName)
        {
            return SearchFieldList.doesSystemNameExist(MetaSysName);
        }

        public string MLSRecordExMapping(string MetaSysName)
        {
            return DefFile.getMLSRecordsEX(MetaSysName);
        }


        private string replaceSpecialChar(string inString)
        {
            inString = inString.Replace("&amp;", "&");
            inString = inString.Replace("&lt;", "<");
            inString = inString.Replace("&gt;", ">");
            inString = inString.Replace("&quot;", "\"");
            inString = inString.Replace("&apos;", "'");
            inString = inString.Replace(";", "~");
            inString = inString.Replace(",", "~");

            return inString;
        }

        private string ToTitleCase(string inString)
        {
            var txtInfo = CultureInfo.CurrentCulture.TextInfo;
            string outString;
            outString = txtInfo.ToTitleCase(inString.ToLower());
            return outString;
        }

        private IEnumerable<dynamic> RemoveDuplicates(IEnumerable<dynamic> LookupValues)
        {
            var p = LookupValues.Count();
            var DistinctLookups = LookupValues.GroupBy(x => x.LongValueCol).Select(x => x.First());
            var LongValues = new List<string>();
            var Values = new List<string>();

            foreach (var Entry in LookupValues)
            {
                LongValues.Add(Entry.LongValueCol);
                Values.Add(Entry.ValueCol);
            }


            var DistinctLookupsWithValues = from entry in DistinctLookups
                                            select new
                                                {
                                                    entry.LongValueCol,
                                                    entry.ShortValueCol,
                                                    ValueCol = CombinedValue(LongValues, Values, entry.LongValueCol)
                                                };

            return DistinctLookupsWithValues;
        }

        private string CombinedValue(List<string> longvalue, List<string> value, string name)
        {
            var combinedvalue = "";

            for (var i = 0; i < longvalue.Count; i++)
            {
                if (longvalue[i] == name)
                {
                    if (combinedvalue == "")
                    {
                        combinedvalue = value[i];
                    }
                    else
                    {
                        combinedvalue = combinedvalue + "," + value[i];
                    }
                }
            }
            return combinedvalue;
        }

        private string PredictiveValues(string RLN, char type)
        {
            //Type D = Decimal
            //Type I = Integer
            //Type C = Character
            //Type P = Partial Character

            var PanelValues = "";


            if (type == 'D')
            {
                PanelValues = " range (eg: ) or minimum value (eg: ) or maximum value (eg: ) or exact value (eg: )";

                //Price
                if ((RLN.ToLower().Contains("price")) &&
                    ((RLN.ToLower().Contains("search")) || (RLN.ToLower().Contains("asking")) ||
                     (RLN.ToLower().Contains("original")) || (RLN.ToLower().Contains("sold")) ||
                     (RLN.ToLower().Contains("list")) || (RLN.ToLower().Contains("sale")) ||
                     (RLN.ToLower().Contains("sell")) || (RLN.ToLower().Contains("close")) ||
                     (RLN.ToLower().Contains("final")) || (RLN.ToLower().Contains("sort")) ||
                     (RLN.ToLower().Contains("previous"))) &&
                    ((!RLN.ToLower().Contains("rent")) && (!RLN.ToLower().Contains("lease"))))
                {
                    PanelValues =
                        " range (eg: 150000-600000 ) or minimum value (eg: 150000+ ) or maximum value (eg: 600000- ) or exact value (eg: 425000 )";
                }
                    //Rental Price
                else if ((RLN.ToLower().Contains("price")) &&
                         ((RLN.ToLower().Contains("rent")) || (RLN.ToLower().Contains("lease"))))
                {
                    PanelValues =
                        " range (eg: 500-2000 ) or minimum value (eg: 2000+ ) or maximum value (eg: 500- ) or exact value (eg: 745 )";
                }
                    //Zip Code
                else if (((RLN.ToLower().Contains("zip")) || (RLN.ToLower().Contains("postal"))))
                {
                    PanelValues =
                        " range (eg: 01001-99950 ) or minimum value (eg: 32118+ ) or maximum value (eg: 73301- ) or exact value (eg: 43055 )";
                }
                    //Bed & Bath
                else if (((RLN.ToLower().Contains("bed")) || (RLN.ToLower().Contains("bath"))))
                {
                    PanelValues =
                        " range (eg: 1.0-5.5 ) or minimum value (eg: 2.5+ ) or maximum value (eg: 4.0- ) or exact value (eg: 3.0 )";
                }
                    //Features
                else if (((RLN.ToLower().Contains("num")) || (RLN.ToLower().Contains("no")) ||
                          (RLN.ToLower().Contains("#"))) &&
                         ((RLN.ToLower().Contains("fireplace")) || (RLN.ToLower().Contains("room")) ||
                          (RLN.ToLower().Contains("building")) || (RLN.ToLower().Contains("fp"))))
                {
                    PanelValues =
                        " range (eg: 1.0-5.0 ) or minimum value (eg: 2.0+ ) or maximum value (eg: 4.0- ) or exact value (eg: 3.0 )";
                }
                    //Lot SqFt
                else if (((RLN.ToLower().Contains("lot")) || (RLN.ToLower().Contains("exterior"))) &&
                         ((RLN.ToLower().Contains("square feet")) || (RLN.ToLower().Contains("in feet")) ||
                          (RLN.ToLower().Contains("sqft")) || (RLN.ToLower().Contains("square foot")) ||
                          (RLN.ToLower().Contains("footage"))))
                {
                    PanelValues =
                        " range (eg: 3000.0-9773.6 ) or minimum value (eg: 2000.0+ ) or maximum value (eg: 8000.0- ) or exact value (eg: 5678.4 )";
                }
                    //SqFt
                else if (((RLN.ToLower().Contains("sqft")) || (RLN.ToLower().Contains("square feet")) ||
                          (RLN.ToLower().Contains("square foot")) || (RLN.ToLower().Contains("in feet")) ||
                          (RLN.ToLower().Contains("footage"))))
                {
                    PanelValues =
                        " range (eg: 1000-2500 ) or minimum value (eg: 1000+ ) or maximum value (eg: 2000- ) or exact value (eg: 1750 )";
                }
                    //Acres
                else if (((RLN.ToLower().Contains("acre"))))
                {
                    PanelValues =
                        " range (eg: 1.54-6.2 ) or minimum value (eg: 1.0+ ) or maximum value (eg: 5.3- ) or exact value (eg: 4.7 )";
                }
                    //Taxes
                else if (((RLN.ToLower().Contains("tax")) || (RLN.ToLower().Contains("fee"))))
                {
                    PanelValues =
                        " range (eg: 1700.00-4000.00 ) or minimum value (eg: 2000.00+ ) or maximum value (eg: 4000.00- ) or exact value (eg: 3045.00 )";
                }
                    //Year Built
                else if (((RLN.ToLower().Contains("built")) || (RLN.ToLower().Contains("blt"))) &&
                         ((RLN.ToLower().Contains("year")) || (RLN.ToLower().Contains("yr"))))
                {
                    PanelValues =
                        " range (eg: 1980-2010 ) or minimum value (eg: 1980+ ) or maximum value (eg: 2003- ) or exact value (eg: 1997 )";
                }
                    //Garage Spaces
                else if (((RLN.ToLower().Contains("space")) || (RLN.ToLower().Contains("capac"))) &&
                         ((RLN.ToLower().Contains("garage")) || (RLN.ToLower().Contains("parking"))))
                {
                    PanelValues =
                        " range (eg: 2.0-5.0 ) or minimum value (eg: 2.0+ ) or maximum value (eg: 4.0- ) or exact value (eg: 3.5 )";
                }
                    //Stories
                else if (((RLN.ToLower().Contains("stories")) || (RLN.ToLower().Contains("levels")) ||
                          (RLN.ToLower().Contains("floors"))))
                {
                    PanelValues =
                        " range (eg: 2.0-4.0 ) or minimum value (eg: 2.0+ ) or maximum value (eg: 4.0- ) or exact value (eg: 1.5 )";
                }
                    //Age
                else if (((RLN.ToLower().Contains("age")) && (!RLN.ToLower().Contains("garage"))))
                {
                    PanelValues =
                        " range (eg: 2-40 ) or minimum value (eg: 2+ ) or maximum value (eg: 40- ) or exact value (eg: 24 )";
                }
                    //DOM
                else if (((RLN.ToLower().Contains("on market")) || (RLN.ToLower().Contains("dom")) ||
                          (RLN.ToLower().Contains("cdom")) || (RLN.ToLower().Contains("cumulative"))))
                {
                    PanelValues =
                        " range (eg: 30-356 ) or minimum value (eg: 7+ ) or maximum value (eg: 60- ) or exact value (eg: 150 )";
                }
                    //Unit/Lot/Street Number
                else if (((RLN.ToLower().Contains("number")) || (RLN.ToLower().Contains("no")) ||
                          (RLN.ToLower().Contains("#"))) &&
                         ((RLN.ToLower().Contains("unit")) || (RLN.ToLower().Contains("lot")) ||
                          (RLN.ToLower().Contains("street")) || (RLN.ToLower().Contains("st")) ||
                          (RLN.ToLower().Contains("address"))))
                {
                    PanelValues =
                        " range (eg: 120-805 ) or minimum value (eg: 140+ ) or maximum value (eg: 301- ) or exact value (eg: 312 )";
                }
            }
            else if (type == 'I')
            {
                PanelValues = " range (eg: ) or minimum value (eg: ) or maximum value (eg: ) or exact value (eg: )";

                //Price
                if ((RLN.ToLower().Contains("price")) &&
                    ((RLN.ToLower().Contains("search")) || (RLN.ToLower().Contains("asking")) ||
                     (RLN.ToLower().Contains("original")) || (RLN.ToLower().Contains("sold")) ||
                     (RLN.ToLower().Contains("list")) || (RLN.ToLower().Contains("sale")) ||
                     (RLN.ToLower().Contains("sell")) || (RLN.ToLower().Contains("close")) ||
                     (RLN.ToLower().Contains("final")) || (RLN.ToLower().Contains("sort")) ||
                     (RLN.ToLower().Contains("previous"))) &&
                    ((!RLN.ToLower().Contains("rent")) && (!RLN.ToLower().Contains("lease"))))
                {
                    PanelValues =
                        " range (eg: 150000-600000 ) or minimum value (eg: 150000+ ) or maximum value (eg: 600000- ) or exact value (eg: 425000 )";
                }
                    //Rental Price
                else if ((RLN.ToLower().Contains("price")) &&
                         ((RLN.ToLower().Contains("rent")) || (RLN.ToLower().Contains("lease"))))
                {
                    PanelValues =
                        " range (eg: 500-2000 ) or minimum value (eg: 2000+ ) or maximum value (eg: 500- ) or exact value (eg: 745 )";
                }
                    //Zip Code
                else if (((RLN.ToLower().Contains("zip")) || (RLN.ToLower().Contains("postal"))))
                {
                    PanelValues =
                        " range (eg: 01001-99950 ) or minimum value (eg: 32118+ ) or maximum value (eg: 73301- ) or exact value (eg: 43055 )";
                }
                    //Bed & Bath
                else if (((RLN.ToLower().Contains("bed")) || (RLN.ToLower().Contains("bath"))))
                {
                    PanelValues =
                        " range (eg: 1-5 ) or minimum value (eg: 2+ ) or maximum value (eg: 4- ) or exact value (eg: 3 )";
                }
                    //Features
                else if (((RLN.ToLower().Contains("num")) || (RLN.ToLower().Contains("no")) ||
                          (RLN.ToLower().Contains("#"))) &&
                         ((RLN.ToLower().Contains("fireplace")) || (RLN.ToLower().Contains("room")) ||
                          (RLN.ToLower().Contains("building")) || (RLN.ToLower().Contains("fp"))))
                {
                    PanelValues =
                        " range (eg: 1-5 ) or minimum value (eg: 2+ ) or maximum value (eg: 4.0- ) or exact value (eg: 3 )";
                }
                    //Lot SqFt
                else if (((RLN.ToLower().Contains("lot")) || (RLN.ToLower().Contains("exterior"))) &&
                         ((RLN.ToLower().Contains("square feet")) || (RLN.ToLower().Contains("in feet")) ||
                          (RLN.ToLower().Contains("sqft")) || (RLN.ToLower().Contains("square foot")) ||
                          (RLN.ToLower().Contains("footage"))))
                {
                    PanelValues =
                        " range (eg: 3000-9773 ) or minimum value (eg: 2000+ ) or maximum value (eg: 8000- ) or exact value (eg: 5678 )";
                }
                    //SqFt
                else if (((RLN.ToLower().Contains("sqft")) || (RLN.ToLower().Contains("square feet")) ||
                          (RLN.ToLower().Contains("square foot")) || (RLN.ToLower().Contains("in feet")) ||
                          (RLN.ToLower().Contains("footage"))))
                {
                    PanelValues =
                        " range (eg: 1000-2500 ) or minimum value (eg: 1000+ ) or maximum value (eg: 2000- ) or exact value (eg: 1750 )";
                }
                    //Acres
                else if (((RLN.ToLower().Contains("acre"))))
                {
                    PanelValues =
                        " range (eg: 1-6 ) or minimum value (eg: 1+ ) or maximum value (eg: 5- ) or exact value (eg: 4 )";
                }
                    //Taxes
                else if (((RLN.ToLower().Contains("tax")) || (RLN.ToLower().Contains("fee"))))
                {
                    PanelValues =
                        " range (eg: 1700-4000 ) or minimum value (eg: 2000+ ) or maximum value (eg: 4000- ) or exact value (eg: 3045 )";
                }
                    //Year Built
                else if (((RLN.ToLower().Contains("built")) || (RLN.ToLower().Contains("blt"))) &&
                         ((RLN.ToLower().Contains("year")) || (RLN.ToLower().Contains("yr"))))
                {
                    PanelValues =
                        " range (eg: 1980-2010 ) or minimum value (eg: 1980+ ) or maximum value (eg: 2003- ) or exact value (eg: 1997 )";
                }
                    //Garage Spaces
                else if (((RLN.ToLower().Contains("space")) || (RLN.ToLower().Contains("capac"))) &&
                         ((RLN.ToLower().Contains("garage")) || (RLN.ToLower().Contains("parking"))))
                {
                    PanelValues =
                        " range (eg: 2-5 ) or minimum value (eg: 2+ ) or maximum value (eg: 4- ) or exact value (eg: 3 )";
                }
                    //Stories
                else if (((RLN.ToLower().Contains("stories")) || (RLN.ToLower().Contains("levels")) ||
                          (RLN.ToLower().Contains("floors"))))
                {
                    PanelValues =
                        " range (eg: 2-4 ) or minimum value (eg: 2+ ) or maximum value (eg: 4- ) or exact value (eg: 1 )";
                }
                    //Age
                else if (((RLN.ToLower().Contains("age")) && (!RLN.ToLower().Contains("garage"))))
                {
                    PanelValues =
                        " range (eg: 2-40 ) or minimum value (eg: 2+ ) or maximum value (eg: 40- ) or exact value (eg: 24 )";
                }
                    //DOM
                else if (((RLN.ToLower().Contains("on market")) || (RLN.ToLower().Contains("dom")) ||
                          (RLN.ToLower().Contains("cdom")) || (RLN.ToLower().Contains("cumulative"))))
                {
                    PanelValues =
                        " range (eg: 30-356 ) or minimum value (eg: 7+ ) or maximum value (eg: 60- ) or exact value (eg: 150 )";
                }
                    //Unit/Lot/Street Number
                else if (((RLN.ToLower().Contains("number")) || (RLN.ToLower().Contains("no")) ||
                          (RLN.ToLower().Contains("#"))) &&
                         ((RLN.ToLower().Contains("unit")) || (RLN.ToLower().Contains("lot")) ||
                          (RLN.ToLower().Contains("street")) || (RLN.ToLower().Contains("st")) ||
                          (RLN.ToLower().Contains("address"))))
                {
                    PanelValues =
                        " range (eg: 120-805 ) or minimum value (eg: 140+ ) or maximum value (eg: 301- ) or exact value (eg: 312 )";
                }
            }
            else if ((type == 'C') || (type == 'P'))
            {
                PanelValues = ")";

                //Lake/Waterfront Name
                if ((RLN.ToLower().Contains("name")) &&
                    ((RLN.ToLower().Contains("lake")) || (RLN.ToLower().Contains("water"))))
                {
                    PanelValues = "Greenwood Lake,Highwater)";
                }
                    //County
                else if (((RLN.ToLower().Contains("county"))))
                {
                    PanelValues = "Morgan,San Juan)";
                }
                    //City
                else if (((RLN.ToLower().Contains("city"))))
                {
                    PanelValues = "Springfield,Greenville)";
                }
                    //Street Number
                else if (((RLN.ToLower().Contains("number")) || (RLN.ToLower().Contains("no")) ||
                          (RLN.ToLower().Contains("#"))) &&
                         ((RLN.ToLower().Contains("street")) || (RLN.ToLower().Contains("st"))))
                {
                    PanelValues = "50205,26039)";
                }
                    //Street Name
                else if (((RLN.ToLower().Contains("name"))) &&
                         ((RLN.ToLower().Contains("street")) || (RLN.ToLower().Contains("st"))))
                {
                    PanelValues = "Oak,Franklin)";
                }
                    //Unit/Lot Number
                else if (((RLN.ToLower().Contains("number")) || (RLN.ToLower().Contains("no")) ||
                          (RLN.ToLower().Contains("#"))) &&
                         ((RLN.ToLower().Contains("unit")) || (RLN.ToLower().Contains("lot"))))
                {
                    PanelValues = "12,703A)";
                }
                    //Zoning
                else if (((RLN.ToLower().Contains("zoning"))))
                {
                    PanelValues = "Medium,Apartment)";
                }
                    //Subdivision
                else if (((RLN.ToLower().Contains("subdiv"))))
                {
                    PanelValues = "Gable Crest,Laurel)";
                }
                    //Elementary School
                else if (((RLN.ToLower().Contains("school"))) &&
                         ((RLN.ToLower().Contains("elem")) || (RLN.ToLower().Contains("primary"))))
                {
                    PanelValues = "Rosemont,Queen Anne)";
                }
                    //Middle School
                else if (((RLN.ToLower().Contains("school"))) &&
                         ((RLN.ToLower().Contains("middle"))))
                {
                    PanelValues = "Antelope,Highland)";
                }
                    //Junior High School
                else if (((RLN.ToLower().Contains("school"))) &&
                         ((RLN.ToLower().Contains("high"))) &&
                         ((RLN.ToLower().Contains("junior")) || (RLN.ToLower().Contains("jr"))))
                {
                    PanelValues = "Quartz Hill,Jones)";
                }
                    //High School
                else if (((RLN.ToLower().Contains("school"))) &&
                         ((RLN.ToLower().Contains("high"))))
                {
                    PanelValues = "Centennial,Roberts)";
                }
                    //School District
                else if (((RLN.ToLower().Contains("school"))) &&
                         ((RLN.ToLower().Contains("district"))))
                {
                    PanelValues = "Morningway,Clark)";
                }
                    //State
                else if (((RLN.ToLower().Contains("state"))))
                {
                    PanelValues = "New Jersey,Virgina)";
                }
                    //Zip Code
                else if (((RLN.ToLower().Contains("zip")) || (RLN.ToLower().Contains("postal"))))
                {
                    PanelValues = "73301,43055)";
                }
                    //Street Suffix
                else if (((RLN.ToLower().Contains("suffix")) || (RLN.ToLower().Contains("type"))) &&
                         ((RLN.ToLower().Contains("street")) || (RLN.ToLower().Contains("st"))))
                {
                    PanelValues = "Avenue,Lane)";
                }
                    //Street Direction
                else if (((RLN.ToLower().Contains("dir")) || (RLN.ToLower().Contains("prefix"))) &&
                         ((RLN.ToLower().Contains("street")) || (RLN.ToLower().Contains("st"))))
                {
                    PanelValues = "North,W)";
                }
            }

            return PanelValues;
        }
    }
}