using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.ObjectModel;

namespace SpeedUp.Model
{
    public class DEF
    {
        //Overview: DEF is a mutable class which represents the parsed search fields and it's related features of a DEF file

        //Attributes
        private List<SearchField> SearchFieldList = new List<SearchField>();
        private List<SearchField> DeletedSearchFieldList = new List<SearchField>();
        private List<SearchField> NewSearchFieldList = new List<SearchField>();
        private List<String>[] MLSRecordsExList = new List<String>[] { new List<string>(), new List<string>() };
        private ObservableCollection<MlsResultField> _mlsResultFieldCollection = new ObservableCollection<MlsResultField>(); 
        private string _className;
        private string _resourceName;

        private string FileLocation;
        private string _fileName;
        private string _standarClassName = "";
        public static string[] STDFieldNameList = new string[] { 
                                                                    "ST_PType",
                                                                    "ST_MLSNo",
                                                                    "ST_Status",
                                                                    "ST_PostalFSA",
                                                                    "ST_Zip",
                                                                    "ST_Beds",
                                                                    "ST_TBaths",
                                                                    "ST_Fbaths",
                                                                    "ST_SqFt",
                                                                    "ST_Area",
                                                                    "ST_ListDate",
                                                                    "ST_ListPr",
                                                                    "ST_SaleDate",
                                                                    "ST_SalePrice",
                                                                    "ST_SearchPrice",
                                                                    "ST_StatusDate",
                                                                    "ST_Lat",
                                                                    "ST_Long",
                                                                    "ST_ListAgentID",
                                                                    "ST_ListBrokerID",
                                                                    "ST_ExpiredDate",
                                                                    "ST_InactiveDate",
                                                                    "ST_PendingDate",
                                                                    "ST_Subdivision",
                                                                    "ST_SaleOrLease",
                                                                    "ST_LastMod",
                                                                    "ST_PicMod", };
        
        public static string[] STDFSearchFieldNameKeepForRdcConvertList = new string[] { 
                                                                    "ST_MLSNo",
                                                                    "ST_Status",
                                                                    "ST_ListDate",
                                                                    "ST_ListPr",
                                                                    "ST_LastMod",
                                                                    "ST_PicMod", };

        public static string[] SectionList = new string[]
            {
                "Comments",
                "Common",
                "TPOnline",
                "TcpIp",
                "MLSRules",
                "Advanced_Chunking",
                "Standard_Search",
                "LTSPType",
                "LTSPTypeResults",
                "Fields_TCS",
                "Fields",
                "PicScript",
                "Pictures",
                "SearchFieldList",
                "SecList",
                "MainScript",
                "RcvData",
                "Sorting",
                "SortingPublicListingStatus",
                "MLSRecordsEx",
                "ResultFieldGroupList",
                "CmaFieldList",
                "ResultScript",
            };

        private Dictionary<string, string> _sectionContentDict = new Dictionary<string, string>();
        private string _vendorName;
        //Constructor
        public DEF(string filePath)
        {
            var iniDef = new IniFile(filePath);
            VendorName = iniDef.Read("NameOfOrigin", "Common");
            _fileName = Path.GetFileName(filePath);
            foreach (var item in SectionList)
            {
                string setionContent = iniDef.GetSectionContent(item);
                if (!string.IsNullOrEmpty(setionContent) && setionContent.IndexOf("[") > -1)
                {
                    setionContent = setionContent.Substring(0, setionContent.IndexOf("["));
                }
                SectionContentDict[item]=setionContent.Trim();
            }
            iniDef = null;
            List<string> StandardSearchFields = new List<string>();
            List<string> TCSSearchFields = new List<string>();
            List<string> NonStandardSearchFields = new List<string>();
            List<string> MLSRecordsEx = new List<string>();

            FileLocation = filePath;

            using (StreamReader reader = new StreamReader(filePath, Encoding.GetEncoding("iso-8859-1")))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Contains("[Standard_Search]") && (!line.StartsWith(";")))
                    {
                        while ((line = reader.ReadLine().Trim()) != "")
                        {
                            if ((!line.StartsWith(";")))
                            {
                                StandardSearchFields.Add(line); // Add to list.
                            }

                        }
                    }

                    if (line.Contains("[Fields_TCS]") && (!line.StartsWith(";")))
                    {
                        while ((line = reader.ReadLine().Trim()) != "")
                        {
                            if ((!line.StartsWith(";")))
                            {
                                TCSSearchFields.Add(line); // Add to list.
                            }
                        }
                    }

                    if (line.Contains("[Fields]") && (!line.StartsWith(";")))

                    {
                        while ((line = reader.ReadLine().Trim()) != "")
                        {
                            if ((!line.StartsWith(";")))
                            {
                                NonStandardSearchFields.Add(line); // Add to list.
                            }
                        }
                    }
                    
                    if (line.Contains("Class=") && (!line.StartsWith(";")))
                    {
                        int startindex = line.LastIndexOf("Class=");
                        int endindex = -1;
                        startindex = startindex + 6;


                        for (int i = startindex; i < line.Length; i++)
                        {
                            if (line[i] == '&')
                            {
                                endindex = i;
                                break;
                            }
                        }

                        if (line[startindex] == '"')
                        {
                            line = reader.ReadLine();

                            int newindex = line.LastIndexOf("transmit \"");
                            newindex = newindex + 10;
                            int secondendindex = line.Length - 1;
                            for (int j = newindex; j < line.Length; j++)
                            {
                                if ((line[j] == '&') || (line[j] == '"'))
                                {
                                    secondendindex = j;
                                    break;
                                }
                            }

                            ClassName = line.Substring(newindex, secondendindex - newindex);

                        }
                        else if (endindex != -1)
                        {
                            ClassName = line.Substring(startindex, endindex - startindex);
                        }
                        else
                        {
                            ClassName = line.Substring(startindex, line.Length - 1 - startindex);
                        }

                    }

                    if (line.Contains("[MLSRecordsEx]") && (!line.StartsWith(";")))
                    {
                        while ((line = reader.ReadLine().Trim()) != "")
                        {
                            if ((!line.StartsWith(";")))
                            {
                                MLSRecordsEx.Add(line); // Add to list.
                            }
                        }
                    }


                }
            }

            this.SearchFieldListGenerator(StandardSearchFields, TCSSearchFields, NonStandardSearchFields);
            this.SearchFieldProperties(filePath);
            this.DisplayMappingParsing(MLSRecordsEx);
        }

        public Dictionary<string, string> SectionContentDict
        {
            get { return _sectionContentDict; }
            set { _sectionContentDict = value; }
        }

        public string ClassName
        {
            get { return _className; }
            set { _className = value; }
        }

        public string ResourceName
        {
            get { return _resourceName; }
            set { _resourceName = value; }
        }

        public string FileName
        {
            get { return _fileName; }
            set { _fileName = value; }
        }

        public string VendorName
        {
            get { return _vendorName; }
            set { _vendorName = value; }
        }

        public ObservableCollection<MlsResultField> MlsResultFieldCollection
        {
            get { return _mlsResultFieldCollection; }
            set { _mlsResultFieldCollection = value; }
        }

        private void DisplayMappingParsing(List<string> MLSRecords)
        {
            //First List should be for System Name
            //Second List should be for the Mappings

            //Get All System Names
            foreach (string Line in MLSRecords)
            {
                try
                {
                    if ((!Line.StartsWith(";")) && (Line.StartsWith("@")))
                    {
                        string[] parts = Line.Split('"');
                        MLSRecordsExList[0].Add(parts[1]);
                    }

                }
                catch (NullReferenceException) { }

            }

            //Remove Duplicates
            MLSRecordsExList[0] = MLSRecordsExList[0].Distinct().ToList();

            //Populate the matching list
            foreach (string Line in MLSRecordsExList[0])
            {
                MLSRecordsExList[1].Add("");
            }

            //Match all One to One Mappings
            foreach (string Line in MLSRecords)
            {
                try
                {
                    if ((!Line.StartsWith(";")) && (Line.StartsWith("@")))
                    {
                        string[] partsStageTwo = Line.Split(new Char[] { '=', ',', '"' });
                        int index = 0;
                        foreach (string SystemName in MLSRecordsExList[0])
                        {
                            if (MLSRecordsExList[0][index] == partsStageTwo[1])
                            {
                                string MappingName = "";
                                if (partsStageTwo[3].ToLower() == "recname")
                                {
                                    MappingName = partsStageTwo[6];
                                }
                                else
                                {
                                    MappingName = partsStageTwo[4];
                                }

                                if (MLSRecordsExList[1][index] != "")
                                {
                                    string Mappings = MLSRecordsExList[1][index];
                                    Mappings = Mappings + ", " + MappingName;
                                    MLSRecordsExList[1].RemoveAt(index);
                                    MLSRecordsExList[1].Insert(index, Mappings);
                                }
                                else
                                {
                                    MLSRecordsExList[1].RemoveAt(index);
                                    MLSRecordsExList[1].Insert(index, MappingName);
                                }

                                break;
                            }
                            index++;
                        }
                        //MLSRecordsExList[1].Add(partsStageTwo[1]);
                    }

                }
                catch (NullReferenceException) { }

            }


            //Gather and Match Variable Fields
            foreach (string Line in MLSRecords)
            {
                try
                {
                    if ((!Line.StartsWith(";")) && (Line.StartsWith("\\")))
                    {
                        string[] partsStageThree = Line.Split(new Char[] {  '\\', '"' });
                        int index = 0;
                        string variablesRaw = partsStageThree[1];
                        string attributesRaw = partsStageThree[3];

                        string[] variables = variablesRaw.Split(new Char[] {','});
                        string[] attributes = attributesRaw.Split(new Char[] { '=', ',', '"' });
                        string mapping = "";

                        for (int i = 0; i < attributes.Count(); i++)
                        {
                            if (attributes[i].Trim().ToLower() == "fldname")
                            {
                                try
                                {
                                    mapping = attributes[i + 1];
                                }
                                catch (IndexOutOfRangeException) { }
                                break;
                            }
                        }

                        foreach (string varMapping in variables)
                        {
                            index = 0;
                            foreach (string entry in MLSRecordsExList[0])
                            {
                                string[] CurMappingSplit = MLSRecordsExList[1][index].Split(new Char[] { ',' });

                                for (int j = 0; j < CurMappingSplit.Count(); j++)
                                {
                                    if ((CurMappingSplit[j].Trim() == varMapping) && (!MLSRecordsExList[1][index].Contains(mapping)))
                                    {
                                        string MappingsAll = MLSRecordsExList[1][index];
                                        MappingsAll = MappingsAll + ", " + mapping;
                                        MLSRecordsExList[1].RemoveAt(index);
                                        MLSRecordsExList[1].Insert(index, MappingsAll);
                                    }
                                }
                                index++;
                            }

                        }
                    }

                }
                catch (NullReferenceException)
                {
                }
            }

        }

        //Helper Functions
        private void SearchFieldListGenerator(List<string> Standard, List<string> TCS, List<string> NonStandard)
        {
            int NonSTDCounter = 1;
            SearchField[] varies = new SearchField[200];
            for (int i = 0; i < 200; i++){
                varies[i] = new SearchField();
            }


            int index = 0;
            int fieldflag = 0;
            int indexstop = 0;
            int skipadd = 0;
            bool exists = false;
            foreach (string field in Standard)
            {
                string[] parts = field.Split('=');
                if (parts[1].Trim() != "")
                {
                    if ((!parts[0].Contains(";")) || (!parts[1].Contains(";")))
                    {
                        for (int i = 0; i < SearchFieldList.Count; i++)
                        {
                            if (SearchFieldList[i].getFldName().ToLower() == parts[1].ToLower())
                            {
                                skipadd = 1;
                                break;
                            }
                        }
                        if (skipadd == 0)
                        {
                            varies[index].setFLDName(parts[1]);

                            for (int zzz = 0; zzz < STDFieldNameList.Count(); zzz++)
                            {
                                if (STDFieldNameList[zzz].ToLower() == parts[0].ToLower())
                                {
                                    varies[index].setSTDFldMapping(STDFieldNameList[zzz]);
                                    exists = true;
                                }
                            }

                            if (!exists)
                            {
                                varies[index].setSTDFldMapping("");
                            }

                            SearchFieldList.Add(varies[index]);

                            exists = false;
                        }
                        skipadd = 0;
                        index++;
                    }
                }
            }

            foreach (string field2 in TCS)
            {
                string[] parts2 = field2.Split('=');
                if (parts2[1].Trim() != "")
                {
                    for (int i = 0; i < SearchFieldList.Count; i++)
                    {
                        if (SearchFieldList[i].getFldName().ToLower() == parts2[0].ToLower())
                        {
                            fieldflag = 1;
                            indexstop = i;
                            break;
                        }
                    }

                    if ((!parts2[0].Contains(";")) || (!parts2[1].Contains(";")))
                    {
                        if (fieldflag == 1)
                        {
                            SearchFieldList[indexstop].setTCSFldMapping(parts2[1]);
                        }
                        else
                        {
                            varies[index].setFLDName(parts2[0]);
                            varies[index].setTCSFldMapping(parts2[1]);
                            SearchFieldList.Add(varies[index]);
                            index++;
                        }
                    }

                    fieldflag = 0;

                }
            }

            foreach (string field3 in NonStandard)
            {
                string[] parts3 = field3.Split('=');
                if (parts3[1].Trim() != "")
                {
                    for (int j = 0; j < SearchFieldList.Count; j++)
                    {

                    }



                    for (int i = 0; i < SearchFieldList.Count; i++)
                    {
                        if (SearchFieldList[i].getFldName().ToLower() == parts3[0].ToLower())
                        {
                            fieldflag = 1;
                            indexstop = i;
                            break;
                        }
                    }

                     if ((!parts3[0].Contains(";")) || (!parts3[1].Contains(";")))
                     {
                        if (fieldflag == 1)
                        {
                            SearchFieldList[indexstop].setNonSTDFldMapping(parts3[1]);
                            SearchFieldList[indexstop].setNonSTDPos(NonSTDCounter);
                            NonSTDCounter++;
                        }
                        else
                        {
                            varies[index].setFLDName(parts3[0]);
                            varies[index].setNonSTDFldMapping(parts3[1]);
                            varies[index].setNonSTDPos(NonSTDCounter);
                            SearchFieldList.Add(varies[index]);
                            index++;
                            NonSTDCounter++;
                        }
                    }
                    fieldflag = 0;
                }
            }

        }

        private void SearchFieldProperties(string filePath)
        {
            int IFcounter = 0;
            List<string> TempList = new List<string>();


            for (int i = 0; i < SearchFieldList.Count; i++)
            {
                using (StreamReader reader = new StreamReader(filePath,Encoding.Default))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Equals("[field_" + SearchFieldList[i].getFldName() + "]", StringComparison.InvariantCultureIgnoreCase))
                        {
                            while ((line = reader.ReadLine().Trim()) != "")
                            {
                                SearchFieldList[i].addFldDef(line); // Add to list.

                            }
                        }


                        if ((line.ToLower().Contains("ifval") || line.ToLower().Contains("ifnval")) && line.ToLower().Contains("\"\\fields." + SearchFieldList[i].getFldName().ToLower() + "\\\"") && (!line.StartsWith(";")))
                        {
                            TempList.Add(line);
                            IFcounter = 1;
                            while ((line = reader.ReadLine()) != "")
                            {
                                if ((!line.StartsWith(";")) && ((line.ToLower().Contains("ifval")) || (line.ToLower().Contains("ifnval")) || (line.ToLower().Contains("ifinc")) || (line.ToLower().Contains("ifinfield"))))
                                {
                                    IFcounter++;
                                }

                                if ((!line.StartsWith(";")) && (line.ToLower().Contains("endiv")))
                                {
                                    IFcounter = IFcounter - 1;
                                }

                                TempList.Add(line);


                                if (((!line.StartsWith(";")) && (line.ToLower().Contains("transmit \"("))) || ((!line.StartsWith(";")) && (line.ToLower().Contains("transmit \",("))))
                                {
                                    char[] parsedline = new char[line.Length];
                                    string systemname = null;

                                    using (StringReader sr = new StringReader(line))
                                    {
                                        sr.Read(parsedline, 0, line.Length);
                                    }

                                    for (int j = 0; j < parsedline.Length; j++)
                                    {
                                        if (parsedline[j] == '(')
                                        {
                                            for (int k = j + 1; (parsedline[k] != '=') && (parsedline[k] != '"'); k++)
                                            {
                                                systemname = systemname + parsedline[k];
                                               //char[] systemnameparsed = new char[line.Length];
                                            }
                                        }
                                    }
                                    SearchFieldList[i].setSystemName(systemname);
                                }
                                if (IFcounter == 0)
                                {
                                    break;
                                }

                            }
                            SearchFieldList[i].addFldScript(TempList);
                            TempList.Clear();
                        }
                    }
                }
            }
        }

        public List<SearchField> getOrigSearchFields()
        {
            return this.SearchFieldList;
        }

        public string getClassName()
        {
            //EFFECTS: Returns the name of this Class which the DEF is built for(this.ClassName)

            string nameOut = this.ClassName;
            return nameOut;
        }

        public void SaveDefChanges(List<SearchField> Fields, List<SearchField> DeletedFields)
        {
            SearchFieldList = Fields;
            DeletedSearchFieldList = DeletedFields;
            //read def into string array than to List<string>
            StringBuilder newFile = new StringBuilder();
            string[] file = File.ReadAllLines(FileLocation);
            List<string> filelist = new List<string>(file);
            int RemoveIndex = 0; 
            int RemoveCount = 0;
            int IFcounter = 1;
            bool exists1 = false;
            bool exists2 = false;
            bool exists3 = false;
            bool exists4 = false;
            bool exists5 = false;

            for (int i = 0; i < DeletedSearchFieldList.Count; i++)
            {

                for (int j = 0; j < filelist.Count; j++)
                {
                    if (filelist[j].Contains("[Standard_Search]") && (!filelist[j].StartsWith(";")))
                    {
                        while (filelist[j].Trim() != "")
                        {
                            if ((filelist[j].ToLower().StartsWith(DeletedSearchFieldList[i].getSTDFldMapping().ToLower() + "=")) && (DeletedSearchFieldList[i].getSTDFldMapping() != ""))
                            {
                                filelist.Insert(j, DeletedSearchFieldList[i].getSTDFldMapping() + "=");
                                filelist.RemoveAt(j + 1);
                            }
                            if ((filelist[j].ToLower().Contains("=" + DeletedSearchFieldList[i].getFldName().ToLower())) && (DeletedSearchFieldList[i].getSTDFldMapping() == ""))
                            {
                                int replaceindex = filelist[j].LastIndexOf(DeletedSearchFieldList[i].getFldName());
                                filelist[j] = filelist[j].Substring(0, replaceindex) + filelist[j].Substring(replaceindex + DeletedSearchFieldList[i].getFldName().Length);
                            }
                            j++;
                        }
                    }

                    if (filelist[j].Contains("[Fields_TCS]") && (!filelist[j].StartsWith(";")))
                    {
                        while (filelist[j].Trim() != "")
                        {
                            if (filelist[j].ToLower().StartsWith(DeletedSearchFieldList[i].getFldName().ToLower() + "="))
                            {
                                filelist.RemoveAt(j);
                            }
                            j++;
                        }
                    }

                    if (filelist[j].Contains("[Fields]") && (!filelist[j].StartsWith(";")))
                    {
                        j++;
                        while (filelist[j].Trim() != "")
                        {
                                filelist.RemoveAt(j);
                        }
                    }


                    if (filelist[j].Equals("[field_" + DeletedSearchFieldList[i].getFldName() + "]", StringComparison.InvariantCultureIgnoreCase))
                    {
                        RemoveIndex = j;
                        while (filelist[j].Trim() != "")
                        {
                            j++;
                        }
                        RemoveCount = j - RemoveIndex;

                        //filelist.RemoveRange(RemoveIndex, RemoveCount);

                        filelist.RemoveRange(RemoveIndex, RemoveCount + 1);
                    }

                    if ((filelist[j].ToLower().Contains("ifval")||filelist[j].ToLower().Contains("ifnval")) && filelist[j].ToLower().Contains("\"\\fields." + DeletedSearchFieldList[i].getFldName().ToLower() + "\\\"") && (!filelist[j].StartsWith(";")))
                    {
                        RemoveIndex = j;
                        IFcounter = 1;
                        j++;
                        while ((filelist[j].Trim() != "") && (IFcounter > 0))
                        {
                            if ((!filelist[j].StartsWith(";")) && ((filelist[j].ToLower().Contains("ifval")) || (filelist[j].ToLower().Contains("ifnval")) || (filelist[j].ToLower().Contains("ifinc")) || (filelist[j].ToLower().Contains("ifinfield"))))
                            {
                                IFcounter++;
                            }

                            if ((!filelist[j].StartsWith(";")) && (filelist[j].ToLower().Contains("endiv")))
                            {
                                IFcounter = IFcounter - 1;
                            }
                            j++;
                        }


                        RemoveCount = j - RemoveIndex;

                        if (filelist[j].StartsWith(";"))
                        {
                            filelist.RemoveRange(RemoveIndex -  1, RemoveCount + 1);
                        }
                        else
                        {
                            filelist.RemoveRange(RemoveIndex, RemoveCount);
                        }

                    }
                }
            }

            for (int j = 0; j < filelist.Count; j++)
            {
                if (filelist[j].Contains("[Fields]") && (!filelist[j].StartsWith(";")))
                {
                    j++; 
                    while (filelist[j].Trim() != "")
                    {
                        filelist.RemoveAt(j);
                    }
                    break;
                }
            }

            int NonStdIndex = 1;
                for (int j = 0; j < filelist.Count; j++)
                {

                    if (filelist[j].Contains("[Fields]") && (!filelist[j].StartsWith(";")))
                    {
                        j++;
                        for (int i = 1; i <= SearchFieldList.Count; i++)
                        {
                            for (int p = 0; p < SearchFieldList.Count; p++)
                            {
                                if (SearchFieldList[p].getNonSTDPos() == NonStdIndex)
                                {
                                    filelist.Insert(j, SearchFieldList[p].getFldName() + "=" + SearchFieldList[p].getNonSTDFldMapping());
                                    NonStdIndex = i+1;
                                    j++;
                                    break;
                                }
                            }
                        }
                        
                    }
                }

            //for each search field which has been modified, update the script and search field
            for (int i = 0; i < SearchFieldList.Count; i++)
            {
                exists1 = false;
                exists2 = false;
                exists3 = false;
                exists4 = false;
                exists5 = false;
                int n = 0;

                if (SearchFieldList[i].ModifiedFlag() == true)
                {

                    for (int j = 0; j < filelist.Count; j++)
                    {

                        if (filelist[j].Contains("[Standard_Search]") && (!filelist[j].StartsWith(";")))
                        {
                            while (filelist[j].Trim() != "")
                            {
                                if ((filelist[j].ToLower().StartsWith(SearchFieldList[i].getSTDFldMapping().ToLower() + "=")) && (SearchFieldList[i].getSTDFldMapping() != ""))
                                {
                                    filelist.Insert(j, SearchFieldList[i].getSTDFldMapping() + "=" + SearchFieldList[i].getFldName());
                                    filelist.RemoveAt(j + 1);
                                    exists1 = true;
                                }
                                if ((filelist[j].ToLower().Contains("=" + SearchFieldList[i].getFldName().ToLower())) && (SearchFieldList[i].getSTDFldMapping() == ""))
                                {
                                    int replaceindex = filelist[j].LastIndexOf(SearchFieldList[i].getFldName());
                                    filelist[j] = filelist[j].Substring(0, replaceindex) + filelist[j].Substring(replaceindex + SearchFieldList[i].getFldName().Length);
                                }
                                j++;
                            }
                            if ((!exists1) && (SearchFieldList[i].getSTDFldMapping() != ""))
                            {
                                filelist.Insert(j, SearchFieldList[i].getSTDFldMapping() + "=" + SearchFieldList[i].getFldName());
                                exists1 = true;
                            }
                        }


                        if (filelist[j].Contains("[Fields_TCS]") && (!filelist[j].StartsWith(";")))
                        {
                            while (filelist[j].Trim() != "")
                            {
                                if (filelist[j].ToLower().StartsWith(SearchFieldList[i].getFldName().ToLower() + "="))
                                {
                                    if (SearchFieldList[i].getTCSFldMapping() == "")
                                    {
                                        filelist.RemoveAt(j);
                                        exists2 = true;
                                    }
                                    else
                                    {
                                        filelist.Insert(j, SearchFieldList[i].getFldName() + "=" + SearchFieldList[i].getTCSFldMapping());
                                        filelist.RemoveAt(j + 1);
                                        exists2 = true;
                                    }

                                }
                                j++;
                            }
                            if ((!exists2) && (SearchFieldList[i].getTCSFldMapping() != ""))
                            {
                                filelist.Insert(j, SearchFieldList[i].getFldName() + "=" + SearchFieldList[i].getTCSFldMapping());
                                exists2 = true;
                            }
                        }

                        if (filelist[j].Equals("[field_" + SearchFieldList[i].getFldName() + "]", StringComparison.InvariantCultureIgnoreCase))
                        {
                            RemoveIndex = j;
                            while (filelist[j].Trim() != "")
                            {
                                j++;
                            }
                            RemoveCount = j - RemoveIndex;

                            filelist.InsertRange(j, SearchFieldList[i].getFldDef());
                            filelist.RemoveRange(RemoveIndex +1, RemoveCount -1);
                            exists4 = true;
                        }
                        if ((filelist[j].StartsWith("[MLSRecordsEx]")) && (exists4 == false) && (SearchFieldList[i].getFldDef() != null))
                        {

                            int k = j;
                            while ((!filelist[k].StartsWith("[field_")) && (!filelist[k].StartsWith("[Field_")))
                            {
                                k--;
                            }
                            while (filelist[k].Trim() != "")
                            {
                                k++;
                            }

                            filelist.Insert(k, "");
                            filelist.Insert(k + 1, "[field_" + SearchFieldList[i].getFldName() + "]");
                            filelist.InsertRange(k + 2, SearchFieldList[i].getFldDef());
                            exists4 = true;
                        }


                        if ((filelist[j].ToLower().Contains("ifval") || filelist[j].ToLower().Contains("ifnval")) && filelist[j].ToLower().Contains("\"\\fields." + SearchFieldList[i].getFldName().ToLower() + "\\\"") && (!filelist[j].StartsWith(";")))
                            {
                                RemoveIndex = j;
                                IFcounter = 1;
                                j++;
                                while ((filelist[j].Trim() != "") && (IFcounter > 0))
                                {
                                    if ((!filelist[j].StartsWith(";")) && ((filelist[j].ToLower().Contains("ifval")) || (filelist[j].ToLower().Contains("ifnval")) || (filelist[j].ToLower().Contains("ifinc")) || (filelist[j].ToLower().Contains("ifinfield"))))
                                    {
                                        IFcounter++;
                                    }

                                    if ((!filelist[j].StartsWith(";")) && (filelist[j].ToLower().Contains("endiv")))
                                    {
                                        IFcounter = IFcounter - 1;
                                    }
                                    j++;
                                }

                                RemoveCount = j - RemoveIndex;

                                List<List<string>> FldScript = SearchFieldList[i].getFldScript();
                                filelist.InsertRange(j, FldScript[n]);
                                filelist.RemoveRange(RemoveIndex, RemoveCount);
                                n++;
                                exists5 = true;
                            }
                            if ((filelist[j].StartsWith("[MLSRecordsEx]")) && (exists5 == false))
                            {

                                int k = j;
                                while (!filelist[k].StartsWith("[MainScript]"))
                                {
                                    k--;
                                }
                                while (((!filelist[k].ToLower().StartsWith("ifval \"\\fields.")) || (!filelist[k + 1].ToLower().Contains("ifval \"\\first\\\""))) && ((!filelist[k].ToLower().StartsWith("ifval \"\\fields.")) || (!filelist[k + 1].ToLower().Contains("transmit \","))))
                                {
                                    k++;
                                }

                                if (filelist[k - 1].StartsWith(";"))
                                {
                                    k--;
                                }

                                List<List<string>> FldScript = SearchFieldList[i].getFldScript();
                                filelist.Insert(k, ";--  --  --  -- " + SearchFieldList[i].getFldName() + " --  --  --  --");
                                filelist.InsertRange(k + 1, FldScript[n]);
                                n++;
                                exists5 = true;
                            }
                    }

                }
            }

            //SearchFieldList.Clear();
            DeletedSearchFieldList.Clear();

            //StreamWriter writer = new StreamWriter(FileLocation);

            using (
            var writer = new StreamWriter(new FileStream(FileLocation, FileMode.Open, FileAccess.ReadWrite), Encoding.UTF8))


            for (int k = 0; k < filelist.Count ; k++)
            {
                writer.WriteLine(filelist[k]);
            }

            //writer.Close();

        }

        public void SetKeyValue(string key, string value, string section)
        {
            IniFile iniFile = new IniFile(FileLocation);
            iniFile.Write(key,value,section);
        }
        public string GetKeyValue(string key,string section)
        {
            var iniFile = new IniFile(FileLocation);
            return iniFile.Read(key, section);
        }

        public bool IsSearchType2()
        {
            var searchType = GetKeyValue("SearchType", "TcpIp");
            var isSearchType2 = false;
            if (!string.IsNullOrEmpty(searchType))
            {
                isSearchType2 = searchType.Equals("2");
            }
            return isSearchType2;
        }

        public string getMLSRecordsEX(string MetaSysName)
        {
            string mapping = "";
            int index = 0; 
            foreach (string instance in MLSRecordsExList[0])
            {
                if (MLSRecordsExList[0][index] == MetaSysName)
                {
                    mapping = MLSRecordsExList[1][index];
                    break;
                }
                index++;
            }
            return mapping;
        }

        public string GetStandardClassesName()
        {
            string result = "";
            string filePostFix = Path.GetFileName(FileLocation).Substring(6, 2);
            switch (filePostFix)
            {
                case "re":
                    result = "SFR";
                    break;
                case "mf":
                    result = "MFM";
                    break;
                case "la":
                    result = "LND";
                    break;
                case "fa":
                    result = "FRM";
                    break;
                case "mb":
                    result = "MOB";
                    break;
                case "ri":
                    result = "MFM";
                    break;
                case "cm":
                    result = "COM";
                    break;
                case "co":
                    result = "CON";
                    break;
                case "cl":
                    result = "COM";
                    break;
                case "rn":
                    result = "RNT";
                    break;
                case "rl":
                    result = "RNT";
                    break;

            }
            return result;
            //if (!string.IsNullOrEmpty(_standarClassName))
            //    return _standarClassName;
            //List<string> classes = new List<string>();
            ////string ts = GetDEFSection("LTSPTypeResults", 0); //
            //string ts = GetSection("LTSPType"); //
            //string[] tss = ts.Split('\r');
            //for (int i = 0; i < tss.Length; i++)
            //{
            //    int k = tss[i].LastIndexOf(":");
            //    if (k > -1)
            //    {
            //        string s = tss[i].Substring(k + 1).Trim(new char[] { '\r', '\n', ' ', '\t' });
            //        string[] ss = s.Split(',');
            //        for (int j = 0; j < ss.Length; j++)
            //        {
            //            string s0 = ss[j].Trim(new char[] { ' ', '\t', '\r', '\n' });
            //            if (s0.Length == 3 && !classes.Contains(s0))
            //                classes.Add(s0);
            //        }
            //    }
            //}
            //if (classes.Count == 0)
            //{
            //    ts = GetSection("CMAfield_STDFSaleOrLease");
            //    if (ts.ToLower().IndexOf("default=lease") > -1)
            //        classes.Add("RNT");
            //}
            //string result = "";
            //for (int i = 0; i < classes.Count; i++)
            //{
            //    result += classes[i] + ",";
            //}
            //_standarClassName = result.TrimEnd(',');
            //return _standarClassName;
        }

        public string GetSystemName(string fieldName)
        {
            string result = "";
            var firstOrDefault = _mlsResultFieldCollection.FirstOrDefault(x => x.DefName.ToLower() == fieldName.ToLower());
            if (firstOrDefault != null)
            {
                result =
                    firstOrDefault.SystemName;
            }
            return result;
        }

        public string GetSection(string sectionName)
        {
            IniFile ifile = new IniFile(FileLocation);
            string result = ifile.GetSectionContent(sectionName);
            if (string.IsNullOrEmpty(result))
                result = "";
            return result;
        }

        public void SaveDefFile()
        {
            string content = "";
            foreach (var item in SectionList)
            {
                string sectionContent = SectionContentDict[item];
                if (!string.IsNullOrEmpty(sectionContent))
                {
                    switch (item)
                    {
                        case "Comments":
                        case "SearchFieldList":
                            break;
                        case "ResultFieldGroupList":
                            break;
                        case "CmaFieldList":
                            break;
                        case "LTSPType":
                        case "LTSPTypeResults":
                            sectionContent = "[" + item + "]";
                            break;
                        //case "MLSRecordsEx":
                        //    break;
                        default:
                            sectionContent = "[" + item + "]\r\n" + sectionContent;
                            break;
                    }
                    content = content + sectionContent.Trim() + "\r\n\r\n";
                }
            }
            
            File.WriteAllText(FileLocation, content, Encoding.GetEncoding("iso-8859-1"));
        }
    }
}
