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
using System.Configuration;

namespace SpeedUp
{
    /// <summary>
    /// Interaction logic for WorkNow.xaml
    /// </summary>
    public partial class WorkNow : Window
    {
        private const string RdctcsDataSourceNameMappingSharePointFilePath = @"http://topix/sites/TopConnector/_layouts/xlviewer.aspx?id=/sites/TopConnector/DataAgg/ORCA-Data-Sources-Active-and-Roster-ALL.XLSX";

        private string RdctcsDataSourceNameMappingLocalFilePath = string.Format(@"{0}\SpeedUp\RDCTCSDataSourceNameMapping.xlsx",ConfigurationManager.AppSettings["Drive"]);
        public string ModuleId { get; set; }

        private readonly MainWindow _mParent;
        public ObservableCollection<DefFileData> DefFileDataCollection
        { get { return _defFileDataCollection; } }

        readonly ObservableCollection<DefFileData> _defFileDataCollection =
        new ObservableCollection<DefFileData>();
        private string WorkingDirectory = @"";
        public static string TfsLocation = @"$/TCDEF/TCDEF_a.1_Main/";
        private string _boardId = "";
        private int _version = 0;
        
        public CookieContainer ReleaseDefCookieContainer { get; set; }

        public WorkNow(MainWindow parent, string input)
        {
            try
            {
                WorkingDirectory = Properties.Settings.Default.TFSLocalFolder;

                if (!WorkingDirectory.EndsWith("\\"))
                    WorkingDirectory += "\\";

                Util.DownloadSharepointDocument(RdctcsDataSourceNameMappingSharePointFilePath,
                                                RdctcsDataSourceNameMappingLocalFilePath);
                _mParent = parent;
                int intModuleId;
                InitializeComponent();
                if (int.TryParse(input, out intModuleId))
                {
                    ModuleId = input;
                    ModuleIDTextBox.Text = input;
                    try
                    {
                        RDCCodeTextBox.Text = Util.GetColumnValueByRowId("Module ID", input, "RDC Code",
                                                                         RdctcsDataSourceNameMappingLocalFilePath);
                        TraceNameTextBox.Text = Util.GetColumnValueByRowId("Module ID", input, "TRACE Name",
                                                                           RdctcsDataSourceNameMappingLocalFilePath);
                        ModuleNameTextBox.Text = Util.GetColumnValueByRowId("Module ID", input, "Module Name",
                                                                            RdctcsDataSourceNameMappingLocalFilePath);
                    }
                    catch (Exception)
                    {
                    }
                }
                else if (input.Length == 4)
                {
                    input = input.ToUpper();
                    RDCCodeTextBox.Text = input;
                    try
                    {
                        ModuleIDTextBox.Text = Util.GetColumnValueByRowId("RDC Code", input, "Module ID",
                                                                          RdctcsDataSourceNameMappingLocalFilePath);
                        ModuleId = ModuleIDTextBox.Text;
                        TraceNameTextBox.Text = Util.GetColumnValueByRowId("RDC Code", input, "TRACE Name",
                                                                           RdctcsDataSourceNameMappingLocalFilePath);
                        ModuleNameTextBox.Text = Util.GetColumnValueByRowId("RDC Code", input, "Module Name",
                                                                            RdctcsDataSourceNameMappingLocalFilePath);
                    }
                    catch (Exception)
                    {
                    }
                }
                else if (input.Length > 4 && input.IndexOf(" ", System.StringComparison.Ordinal) == -1)
                {
                    TraceNameTextBox.Text = input;
                    try
                    {
                        ModuleIDTextBox.Text = Util.GetColumnValueByRowId("TRACE Name", input, "Module ID",
                                                                          RdctcsDataSourceNameMappingLocalFilePath);
                        ModuleId = ModuleIDTextBox.Text;
                        RDCCodeTextBox.Text = Util.GetColumnValueByRowId("TRACE Name", input, "RDC Code",
                                                                         RdctcsDataSourceNameMappingLocalFilePath);
                        ModuleNameTextBox.Text = Util.GetColumnValueByRowId("TRACE Name", input, "Module Name",
                                                                            RdctcsDataSourceNameMappingLocalFilePath);
                    }
                    catch (Exception)
                    {
                    }
                }
                else
                {
                    MessageBox.Show("ModuleID can't be found in the Excel file",
                                    "ModuleID can't be found in the Excel file", MessageBoxButton.OK,
                                    MessageBoxImage.Exclamation);
                }

                if (ModuleId == null)
                {
                    throw new Exception("Invalid Module ID");
                }
                try
                {

                    using (TCSEntitiesProduction tcsDB = new TCSEntitiesProduction())
                    {
                        tcsDB.Connection.Open();
                        ObjectResult<tcs_GetBoardID_Result> boardIDResult =
                            tcsDB.tcs_GetBoardID(Convert.ToInt16(ModuleId));
                        foreach (var boardInfo in boardIDResult)
                        {
                            _boardId = boardInfo.board_id.ToString();
                        }
                        var moduleId = int.Parse(ModuleId);
                        ModuleNameTextBox.Text = tcsDB.cma_mls_modules.Where(x => x.module_id == moduleId).Single().module_name;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to access Qa1 db\r\n" + ex.Message + ex.Source + ex.StackTrace);
                }

                
                WorkingFolderTextBox.Text = WorkingDirectory + ModuleId;
                TFSLocationTextBox.Text = TfsLocation + ModuleId;
                this.Title = "Working on " + ModuleId;

                LoadDefFileListView();

                BoardIDTextBox.Text = _boardId;
                try
                {
                    bool loginInfoExists = false;
                    if (!string.IsNullOrEmpty(RDCCodeTextBox.Text))
                    {
                        using (SystemEntities se = new SystemEntities())
                        {
                            se.Connection.Open();
                            ObjectResult<spDA_RETSConnectionInfo_sel_Result> loginTrace =
                                se.spDA_RETSConnectionInfo_sel(RDCCodeTextBox.Text, false);
                            foreach (var connInfo in loginTrace)
                            {
                                LoginURLTextBox.Text = connInfo.RETSLoginURL;
                                LoginNameTextBox.Text = connInfo.RETSUserName;
                                PasswordTextBox.Text = connInfo.RETSPassword;
                                UserAgentTextBox.Text = connInfo.RETSUserAgent;
                                UAPasswordTextBox.Text = connInfo.RETSUserAgentPassword;
                                if(!string.IsNullOrEmpty(LoginURLTextBox.Text))
                                    loginInfoExists = true;
                            }
                        }
                    }
                    if (!loginInfoExists)
                    {
                        using (var tcsDb = new TCSEntities())
                        {
                            short moduleId = Convert.ToInt16(ModuleIDTextBox.Text);
                            if (tcsDb.tcs_rets_connection_info.Any(x => x.module_id == moduleId))
                            {
                                var qe = tcsDb.tcs_rets_connection_info.Where(x => x.module_id == moduleId).FirstOrDefault();
                                LoginURLTextBox.Text = qe.login_url;
                                LoginNameTextBox.Text = qe.user_name;
                                PasswordTextBox.Text = qe.password;
                                UserAgentTextBox.Text = qe.user_agent;
                                UAPasswordTextBox.Text = qe.ua_password;
                            }
                        }
                    }
                }catch (Exception ex)
                {
                    MessageBox.Show("Failed to access DataAgg db\r\n" + ex.Message + ex.Source + ex.StackTrace);
                }
                CommentTextBox.Text = "; " + DateTime.Now.ToString("MMMM dd, yyyy") + string.Format(" [{0}] as per project #", char.ToUpper(Environment.UserName[0]).ToString() + char.ToUpper(Environment.UserName[1]).ToString() + Environment.UserName.Substring(2));
                CheckInCommentsTextBox.Text = _version.ToString() + ".1-R";
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        private void LoadDefFileListView()
        {

            _defFileDataCollection.Clear();
            var TFSFileDataDict = new Dictionary<string, TfsFileData>();
            try
            {
                var tfs = new TFSOperations("vstfsrv01", WorkingFolderTextBox.Text);
                TFSFileDataDict=tfs.GetTFSProjectInfo(WorkingFolderTextBox.Text, TFSOperations.PathType.Directory);

            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
            using (var tcsDB = new TCSEntities())
            {
                tcsDB.Connection.Open();
                ObjectResult<cma_GetMLSConnection_Result> connectoinResult = tcsDB.cma_GetMLSConnection(Convert.ToInt16(_boardId), "", "");
                foreach (var item in connectoinResult)
                {
                    cma_GetMLSConnection_Result item1 = item;
                    var proclass =
                        tcsDB.tcs_module_class_settings.Where(
                            cl => item1 != null && cl.connection_name == item1.connection_name)
                             .Select(cl => new {cl.@class});
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
                    
                    _boardId = item.board_id.ToString();
                }
            }

            try
            {
                var di = new DirectoryInfo(WorkingDirectory + ModuleId);
                
                foreach (var fi in di.GetFiles())
                {
                    var defFileData = _defFileDataCollection.Where(c => c.FileName.ToLower() == fi.Name.ToLower());

                    var def = new IniFile(fi.FullName);

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
                        _version = int.Parse(def.Read("Version", "Common"));
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
                            _version = int.Parse(def.Read("Version", "Common"));
                        }
                    }
                }
            }
            catch (Exception)
            { }

            try
            {
                foreach (var data in _defFileDataCollection)
                {
                    if (TFSFileDataDict.ContainsKey(data.FileName.ToLower()))
                    {
                        data.CheckoutBy = TFSFileDataDict[data.FileName.ToLower()].CheckoutBy;
                        if (!string.IsNullOrEmpty(data.CheckoutBy))
                            data.CheckoutBy = data.CheckoutBy.Substring(data.CheckoutBy.IndexOf("\\") + 1);
                        data.IsLatest = TFSFileDataDict[data.FileName.ToLower()].IsLatest;
                    }
                    else
                        data.IsLatest = "New";
                }
            }
            catch (Exception)
            { }
            //GetTFSFileList();
            CheckInCommentsTextBox.Text = _version.ToString() + ".1-R";
            listView1.SelectedIndex = 0;
        }
        
        private void PerformanceLogProduction_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("http://tcs.topproducer.com/auditlog/89F350726EB2B7F18/performance.aspx?module_id=" + ModuleId);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void PerformanceLogQa1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("http://tcsqa.topproducer.com/auditlog/89F350726EB2B7F18/performance.aspx?module_id=" + ModuleId);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void OpenTFS_Click(object sender, RoutedEventArgs e)
        { 
            try
            {
                System.Diagnostics.Process.Start(@"C:\Program Files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe"," /Command View.TfsSourceControlExplorer");
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _mParent.Show();
        }

        

        private void GetMetadataButton_Click(object sender, RoutedEventArgs e)
        {
            UiServices.SetBusyState();
            
            if (listView1.SelectedItems.Count == 0)
                {
                    MessageBoxResult messageResult = MessageBox.Show("Please select one def file", "Select one Def file", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            string defPath = System.IO.Path.Combine(WorkingFolderTextBox.Text, ((DefFileData)listView1.SelectedItem).FileName);
            try
            {
                Util.DownloadMlsMetadata(WorkingDirectory, ModuleIDTextBox.Text, defPath, LoginNameTextBox.Text,
                                         PasswordTextBox.Text, UserAgentTextBox.Text, UAPasswordTextBox.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void OpenMetaDataLogButton_Click(object sender, RoutedEventArgs e)
        {
            try{
                UiServices.SetBusyState();
                string fileName = System.IO.Path.Combine(System.IO.Path.Combine(WorkingDirectory, ModuleId), "metadata.log");
                if (File.Exists(fileName))
                {
                    var process = new System.Diagnostics.Process
                        {
                            StartInfo = {UseShellExecute = true, FileName = "notepad.exe", Arguments = fileName}
                        };
                    process.Start();
                }
                else
                {
                    MessageBox.Show("File does not exist.");
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void WorkingFolderTextBox_PreviewMouseDown(object sender, EventArgs e)
        {
            try{
                Clipboard.SetDataObject(WorkingFolderTextBox.Text);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try{
                var process = new System.Diagnostics.Process
                    {
                        StartInfo = {UseShellExecute = true, FileName = WorkingFolderTextBox.Text}
                    };
                process.Start();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void UploadDefFileQa1Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UiServices.SetBusyState();
                var mls = new TCSWebService.MlsSoapClient();
                string msg = "";
                for (var i = 0; i < listView1.SelectedItems.Count; i++)
                {
                    var item = (DefFileData)listView1.SelectedItems[i];
                    string upfileName = System.IO.Path.Combine(WorkingFolderTextBox.Text, item.FileName);

                    //                        string boardId = rows[0]["Board ID"].ToString();
                    string connectionName = item.FileName;
                    string uploadFileName = connectionName;

                    byte[] bytFile = File.ReadAllBytes(upfileName);

                    //CAPICOM.Utilities cau = new CAPICOM.Utilities();
                    //string uploadData=cau.Base64Encode(cau.ByteArrayToBinaryString(bytFile));
                    string uploadData = Convert.ToBase64String(bytFile);

                    XmlDocument xDoc = new XmlDocument();
                    xDoc.LoadXml("<DEFMLSRequest></DEFMLSRequest>");
                    if (xDoc.DocumentElement != null)
                    {
                        xDoc.DocumentElement.SetAttribute("upload_flag", "1");
                        xDoc.DocumentElement.SetAttribute("board_id", BoardIDTextBox.Text);
                        xDoc.DocumentElement.SetAttribute("connection_name", connectionName);
                        xDoc.DocumentElement.SetAttribute("upload_filename", uploadFileName);
                        xDoc.DocumentElement.SetAttribute("upload_data", uploadData);

                        if (i == (listView1.SelectedItems.Count - 1))
                        {
                            xDoc.DocumentElement.SetAttribute("metadata_request", "1");
                            xDoc.DocumentElement.SetAttribute("categorization_request", "0");
                        }
                        else
                        {
                            xDoc.DocumentElement.SetAttribute("metadata_request", "0");
                            xDoc.DocumentElement.SetAttribute("categorization_request", "0");
                        }
                    }

                    string sOutput = "";

                    try
                    {
                        string sInput = xDoc.OuterXml;

                        sOutput = mls.GetDefList(sInput);

                        xDoc.LoadXml(sOutput);
                        if (xDoc.DocumentElement != null) msg += xDoc.DocumentElement.GetAttribute("result") + "\r\n";
                    }
                    catch (Exception ex)
                    {
                        msg += upfileName + ": (Error) " + ex.Message + "[" + sOutput + "]\r\n";
                    }
                }

                MessageBox.Show(msg);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void GenerateDataAggRequestButton_Click(object sender, RoutedEventArgs e)
        {
            try{
                UiServices.SetBusyState();
                string request200 = @"<TCService ClientName=""ORCA""><Function>GetDataAggListings</Function><Login><password>{0}</password><password>{1}</password><UserAgent>{2}</UserAgent><RetsUAPwd>{3}</RetsUAPwd></Login><Board BoardId=""{4}"" MetadataVersion=""0""/><Search BypassARAuthentication=""1"" Count=""0"" RecordsLimit=""200"" ST_MLSNo="""" IncludeCDATA=""1"" SelectPicFieldsOnly=""0"" ReturnDataAggListingsXML=""1"" ST_PublicListingStatus=""A"" ST_LastMod=""{5}""/></TCService>";
                //request200 = string.Format(request200, LoginNameTextBox.Text, PasswordTextBox.Text, UserAgentTextBox.Text, UAPasswordTextBox.Text, ModuleIDTextBox.Text);
                string timeRange = "";
                string selectedFileName = ((DefFileData)listView1.SelectedItem).FileName;
                if (selectedFileName.IndexOf("ag.sql", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
                {
                    request200 = @"<TCService ClientName=""ORCA""><Function>GetAgentRoster</Function><Login><password>{0}</password><password>{1}</password><UserAgent>{2}</UserAgent><RetsUAPwd>{3}</RetsUAPwd></Login><Board ModuleId=""{4}"" MetadataVersion=""0""/><Search BypassARAuthentication=""1"" Count=""0"" RecordsLimit=""200"" ReturnDataAggListingsXML=""0"" /></TCService>";
                }
                else if (selectedFileName.IndexOf("of.sql", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
                {
                    request200 = @"<TCService ClientName=""ORCA""><Function>GetOfficeRoster</Function><Login><password>{0}</password><password>{1}</password><UserAgent>{2}</UserAgent><RetsUAPwd>{3}</RetsUAPwd></Login><Board ModuleId=""{4}"" MetadataVersion=""0""/><Search BypassARAuthentication=""1"" Count=""0"" RecordsLimit=""200"" ReturnDataAggListingsXML=""0"" /></TCService>";
                }
                else if (selectedFileName.IndexOf("oh.sql", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
                {
                    //request200 = @"<TCService ClientName=""ORCA""><Function>GetOpenHouse</Function><Login><password>{0}</password><password>{1}</password><UserAgent>{2}</UserAgent><RetsUAPwd>{3}</RetsUAPwd></Login><Board ModuleID=""{4}"" DefPath=""{4}"" MetadataVersion=""0""/><Search BypassARAuthentication=""1"" Count=""0"" DACount=""0"" RecordsLimit=""200"" ST_SearchDate=""{5}"" ReturnDataAggListingsXML=""1"" /></TCService>";
                    request200 = @"<TCService ClientName=""ORCA""><Function>GetOpenHouse</Function><Login><password>{0}</password><password>{1}</password><UserAgent>{2}</UserAgent><RetsUAPwd>{3}</RetsUAPwd></Login><Board ModuleId=""{4}"" MetadataVersion=""0""/><Search BypassARAuthentication=""1"" Count=""0"" RecordsLimit=""200"" ST_SearchDate=""{5}"" ReturnDataAggListingsXML=""0"" /></TCService>";
                    timeRange = DateTime.Now.ToString("MM/dd/yyyyTHH:mm:ss") + "-" + DateTime.Now.AddYears(1).ToString("MM/dd/yyyyTHH:mm:ss");
                }
                else
                {
                    request200 = @"<TCService ClientName=""ORCA""><Function>GetDataAggListings</Function><Login><password>{0}</password><password>{1}</password><UserAgent>{2}</UserAgent><RetsUAPwd>{3}</RetsUAPwd></Login><Board ModuleId=""{4}"" MetadataVersion=""0""/><Search BypassARAuthentication=""1"" Count=""0"" RecordsLimit=""200"" ST_MLSNo="""" IncludeCDATA=""1"" SelectPicFieldsOnly=""0"" ReturnDataAggListingsXML=""1"" ST_PublicListingStatus=""A"" ST_LastMod=""{5}""/></TCService>";
                    timeRange = DateTime.Now.AddDays(-20).ToString("MM/dd/yyyyTHH:mm:ss") + "-" + DateTime.Now.ToString("MM/dd/yyyyTHH:mm:ss");
                }

                request200 = string.Format(request200, Util.ConvertStringToXml(LoginNameTextBox.Text), Util.ConvertStringToXml(PasswordTextBox.Text), Util.ConvertStringToXml(UserAgentTextBox.Text), Util.ConvertStringToXml(UAPasswordTextBox.Text), ModuleIDTextBox.Text, timeRange);

                Clipboard.SetDataObject(request200);
                AutoClosingMessageBox.Show("Copied to Clipboard", "Generate Request", 2000);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }
        private void GenerateDataAggRequestButton_LostFocus(object sender, RoutedEventArgs e)
        {
            this.Title = "Working on " + ModuleId;
        }

        private void ModuleIDTextBox_PreviewMouseDown(object sender, RoutedEventArgs e)
        {
            try{
                if (!string.IsNullOrEmpty(ModuleIDTextBox.Text))
                    Clipboard.SetDataObject(ModuleIDTextBox.Text);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void RDCCodeTextBox_PreviewMouseDown(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(RDCCodeTextBox.Text))
                    Clipboard.SetDataObject(RDCCodeTextBox.Text);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void BoardIDTextBox_PreviewMouseDown(object sender, RoutedEventArgs e)
        {
            try{
                if (!string.IsNullOrEmpty(BoardIDTextBox.Text))
                    Clipboard.SetDataObject(BoardIDTextBox.Text);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void LoginNameTextBox_PreviewMouseDown(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(LoginNameTextBox.Text))
                    Clipboard.SetDataObject(LoginNameTextBox.Text);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void PasswordTextBox_PreviewMouseDown(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(PasswordTextBox.Text))
                    Clipboard.SetDataObject(PasswordTextBox.Text);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void LoginURLTextBox_PreviewMouseDown(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(LoginURLTextBox.Text))
                    Clipboard.SetDataObject(LoginURLTextBox.Text);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void UserAgentTextBox_PreviewMouseDown(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(UserAgentTextBox.Text))
                    Clipboard.SetDataObject(UserAgentTextBox.Text);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void UAPasswordTextBox_PreviewMouseDown(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(UAPasswordTextBox.Text))
                    Clipboard.SetDataObject(UAPasswordTextBox.Text);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }

        }

        private void ModuleNameTextBox_PreviewMouseDown(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(ModuleNameTextBox.Text))
                    Clipboard.SetDataObject(ModuleNameTextBox.Text);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void TraceNameTextBox_PreviewMouseDown(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(TraceNameTextBox.Text))
                    Clipboard.SetDataObject(TraceNameTextBox.Text);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void TFSLocationTextBox_PreviewMouseDown(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(TFSLocationTextBox.Text))
                    Clipboard.SetDataObject(TFSLocationTextBox.Text);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void GetSampleDataButton_Click(object sender, RoutedEventArgs e)
        {
            UiServices.SetBusyState();
            if (listView1.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select one def file", "Select one Def file", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            DownloadSampleDataFiles(listView1.SelectedItems);
        }

        private void GetAllSampleDataButton_Click(object sender, RoutedEventArgs e)
        {
            UiServices.SetBusyState();
            DownloadSampleDataFiles(listView1.Items);
        }

        private void DownloadSampleDataFiles(IEnumerable items)
        {
            var messageResult = "";
            foreach (var item in items)
            {
                var selectedFileName = ((DefFileData)item).FileName;
                try
                {
                    var filePath = WorkingDirectory + ModuleId + "\\" +
                               selectedFileName.Substring(0, selectedFileName.Length - 4);
                    GetSampleDataForSingleDef(selectedFileName,
                        filePath + "_grid.dat",
                        filePath + "_comm.log",
                        filePath + "_tcs.xml");
                }
                catch (Exception ex)
                {
                    messageResult += "Failed to download sample data for " + selectedFileName + "\r\n";
                }
            }
            if (!string.IsNullOrEmpty(messageResult))
                MessageBox.Show(messageResult);
        }

        private LoginInfo GetLoginInfo()
        {
            return new LoginInfo
            {
                ByPassAuthentication = "1",
                UserName = LoginNameTextBox.Text,
                Password = PasswordTextBox.Text,
                UserAgent = UserAgentTextBox.Text,
                UaPassword = UAPasswordTextBox.Text
            };
        }

        private void GetSampleDataForSingleDef(string selectedFileName, string sampleDataFilePath, string logPath, string resultFilePath)
        {
            var searchEngine = new SearchEngine();
            searchEngine.IsDebug = true;
            searchEngine.BoardID = 999999;
                
            var defPath = GetDefFilePath(selectedFileName);
            var resultFolder = GetTempResultFolder();
            var defPathInTempResultFolder = System.IO.Path.Combine(resultFolder,selectedFileName);
            File.Copy(defPath, defPathInTempResultFolder, true);

            var searchRequestXml = RequestXmlHelper.GetRequestXmlForSampleData(defPathInTempResultFolder, GetLoginInfo(),
                MlsNumberTextBox.Text.Trim(), IncludeRETSBlobCheckBox.IsChecked.HasValue && IncludeRETSBlobCheckBox.IsChecked.Value);
            var result = searchEngine.RunClientRequest(searchRequestXml);

            var fileList = new DirectoryInfo(resultFolder).GetFiles("@@grid@@.dat", SearchOption.AllDirectories);
                
            fileList.First().CopyTo(sampleDataFilePath, true);
               
            var commnicationFile = new DirectoryInfo(resultFolder).GetFiles("*.log", SearchOption.AllDirectories);
            {
                var fileName = commnicationFile.First().FullName;
                File.Copy(fileName, logPath, true);
            }
            var defName = ((DefFileData)listView1.SelectedItem).FileName;
            if (!string.IsNullOrEmpty(resultFilePath))
                File.Copy(resultFolder + "\\tcslisting.xml", resultFilePath, true);
        }

        private string GetTempResultFolder()
        {
            var datetimeFolderName = DateTime.Now.ToString("yyyyMMddhhmmss");
            string runningDirectory = string.Format(@"{0}\speedup",ConfigurationManager.AppSettings["Drive"]); //System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string resultFolder = System.IO.Path.Combine(System.IO.Path.Combine(runningDirectory, "Result"), datetimeFolderName);
            Directory.CreateDirectory(resultFolder);
            return resultFolder;
        }

        private string GetDefFilePath(string fileName)
        {
            return WorkingDirectory + ModuleId + "\\" + fileName;
        }

        private void OpenSampleDataLogButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UiServices.SetBusyState();
                string fileName = System.IO.Path.Combine(WorkingDirectory, ModuleId + "\\" + ((DefFileData)listView1.SelectedItem).FileName.Substring(0, ((DefFileData)listView1.SelectedItem).FileName.Length - 4) + "_comm.log");
                if (File.Exists(fileName))
                {
                    var process = new System.Diagnostics.Process
                        {
                            StartInfo = {UseShellExecute = true, FileName = "notepad.exe", Arguments = fileName}
                        };
                    process.Start();
                }
                else
                {
                    MessageBox.Show("File does not exist.");
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }
        private void OpenTCSResultButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UiServices.SetBusyState();
                var fileName = ((DefFileData)listView1.SelectedItem).FileName.Substring(0, ((DefFileData)listView1.SelectedItem).FileName.Length - 4) + "_tcs.xml";
                fileName = WorkingFolderTextBox.Text + "\\" + fileName;
                if (File.Exists(fileName))
                {
                    var process = new System.Diagnostics.Process
                        {
                            StartInfo =
                                {
                                    UseShellExecute = true,
                                    FileName = "iexplore.exe",
                                    WorkingDirectory = WorkingFolderTextBox.Text,
                                    Arguments = "File:\\\\" + fileName
                                }
                        };
                    process.Start();
                }
                else
                {
                    MessageBox.Show("File does not exist.");
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void OpenMetaDataButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UiServices.SetBusyState();
                var process = new System.Diagnostics.Process
                    {
                        StartInfo =
                            {
                                UseShellExecute = true,
                                FileName = "iexplore.exe",
                                WorkingDirectory = WorkingFolderTextBox.Text,
                                Arguments = "File:\\\\" + WorkingFolderTextBox.Text + "\\metadata.xml"
                            }
                    };
                process.Start();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void OpenSampleData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UiServices.SetBusyState();
                string fileName = ((DefFileData)listView1.SelectedItem).FileName.Substring(0, ((DefFileData)listView1.SelectedItem).FileName.Length - 4) + "_grid.dat";
                if (File.Exists(WorkingFolderTextBox.Text + "\\" + fileName))
                {
                    var fileContent = File.ReadAllText(WorkingFolderTextBox.Text + "\\" + fileName);
                    fileContent = fileContent.Substring(fileContent.IndexOf("<COLUMNS>", 0, StringComparison.CurrentCultureIgnoreCase));
                    var excelFormatFileName = WorkingFolderTextBox.Text + "\\" + fileName.Replace(".dat", "_excel.dat");
                    if (File.Exists(excelFormatFileName))
                    {
                        try
                        {
                            File.Delete(excelFormatFileName);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, "Can't open sample data file", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    File.WriteAllText(excelFormatFileName, fileContent);
                    var process = new System.Diagnostics.Process
                        {
                            StartInfo =
                                {
                                    UseShellExecute = true,
                                    FileName = "excel.exe",
                                    WorkingDirectory = WorkingFolderTextBox.Text,
                                    Arguments = fileName.Replace(".dat", "_excel.dat")
                                }
                        };
                    process.Start();
                }
                else
                {
                    MessageBox.Show("File does not exist.");
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        public static string ReplaceString(string str, string oldValue, string newValue, StringComparison comparison)
        {
            var sb = new StringBuilder();

            int previousIndex = 0;
            int index = str.IndexOf(oldValue, comparison);
            while (index != -1)
            {
                sb.Append(str.Substring(previousIndex, index - previousIndex));
                sb.Append(newValue);
                index += oldValue.Length;

                previousIndex = index;
                index = str.IndexOf(oldValue, index, comparison);
            }
            sb.Append(str.Substring(previousIndex));

            return sb.ToString();
        }

        private void ReplyEmailButton_Click(object sender, RoutedEventArgs e)
        {
            string message = string.Format(Properties.Settings.Default.ReplyEmailTemplate,_version.ToString());
            Clipboard.SetDataObject(message);
            AutoClosingMessageBox.Show("Copied to Clipboard", "Reply Email", 2000);
            
        }

        private void AddCommentButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UiServices.SetBusyState();
                foreach (DefFileData item in listView1.SelectedItems)
                {
                    string content = File.ReadAllText(System.IO.Path.Combine(WorkingFolderTextBox.Text, item.FileName), Encoding.GetEncoding("iso-8859-1"));
                    int pos = content.IndexOf("\r\n\r\n[Common]", System.StringComparison.Ordinal);
                    if (pos > -1)
                    {
                        content = content.Replace("\r\n\r\n[Common]", "\r\n" + CommentTextBox.Text + "\r\n\r\n[Common]");
                        File.WriteAllText(System.IO.Path.Combine(WorkingFolderTextBox.Text, item.FileName), content, Encoding.GetEncoding("iso-8859-1"));
                    }
                    else
                    {
                        MessageBox.Show("Failed add comment to file " + item.FileName, "Failed add comment", MessageBoxButton.OK
                           , MessageBoxImage.Error);
                    }

                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void IncreaseVersionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UiServices.SetBusyState();
                foreach (DefFileData item in listView1.Items)
                {
                    var def = new IniFile(System.IO.Path.Combine(WorkingFolderTextBox.Text, item.FileName));
                    string version = def.Read("Version", "Common");
                    def.Write("Version", (int.Parse(version) + 1).ToString(), "Common");
                    def.Write("LastModified", DateTime.Now.ToString("MMM d, yyyy"), "Common");
                }
                LoadDefFileListView();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void DownLoadFileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UiServices.SetBusyState();
                string result = "";
                string filePath = DownloadFileTextBox.Text;
                try
                {
                    if (filePath.ToLower().StartsWith("\\\\netapp-tpolab\\tcssqa1"))
                    {
                        var mls = new TCSWebService.MlsSoapClient();
                        result = mls.GetFileContent(filePath);
                    }
                    else if (filePath.ToLower().StartsWith("\\\\netapp02\\tcs_staging"))
                    {
                        var stagingMls = new StagingServiceReference.MlsSoapClient();
                        result = stagingMls.GetFileContent(filePath);
                    }
                    else if (filePath.ToLower().StartsWith("\\\\netapp02\\tcs"))
                    {
                        var productionMls = new ProductionServiceReference.MlsSoapClient();
                        result = productionMls.GetFileContent(filePath);
                    }
                    else if (filePath.StartsWith("Pr:"))
                    {
                        filePath = filePath.Replace("Pr:", "");
                        var productionMls = new ProductionServiceReference.MlsSoapClient();
                        result = productionMls.GetFileContent(filePath);
                    }


                    if (result.Length > 0)
                    {
                        if (!Directory.Exists(string.Format(@"{0}\TCSDownload",ConfigurationManager.AppSettings["Drive"])))
                            Directory.CreateDirectory(string.Format(@"{0}\TCSDownload",ConfigurationManager.AppSettings["Drive"]));
                        File.WriteAllText(System.IO.Path.Combine(string.Format(@"{0}\TCSDownload",ConfigurationManager.AppSettings["Drive"]), System.IO.Path.GetFileName(filePath)), result);
                        System.Diagnostics.Process process = new System.Diagnostics.Process
                            {
                                StartInfo = {UseShellExecute = true, FileName = string.Format(@"{0}\TCSDownload",ConfigurationManager.AppSettings["Drive"])}
                            };
                        process.Start();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("File does not exist.");
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void ResetDefProductionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UiServices.SetBusyState();
                var mls = new ProductionServiceReference.MlsSoapClient();
                string msg = "";

                var xDoc = new XmlDocument();
                xDoc.LoadXml("<DEFMLSRequest></DEFMLSRequest>");
                if (xDoc.DocumentElement != null)
                {
                    xDoc.DocumentElement.SetAttribute("update_mls_board", "1");
                    xDoc.DocumentElement.SetAttribute("board_id", BoardIDTextBox.Text);
                }

                string sOutput = "";

                try
                {
                    string sInput = xDoc.OuterXml;

                    sOutput = mls.GetDefList(sInput);

                    xDoc.LoadXml(sOutput);
                    if (xDoc.DocumentElement != null) msg = xDoc.DocumentElement.GetAttribute("result");
                }
                catch (Exception ex)
                {
                    msg += " (Error) " + ex.Message + "[" + sOutput + "]\r\n";
                }

                MessageBox.Show(msg);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void ResetDefQa1Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UiServices.SetBusyState();
                var mls = new TCSWebService.MlsSoapClient();
                string msg = "";


                var xDoc = new XmlDocument();
                xDoc.LoadXml("<DEFMLSRequest></DEFMLSRequest>");
                if (xDoc.DocumentElement != null)
                {
                    xDoc.DocumentElement.SetAttribute("update_mls_board", "1");
                    xDoc.DocumentElement.SetAttribute("board_id", BoardIDTextBox.Text);
                }

                string sOutput = "";

                try
                {
                    string sInput = xDoc.OuterXml;

                    sOutput = mls.GetDefList(sInput);

                    xDoc.LoadXml(sOutput);
                    if (xDoc.DocumentElement != null) msg = xDoc.DocumentElement.GetAttribute("result");
                }
                catch (Exception ex)
                {
                    msg += " (Error) " + ex.Message + "[" + sOutput + "]\r\n";
                }

                MessageBox.Show(msg);
                LoadDefFileListView();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void OpenStandardFieldDocButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UiServices.SetBusyState();
                SharepointXlsHelper.RefreshDataDictionary();
                System.Diagnostics.Process process = new System.Diagnostics.Process
                    {
                        StartInfo =
                            {
                                UseShellExecute = true,
                                FileName = "Excel.exe",
                                WorkingDirectory = string.Format(@"{0}\SpeedUp\",ConfigurationManager.AppSettings["Drive"]),
                                Arguments = SharepointXlsHelper.TcsStandardFieldDocLocalFilePath
                            }
                    };
                process.Start();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }

        }

        private void PublishCategorizationToQa1Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UiServices.SetBusyState();
                var mls = new TCSWebService.MlsSoapClient();
                string result = "";
                if (RDCCodeTextBox.Text.Trim().Length != 4 || ModuleIDTextBox.Text.Length != 4)
                {
                    MessageBox.Show("Module ID or Data source ID is not valid.", "Publish category", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }


                try
                {

                    //string result =tcsSoap.PublishCategorizationPackage(textBox_boardID.Text.Trim());
                    result = mls.PublishCategorizationPackageWithDataSourceID(ModuleIDTextBox.Text.Trim(), RDCCodeTextBox.Text.Trim());

                    if (string.IsNullOrEmpty(result))
                        result = "Categorization package has been published successfully";

                    if (result.IndexOf("\r\n", System.StringComparison.Ordinal) == -1)
                        result = result.Replace("\n", "\r\n");

                }
                catch (Exception ex)
                {
                    result = ex.Message;
                }
                MessageBox.Show(result);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void PublishCategorizationToProductionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UiServices.SetBusyState();
                var mls = new ProductionServiceReference.MlsSoapClient();
                if (RDCCodeTextBox.Text.Trim().Length != 4 || ModuleIDTextBox.Text.Length != 4)
                {
                    MessageBox.Show("Module ID or Data source ID is not valid.", "Publish category", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }

                if (MessageBoxResult.Yes == MessageBox.Show("Publishing categorization to Production. Are you Sure to Continue?", "Publish category", MessageBoxButton.YesNo, MessageBoxImage.Exclamation))
                {
                    string result = "";
                    try
                    {

                        //string result =tcsSoap.PublishCategorizationPackage(textBox_boardID.Text.Trim());
                            result = mls.PublishCategorizationPackageWithDataSourceID(ModuleIDTextBox.Text.Trim(), RDCCodeTextBox.Text.Trim());

                        if (string.IsNullOrEmpty(result))
                            result = "Categorization package has been published successfully";

                        if (result.IndexOf("\r\n", System.StringComparison.Ordinal) == -1)
                            result = result.Replace("\n", "\r\n");

                    }
                    catch (Exception ex)
                    {
                        result = ex.Message;
                    }
                    MessageBox.Show(result);
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void SettingButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var setting = new Setting();
                setting.Show();
                LoadDefFileListView();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void RetrieveCategorizationFromQa1Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UiServices.SetBusyState();
                var mls = new TCSWebService.MlsSoapClient();
                string result = "";
                if (RDCCodeTextBox.Text.Trim().Length != 4 || ModuleIDTextBox.Text.Length != 4)
                {
                    MessageBox.Show("Module ID or Data source ID is not valid.", "Publish category", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }

                try
                {

                    //string result =tcsSoap.PublishCategorizationPackage(textBox_boardID.Text.Trim());
                    result = mls.GetCategorizationPackageWithDataSourceID(ModuleIDTextBox.Text.Trim(), RDCCodeTextBox.Text.Trim());

                }
                catch (Exception ex)
                {
                    result = ex.Message;
                }
                File.WriteAllText(System.IO.Path.Combine(WorkingFolderTextBox.Text, "categoryzation_qa1.xml"), result);
                System.Diagnostics.Process process = new System.Diagnostics.Process
                    {
                        StartInfo =
                            {
                                UseShellExecute = true,
                                FileName = "iexplore.exe",
                                WorkingDirectory = WorkingFolderTextBox.Text,
                                Arguments =
                                    "File:\\\\" + System.IO.Path.Combine(WorkingFolderTextBox.Text, "categoryzation_qa1.xml")
                            }
                    };
                process.Start();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void RetrieveCategorizationFromProductionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UiServices.SetBusyState();
                var mls = new ProductionServiceReference.MlsSoapClient();
                string result = "";
                if (RDCCodeTextBox.Text.Trim().Length != 4 || ModuleIDTextBox.Text.Length != 4)
                {
                    MessageBox.Show("Module ID or Data source ID is not valid.", "Publish category", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }

                try
                {

                    //string result =tcsSoap.PublishCategorizationPackage(textBox_boardID.Text.Trim());
                    result = mls.GetCategorizationPackageWithDataSourceID(ModuleIDTextBox.Text.Trim(), RDCCodeTextBox.Text.Trim());

                }
                catch (Exception ex)
                {
                    result = ex.Message;
                }
                File.WriteAllText(System.IO.Path.Combine(WorkingFolderTextBox.Text, "categoryzation_pro.xml"), result);
                System.Diagnostics.Process process = new System.Diagnostics.Process
                    {
                        StartInfo =
                            {
                                UseShellExecute = true,
                                FileName = "iexplore.exe",
                                WorkingDirectory = WorkingFolderTextBox.Text,
                                Arguments =
                                    "File:\\\\" + System.IO.Path.Combine(WorkingFolderTextBox.Text, "categoryzation_pro.xml")
                            }
                    };
                process.Start();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void GenerateDataAggRequestAllButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UiServices.SetBusyState();
                string requestAll = @"<TCService ClientName=""ORCA""><Function>GetDataAggListings</Function><Login><password>{0}</password><password>{1}</password><UserAgent>{2}</UserAgent><RetsUAPwd>{3}</RetsUAPwd></Login><Board ModuleId=""{4}"" MetadataVersion=""0""/><Search BypassARAuthentication=""1"" Count=""0"" OverrideRecordsLimit=""9999999"" ST_MLSNo="""" IncludeCDATA=""1"" SelectPicFieldsOnly=""0"" ReturnDataAggListingsXML=""0"" ST_PublicListingStatus=""A"" /></TCService>";
                requestAll = string.Format(requestAll, Util.ConvertStringToXml(LoginNameTextBox.Text), Util.ConvertStringToXml(PasswordTextBox.Text), Util.ConvertStringToXml(UserAgentTextBox.Text), Util.ConvertStringToXml(UAPasswordTextBox.Text), ModuleIDTextBox.Text);

                string selectedFileName = ((DefFileData)listView1.SelectedItem).FileName;
                if (selectedFileName.IndexOf("ag.sql", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
                {
                    requestAll = @"<TCService ClientName=""ORCA""><Function>GetAgentRoster</Function><Login><password>{0}</password><password>{1}</password><UserAgent>{2}</UserAgent><RetsUAPwd>{3}</RetsUAPwd></Login><Board ModuleId=""{4}"" MetadataVersion=""0""/><Search BypassARAuthentication=""1"" Count=""0"" OverrideRecordsLimit=""9999999"" ReturnDataAggListingsXML=""0"" /></TCService>";
                }
                else if (selectedFileName.IndexOf("of.sql", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
                {
                    requestAll = @"<TCService ClientName=""ORCA""><Function>GetOfficeRoster</Function><Login><password>{0}</password><password>{1}</password><UserAgent>{2}</UserAgent><RetsUAPwd>{3}</RetsUAPwd></Login><Board ModuleId=""{4}"" MetadataVersion=""0""/><Search BypassARAuthentication=""1"" Count=""0"" OverrideRecordsLimit=""9999999"" ReturnDataAggListingsXML=""0"" /></TCService>";
                }
                else if (selectedFileName.IndexOf("oh.sql", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
                {
                    //request200 = @"<TCService ClientName=""ORCA""><Function>GetOpenHouse</Function><Login><password>{0}</password><password>{1}</password><UserAgent>{2}</UserAgent><RetsUAPwd>{3}</RetsUAPwd></Login><Board ModuleID=""{4}"" DefPath=""{4}"" MetadataVersion=""0""/><Search BypassARAuthentication=""1"" Count=""0"" DACount=""0"" RecordsLimit=""200"" ST_SearchDate=""{5}"" ReturnDataAggListingsXML=""1"" /></TCService>";
                    requestAll = @"<TCService ClientName=""ORCA""><Function>GetOpenHouse</Function><Login><password>{0}</password><password>{1}</password><UserAgent>{2}</UserAgent><RetsUAPwd>{3}</RetsUAPwd></Login><Board ModuleId=""{4}"" MetadataVersion=""0""/><Search BypassARAuthentication=""1"" Count=""0"" OverrideRecordsLimit=""9999999"" ST_SearchDate=""{5}"" ReturnDataAggListingsXML=""0"" /></TCService>";
                }
                else
                {
                    requestAll = @"<TCService ClientName=""ORCA""><Function>GetDataAggListings</Function><Login><password>{0}</password><password>{1}</password><UserAgent>{2}</UserAgent><RetsUAPwd>{3}</RetsUAPwd></Login><Board ModuleId=""{4}"" MetadataVersion=""0""/><Search BypassARAuthentication=""1"" Count=""0"" OverrideRecordsLimit=""9999999"" ST_MLSNo="""" IncludeCDATA=""1"" SelectPicFieldsOnly=""0"" ReturnDataAggListingsXML=""0"" ST_PublicListingStatus=""A"" /></TCService>";
                }
                string nextOneYear = DateTime.Now.ToString("MM/dd/yyyyTHH:mm:ss") + "-" + DateTime.Now.AddYears(1).ToString("MM/dd/yyyyTHH:mm:ss");

                requestAll = string.Format(requestAll, Util.ConvertStringToXml(LoginNameTextBox.Text), Util.ConvertStringToXml(PasswordTextBox.Text), Util.ConvertStringToXml(UserAgentTextBox.Text), Util.ConvertStringToXml(UAPasswordTextBox.Text), ModuleIDTextBox.Text, nextOneYear);

                Clipboard.SetDataObject(requestAll);
                AutoClosingMessageBox.Show("Copied to Clipboard", "Generate Request", 2000);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void LaunchRETSClientToolButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(Properties.Settings.Default.RETSClientToolPath))
                {
                    if (File.Exists(Properties.Settings.Default.RETSClientToolPath))
                    {
                        string defPath = System.IO.Path.Combine(WorkingFolderTextBox.Text, ((DefFileData)listView1.SelectedItems[0]).FileName);
                        Util.MakeFileWritable(defPath);
                        var def = new IniFile(defPath);

                        var sb = new StringBuilder();
                        sb.Append("[RETS Setting]\r\n");
                        sb.Append("$USER_NAME$=" + Util.ConvertStringToXml(LoginNameTextBox.Text) + "\r\n");
                        sb.Append("$PASSWORD$=" + Util.ConvertStringToXml(PasswordTextBox.Text) + "\r\n");
                        sb.Append("$METEDATA_QUERY$=" + "\r\n");
                        sb.Append("$RETS_VERSION$=" + def.Read("RetsVersion", "TcpIp") + "\r\n");
                        sb.Append("$HTTP_VERSION$=" + def.Read("HttpVersion") + "\r\n");
                        sb.Append("$LOGIN_URL$=" + LoginURLTextBox.Text + "\r\n");
                        sb.Append("$SEARCH_QUERY$=" + "\r\n");
                        sb.Append("$PICTURE_QUERY$=" + "\r\n");
                        sb.Append("$RETS_UA_USERAGENT$=" + "\r\n");
                        sb.Append("$RETS_UA_PASSWORD$=" + Util.ConvertStringToXml(UAPasswordTextBox.Text) + "\r\n");
                        sb.Append("$HTTP_USERAGENT$=" + UserAgentTextBox.Text + "\r\n"); ;
                        sb.Append("$HTTP_METHOD$" + def.Read("HttpMethod") + "\r\n");
                        sb.Append("$TCPIP_TYPE$=9" + "\r\n");
                        sb.Append("$USE_RELATIVEPATH$=" + def.Read("UseRelativePath") + "\r\n");
                        File.WriteAllText(System.IO.Path.Combine(WorkingFolderTextBox.Text, "RETSClientParameter.txt"), sb.ToString());

                        var process = new System.Diagnostics.Process
                            {
                                StartInfo =
                                    {
                                        UseShellExecute = true,
                                        FileName = Properties.Settings.Default.RETSClientToolPath,
                                        WorkingDirectory = WorkingFolderTextBox.Text,
                                        Arguments = System.IO.Path.Combine(WorkingFolderTextBox.Text, "RETSClientParameter.txt")
                                    }
                            };
                        process.Start();
                    }
                    else
                    {
                        MessageBox.Show("Can't locate the Application");
                    }
                }
                else
                {
                    MessageBox.Show("Can't locate the Application");
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void LaunchHandyMapperButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(Properties.Settings.Default.HandyMapperPath))
                {
                    if (File.Exists(Properties.Settings.Default.HandyMapperPath))
                    {
                        //Creat a batch file
                        var sb = new StringBuilder();
                        string fileName = ((DefFileData)listView1.SelectedItems[0]).FileName;
                        string defPath = System.IO.Path.Combine(WorkingFolderTextBox.Text, fileName);

                        sb.Append("#Name^ " + ModuleIDTextBox.Text + "_" + fileName.Substring(0, fileName.Length - 4) + "\r\n");
                        sb.Append("#LoadDef^ " + defPath + "\r\n");
                        if(File.Exists(WorkingFolderTextBox.Text + "\\metadata.xml"))
                            sb.Append("#LoadMeta^ " + WorkingFolderTextBox.Text + "\\metadata.xml" + "\r\n");
                        if(File.Exists(WorkingFolderTextBox.Text + "\\" + fileName.Substring(0, fileName.Length - 4) + "_grid.dat"))
                            sb.Append("#LoadSample^ " + WorkingFolderTextBox.Text + "\\" + fileName.Substring(0, fileName.Length - 4) + "_grid.dat" + "\r\n");

                        string batchFilePath = WorkingFolderTextBox.Text + "\\" + fileName.Substring(0, fileName.Length - 4) + "_hmbat.txt";
                        File.WriteAllText(batchFilePath, sb.ToString());


                        var process = new System.Diagnostics.Process
                            {
                                StartInfo =
                                    {
                                        UseShellExecute = true,
                                        FileName = Properties.Settings.Default.HandyMapperPath,
                                        Arguments = "BATCH " + batchFilePath
                                    }
                            };
                        //process.StartInfo.WorkingDirectory = WorkingFolderTextBox.Text;
                        process.Start();
                    }
                    else
                    {
                        MessageBox.Show("Can't locate the Application");
                    }
                }
                else
                {
                    MessageBox.Show("Can't locate the Application");
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void LaunchSearchToolButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(Properties.Settings.Default.SearchToolPath))
                {
                    if (File.Exists(Properties.Settings.Default.SearchToolPath))
                    {
                        string defPath = System.IO.Path.Combine(WorkingFolderTextBox.Text, ((DefFileData)listView1.SelectedItems[0]).FileName);
                        var process = new System.Diagnostics.Process
                            {
                                StartInfo =
                                    {
                                        UseShellExecute = true,
                                        FileName = Properties.Settings.Default.SearchToolPath,
                                        WorkingDirectory = WorkingFolderTextBox.Text
                                    }
                            };
                        string metadataPath = WorkingFolderTextBox.Text + "\\metadata.xml";
                        if (!File.Exists(metadataPath))
                            metadataPath = "";
                        process.StartInfo.Arguments = "\"" + metadataPath + "\" \"" + defPath + "\"";
                        process.Start();
                    }
                    else
                    {
                        MessageBox.Show("Can't locate the Application");
                    }
                }
                else
                {
                    MessageBox.Show("Can't locate the Application");
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void LaunchTesetHarnessButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(Properties.Settings.Default.TestHarnessPath))
            {
                if (File.Exists(Properties.Settings.Default.TestHarnessPath))
                {
                    string defPath = System.IO.Path.Combine(WorkingFolderTextBox.Text, ((DefFileData)listView1.SelectedItems[0]).FileName);
                    var process = new System.Diagnostics.Process
                        {
                            StartInfo =
                                {
                                    UseShellExecute = true,
                                    FileName = Properties.Settings.Default.TestHarnessPath,
                                    WorkingDirectory = WorkingFolderTextBox.Text,
                                    Arguments = defPath
                                }
                        };
                    process.Start();
                }
                else
                {
                    MessageBox.Show("Can't locate the Application");
                }
            }
            else
            {
                MessageBox.Show("Can't locate the Application");
            }
        }

        private void OpenTCSURLLinkPageButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("https://wiki.move.com/display/DA/TCS+URLs");
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void OpenOrcaDataSourceActiveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UiServices.SetBusyState();
                var process = new System.Diagnostics.Process
                    {
                        StartInfo =
                            {
                                UseShellExecute = true,
                                FileName = "Excel.exe",
                                WorkingDirectory = string.Format(@"{0}\SpeedUp\",ConfigurationManager.AppSettings["Drive"]),
                                Arguments = RdctcsDataSourceNameMappingLocalFilePath
                            }
                    };
                process.Start();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void ReleaseDEFFilesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (DefFileData item in listView1.Items)
                {
                    string fileName = item.FileName;
                    using (var tcsDB = new TCSEntitiesProduction())
                    {
                        tcsDB.Connection.Open();
                        var q = (from t in tcsDB.cma_mls_boards
                                 join d in tcsDB.cma_mls_board_connections
                                 on t.board_id equals d.board_id
                                 where d.connection_name == fileName
                                 select new { ModuleID = t.module_id }).Distinct();
                        foreach (var moduleId in q)
                        {
                            if (!moduleId.ModuleID.ToString().Equals(ModuleIDTextBox.Text))
                            {
                                throw new Exception("The DEF file name is using by other Module ID:" + moduleId.ModuleID.ToString());
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(CommentTextBox.Text.Trim()))
                {
                    throw new Exception("Comment can't be blank for Mls manager note");
                }

                if (MessageBoxResult.Yes == MessageBox.Show("Releasing DEF files to CSS Production server. Are you Sure to Continue?\r\n\r\nNote: \"" + CommentTextBox.Text + "\" will be added to Mls manager note", "Release DEF files", MessageBoxButton.YesNo, MessageBoxImage.Exclamation))
                {
                    string postData = "";

                    UiServices.SetBusyState();
                    ReleaseDefCookieContainer = new CookieContainer();
                    
                    RunHttpRequest("http://oep.toppro.net/onyxemployeePortal_Onyx/tp/mlsmanager/projectBrowser/frameset.asp", "", "", false);

                    //0.2 Login in with CSS

                    postData = string.Format("username={0}&password={1}&Submit=Login&bFlag=true\r\n", "alexm", "richmond");
                    RunHttpRequest("http://oep.toppro.net/onyxemployeePortal_Onyx/tp/mlsmanager/Login.asp", postData, "", false);
                    RunHttpRequest("http://oep.toppro.net/onyxemployeePortal_Onyx/tp/mlsmanager/projectupdate/frameset.asp?iProjectnumber=6334", "", "", false);

                    
                    foreach (DefFileData item in listView1.Items)
                    {
                        RunHttpRequest("http://oep.toppro.net/onyxemployeePortal_Onyx/tp/mlsmanager/Defs/defmanager.asp", "", "", false);
                        RunHttpRequest("http://oep.toppro.net/onyxemployeePortal_Onyx/tp/mlsmanager/projectupdate/MainUpdateForm.asp", "", "", false);
                        CookieCollection coolieCollection = ReleaseDefCookieContainer.GetCookies(new Uri("http://oep.toppro.net/onyxemployeePortal_Onyx"));
                        var cookie = coolieCollection["MLSProjectSession"];
                        if (cookie != null)
                        {
                            string cookies = cookie.Value;

                            cookies = cookies.Substring(0, cookies.IndexOf("iShippingID=", System.StringComparison.Ordinal)) + "iShippingID=" + ModuleIDTextBox.Text + cookies.Substring(cookies.IndexOf("iShippingID=") + 16);
                            var coolie = cookie;
                            coolie.Value = cookies;
                        }

                        //1. Upload local file
                        postData = string.Format("-----------------------------7dd38014111694\r\nContent-Disposition: form-data; name=\"Defile1\"; filename=\"{0}\"\r\nContent-Type: application/octet-stream\r\n\r\n", item.FileName);//"ixmcflag.sql");
                        string defFilePath = System.IO.Path.Combine(WorkingFolderTextBox.Text, item.FileName);
                        string content = File.ReadAllText(defFilePath, Encoding.GetEncoding("iso-8859-1"));
                        const string endingData = "\r\n-----------------------------7dd38014111694\r\nContent-Disposition: form-data; name=\"btnNext\"\r\n\r\nNext\r\n-----------------------------7dd38014111694--\r\n";

                        RunHttpRequest("http://oep.toppro.net/onyxemployeePortal_Onyx/tp/mlsmanager/Defs/Def%20File%20Manager/UploadFromLocal.asp", postData + content + endingData, "multipart/form-data; boundary=---------------------------7dd38014111694", false);

                        //2. Scan def file
                        var ini = new IniFile(defFilePath);
                        string version = ini.Read("Version");
                        postData = string.Format("Version%7C{0}={1}&DateModified%7C{0}={2}", item.FileName, version, DateTime.Now.ToString("M/dd/yyyy h:mm:ss tt"));//"ixmcflag.sql");
                        postData = HttpUtility.UrlEncode(postData);

                        RunHttpRequest("http://oep.toppro.net/onyxemployeePortal_Onyx/tp/mlsmanager/Defs/Def%20File%20Manager/ScanDefFiles.asp", postData, "", false);
                        //3. upload 
                        postData = "mnuServer=1&btnUploadDefs=Upload+Def+File%28s%29";
                        RunHttpRequest("http://oep.toppro.net/onyxemployeePortal_Onyx/tp/mlsmanager/Defs/Def%20File%20Manager/UploadToServer.asp", postData, "", false);

                    }
                    var cos = ReleaseDefCookieContainer.GetCookies(new Uri("http://oep.toppro.net/onyxemployeePortal_Onyx"));
                    foreach (Cookie co in cos)
                    {
                        co.Expires = DateTime.Now.Subtract(TimeSpan.FromDays(2));
                    }
                    cos = ReleaseDefCookieContainer.GetCookies(new Uri("http://oep.toppro.net/"));
                    foreach (Cookie co in cos)
                    {
                        co.Expires = DateTime.Now.Subtract(TimeSpan.FromDays(2));
                    }
                    ReleaseToMlsManager();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to upload DEF files to CSS server, please redo in MLS project manager and MLS manager.\r\n\r\n" + ex.Message);
            }

            
        }

        public void ReleaseToMlsManager()
        {
            try
            {
                //Thread.Sleep(5000);
                ReleaseDefCookieContainer = new CookieContainer();
                //var allCookies = GetAllCookies(ReleaseDefCookieContainer);
                //var cookies = ReleaseDefCookieContainer.GetCookies(new Uri("http://oep.toppro.net/onyxemployeePortal_Onyx"));
                //foreach (Cookie co in cookies)
                //{
                //    co.Expires = DateTime.Now.Subtract(TimeSpan.FromDays(2));
                //}
                //cookies = ReleaseDefCookieContainer.GetCookies(new Uri("http://oep.toppro.net/"));
                //foreach (Cookie co in cookies)
                //{
                //    co.Expires = DateTime.Now.Subtract(TimeSpan.FromDays(2));
                //}
                string postData =
                    "FirstDayOfWeek=1&DateOrder=MDY&DateSeparator=%2F&TimeSeparator=%3A&Is24Hour=FALSE&AMSuffix=AM&PMSuffix=PM&DecimalSymbol=.&NegativeSymbol=-&LocaleDateFormat=MM%2FDD%2FYYYY&LocaleTimeFormat=HH%3Amm+AM%2FPM&AMPMPosition=SUFFIX&ClientAppPath=%2Fonyxemployeeportal_onyx%2F&ClientsDecimalPlaces=2&ClientGroupingSymbol=%2C&ClientNegativeOrder=0&ClientCurrencyOrder=0";
                RunHttpRequest("http://oep.toppro.net/onyxemployeeportal_onyx/tp/mlsmanager/mlsmanager/frameset.asp", "",
                    "", true);
                RunHttpRequest(
                    "http://oep.toppro.net/onyxemployeeportal_onyx/common/onyxclientinitprocess.asp?referrer=/onyxemployeeportal_onyx/tp/mlsmanager/mlsmanager/frameset.asp",
                    postData, "", true);
                var userName = "";
                var password = "";
                var windowsUser = System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToLower().Replace("corp\\","").ToLower();
                switch (windowsUser)
                {
                    case "nchuah":
                        userName = "nchuah";
                        password = "nchuah";
                        break;
                    case "mhun":
                        userName = "mhun";
                        password = "mhun";
                        break;
                    case "mwinter":
                        userName = "mwinter";
                        password = "mwinter";
                        break;
                    case "amakhanko":
                        userName = "alexm";
                        password = "icma";
                        break;
                    case "hmcgillvray":
                        userName = "hmcgillvra";
                        password = "hmcgillvra";
                        break;
                    case "mwallin":
                        userName = "mwallin";
                        password = "mwallin";
                        break;
                    case "ogareys":
                        userName = "ogareys";
                        password = "ogareys";
                        break;
                    case "khiller":
                        userName = "khiller";
                        password = "khiller";
                        break;
                    default:
                        userName = "stewartd";
                        password = "mls";
                        break;
                }

                postData = string.Format("vUser={0}&vPassWord={1}&btnLogin=Login", userName, password);
                RunHttpRequest(
                    "http://oep.toppro.net/onyxemployeeportal_onyx/tp/mlsmanager/mlsmanager/login/login.asp", postData,
                    "", true);
                postData =
                    string.Format(
                        "iSearchType=1&sSearch={0}&btnSearch=Search&iCompatibilityFilter=0&chkFilterDiscontinued=on&sOrderBy=",
                        ModuleNameTextBox.Text.Trim().Replace(" ", "+"));
                var response =
                    RunHttpRequest(
                        "http://oep.toppro.net/onyxemployeeportal_onyx/tp/mlsmanager/mlsmanager/Search/BoardSearch.asp",
                        postData, "", true);

                var posStart = response.IndexOf("Boardsearch.asp?iBoardID=", System.StringComparison.Ordinal);
                if(posStart<1)
                    throw new Exception("Can't find board info");
                var posEnd = response.IndexOf("'", posStart + 1, System.StringComparison.Ordinal);
                var boardInfoUrl = response.Substring(posStart, posEnd - posStart);
                if(boardInfoUrl.IndexOf(ModuleIDTextBox.Text.Trim(), System.StringComparison.Ordinal)<0)
                    throw new Exception("Wrong board info:" + boardInfoUrl);
                RunHttpRequest(
                    string.Format("http://oep.toppro.net/onyxemployeeportal_onyx/tp/mlsmanager/mlsmanager/Search/{0}",
                        boardInfoUrl), "", "", true);
                RunHttpRequest(
                    "http://oep.toppro.net/onyxemployeeportal_onyx/tp/mlsmanager/mlsmanager/mlsdept/MLSDept.asp", "", "",
                    true);
                RunHttpRequest(
                    "http://oep.toppro.net/onyxemployeeportal_onyx/tp/mlsmanager/mlsmanager/mlsdept/mlsdept.asp?iFuncType=10&",
                    "", "", true);
                //Cookie: vUser=vPassWord=hab&vUsername=YigVlZih; vSession=sOrderBy=&iVendorID=51&vAccessLevel=1&vVendorname=Stratus&sLastSearch=TCS+Test+021607%2D2+board&iSearchType=&iBoardID=6160&vSystemName=Online&iShippingID=2144&vBoardName=TCS+Test+021607%2D2+Board&iCompatibilityFilter=0; offset=480; ASPSESSIONIDACTATAQR=LJHJLBCDPAJKIHBFHCKPKIJJ; S=6=5C22A18E%2DC51F%2D42D7%2D8C5E%2D77A1316D90A1&4=KYang&3=1&2=Onyx&1=1&0%5F10=True&7=3630936179; O%5FLD=7=FALSE&2=SUFFIX&9=MM%2FDD%2FYYYY&10=HH%253Amm%2520AM%2FPM&6=AM&4=%253A&5=%2F&3=MDY&8=PM&1=1; O%5FP=42024%2E655150463; O%5FBFIRSTTIME=1; O%5FLN=CCO=0&CGS=%252C&3=2&2=%2D&CNO=0&1=%2E; O%5FO%5F9=%2Fonyxemployeeportal%5Fonyx%2F; O%5FO%5F8=true; ASPSESSIONIDACRASDQR=JHGNLKMDPCJAGGFBCFDLCLOJ
                postData = "";

                //1. Set compatibility
                foreach (DefFileData item in listView1.Items)
                {
                    postData += item.FileName + "=2&";
                }

                RunHttpRequest(
                    "http://oep.toppro.net/onyxemployeeportal_onyx/tp/mlsmanager/mlsmanager/mlsdept/mlsdept.asp?iFuncType=10&",
                    postData, "", true);

                //2. Set minimum version number
                RunHttpRequest(
                    "http://oep.toppro.net/onyxemployeeportal_onyx/tp/mlsmanager/mlsmanager/mlsdept/mlsdept.asp?iFuncType=11&",
                    postData, "", true);

                postData = postData.Replace("2", "1");
                RunHttpRequest(
                    "http://oep.toppro.net/onyxemployeeportal_onyx/tp/mlsmanager/mlsmanager/mlsdept/mlsdept.asp?iFuncType=11&",
                    postData, "", true);

                //3. Push to TCS
                RunHttpRequest(
                    "http://oep.toppro.net/onyxemployeeportal_onyx/tp/mlsmanager/mlsmanager/mlsdept/mlsdept.asp?iFuncType=18&",
                    "", "", true);

                //4. Add note
                RunHttpRequest(
                    "http://oep.toppro.net/onyxemployeeportal_onyx/tp/mlsmanager/mlsmanager/notes/NoteFrame.asp?iNoteTypeID=3",
                    "", "", true);
                postData = string.Format("sNote={0}&btnSave=Save+Note&bInsertFlag=1",
                    CommentTextBox.Text.Replace(" ", "+"));
                RunHttpRequest(
                    "http://oep.toppro.net/onyxemployeeportal_onyx/tp/mlsmanager/mlsmanager/notes/addNotes.asp?iNoteTypeID=3",
                    postData, "", true);
            }
            catch (Exception exc)
            {
                MessageBox.Show("Failed to update Mls manager, please redo in Mls manager.\r\n\r\n" + exc.Message);
            }
            var cos = ReleaseDefCookieContainer.GetCookies(new Uri("http://oep.toppro.net/onyxemployeePortal_Onyx"));
            foreach (Cookie co in cos)
            {
                co.Expires = DateTime.Now.Subtract(TimeSpan.FromDays(2));
            }
            cos = ReleaseDefCookieContainer.GetCookies(new Uri("http://oep.toppro.net/"));
            foreach (Cookie co in cos)
            {
                co.Expires = DateTime.Now.Subtract(TimeSpan.FromDays(2));
            }
            
        }

        public static CookieCollection GetAllCookies(CookieContainer cookieJar)
        {
            CookieCollection cookieCollection = new CookieCollection();

            Hashtable table = (Hashtable)cookieJar.GetType().InvokeMember("m_domainTable",
                                                                            BindingFlags.NonPublic |
                                                                            BindingFlags.GetField |
                                                                            BindingFlags.Instance,
                                                                            null,
                                                                            cookieJar,
                                                                            new object[] { });

            foreach (var tableKey in table.Keys)
            {
                String str_tableKey = (string)tableKey;

                if (str_tableKey[0] == '.')
                {
                    str_tableKey = str_tableKey.Substring(1);
                }

                SortedList list = (SortedList)table[tableKey].GetType().InvokeMember("m_list",
                                                                            BindingFlags.NonPublic |
                                                                            BindingFlags.GetField |
                                                                            BindingFlags.Instance,
                                                                            null,
                                                                            table[tableKey],
                                                                            new object[] { });

                foreach (var listKey in list.Keys)
                {
                    String url = "http://" + str_tableKey + (string)listKey;
                    cookieCollection.Add(cookieJar.GetCookies(new Uri(url)));
                }
            }

            return cookieCollection;
        }

        private string RunHttpRequest(string url, string postData, string contentType, bool isMlsmanagerRequest)
        {
            //Thread.Sleep(500);
            var request = (HttpWebRequest)HttpWebRequest.Create(url);

            request.Method = !string.IsNullOrEmpty(postData) ? "POST" : "GET";
            request.UseDefaultCredentials = true;
            request.PreAuthenticate = true;
            request.Credentials = CredentialCache.DefaultCredentials; //new NetworkCredential("name", "password", "domain");
            request.CookieContainer = ReleaseDefCookieContainer;
            request.Headers["Pragma"] = "no - cache";
            request.Headers["Accept-Language"] = "en-US";
            request.Accept = "text/html, application/xhtml+xml, */*";
            request.KeepAlive = false;
            if(!isMlsmanagerRequest)
                request.Headers["Accept-Encoding"] = "gzip, deflate";
            request.UserAgent = "Mozilla/5.0 (compatible; MSIE 8.0; Windows NT 6.1; WOW64; Trident/5.0; BOIE9;ENUS)";
            if (!string.IsNullOrEmpty(postData))
            {
                request.ContentType = !string.IsNullOrEmpty(contentType)
                    ? contentType
                    : "application/x-www-form-urlencoded";
                byte[] byteArray = Encoding.GetEncoding("iso-8859-1").GetBytes(postData);
                request.ContentLength = byteArray.Length;
                using (var dataStream = request.GetRequestStream())
                {
                    // Write the data to the request stream.
                    dataStream.Write(byteArray, 0, byteArray.Length);

                    // Close the Stream object.
                    dataStream.Close();
                }
            }

            using (var response = (HttpWebResponse) request.GetResponse())
            {
                var sb = new StringBuilder();
                var buf = new byte[8192];
                var resStream = response.GetResponseStream();
                var count = 0;
                do
                {
                    count = resStream.Read(buf, 0, buf.Length);
                    if (count != 0)
                    {
                        sb.Append(Encoding.UTF8.GetString(buf, 0, count)); // just hardcoding UTF8 here
                    }
                } while (count > 0);
                var contentBoardList = sb.ToString();
                return contentBoardList;
            }
            
            return "";
        }

        private void CollectTimeZoneInfoButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var tcsDb = new TCSEntitiesProduction())
                {
                    tcsDb.Connection.Open();
                    var q = (from t in tcsDb.cma_mls_boards
                             select new { ModuleID = t.module_id }).Distinct();
                    foreach (var moduleId in q)
                    {

                        if (!moduleId.ModuleID.ToString().Equals(ModuleIDTextBox.Text))
                        {
                            throw new Exception("The DEF file name is using by other Module ID:" + moduleId.ModuleID.ToString());
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void CheckOutButton_Click(object sender, RoutedEventArgs e)
        {
            try 
            {
                UiServices.SetBusyState();
                var tfs = new TFSOperations("vstfsrv01", WorkingFolderTextBox.Text);
                tfs.CheckOut(WorkingFolderTextBox.Text, TFSOperations.PathType.Directory);
                LoadDefFileListView();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void GetLatestVersionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UiServices.SetBusyState();
                var tfs = new TFSOperations("vstfsrv01", WorkingFolderTextBox.Text);
                tfs.GetLatestVersion(WorkingFolderTextBox.Text, TFSOperations.PathType.Directory);
                LoadDefFileListView();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void UndoCheckoutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UiServices.SetBusyState();
                var tfs = new TFSOperations("vstfsrv01", WorkingFolderTextBox.Text);
                tfs.UndoPendingChange(WorkingFolderTextBox.Text, TFSOperations.PathType.Directory);
                LoadDefFileListView();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void CheckInButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UiServices.SetBusyState();
                var tfs = new TFSOperations("vstfsrv01", WorkingFolderTextBox.Text);
                var newFiles = _defFileDataCollection.Where(x => x.IsLatest == "New");
                foreach(var file in newFiles)
                {
                    tfs.PendAdd(System.IO.Path.Combine(WorkingFolderTextBox.Text, file.FileName));
                }
                tfs.CheckIn(WorkingFolderTextBox.Text, TFSOperations.PathType.Directory, 2, CheckInCommentsTextBox.Text);
                LoadDefFileListView();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void RunRequestButton_Click(object sender, RoutedEventArgs e)
        {
            UiServices.SetBusyState();
            try
            {
                var searchEngine = new SearchEngine();
                searchEngine.IsDebug = true;
                searchEngine.BoardID = 999999;
                string datetimeFolderName = DateTime.Now.ToString("yyyyMMddhhmmss");
                if (listView1.SelectedItems.Count == 0)
                {
                    var messageResult = MessageBox.Show("Please select one def file", "Select one Def file", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                string selectedFileName = ((DefFileData)listView1.SelectedItem).FileName;
                string runningDirectory = string.Format(@"{0}\speedup",ConfigurationManager.AppSettings["Drive"]); //System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                string resultFolder = System.IO.Path.Combine(System.IO.Path.Combine(runningDirectory, "Result"), datetimeFolderName);
                Directory.CreateDirectory(resultFolder);
                string defPath = System.IO.Path.Combine(resultFolder, ((DefFileData)listView1.SelectedItem).FileName);
                File.Copy(WorkingDirectory + ModuleId + "\\" + ((DefFileData)listView1.SelectedItem).FileName, System.IO.Path.Combine(resultFolder, ((DefFileData)listView1.SelectedItem).FileName), true);
                const string boardInfo = @"<Board BoardId=""99999"" DefPath=""{0}"" MetadataVersion=""0""/>";
                var requestText = CommentTextBox.Text;
                requestText = requestText.Substring(0, requestText.IndexOf("<Board", System.StringComparison.Ordinal)) +
                    string.Format(boardInfo, defPath) +
                              requestText.Substring(requestText.IndexOf("<Search", System.StringComparison.Ordinal));
                string result = searchEngine.RunClientRequest(requestText);

                string requestText2 = @"<TCService><Function>GetMetadata</Function><Board BoardId=""5499"" ModuleId=""""/><Search Type=""METADATA-AggregationResultsTable"" Format=""XML"" Recursive=""1""/></TCService>";
                string result2 = searchEngine.GetMetadata(requestText2);

                var fileList = new DirectoryInfo(resultFolder).GetFiles("*.dat", SearchOption.AllDirectories);
                try
                {
                    fileList.First().CopyTo(WorkingDirectory + ModuleId + "\\" + ((DefFileData)listView1.SelectedItem).FileName.Substring(0, ((DefFileData)listView1.SelectedItem).FileName.Length - 4) + "_grid.dat", true);
                }
                catch (Exception)
                {
                    MessageBox.Show("Failed to download data", "Failed to download data", MessageBoxButton.OK, MessageBoxImage.Error);

                }

                var commnicationFile = new DirectoryInfo(resultFolder).GetFiles("*.log", SearchOption.AllDirectories);
                {
                    string fileName = commnicationFile.First().FullName;
                    File.Copy(fileName, System.IO.Path.Combine(WorkingDirectory, ModuleId + "\\" + ((DefFileData)listView1.SelectedItem).FileName.Substring(0, ((DefFileData)listView1.SelectedItem).FileName.Length - 4) + "_comm.log"), true);
                }
                string defName = ((DefFileData)listView1.SelectedItem).FileName;
                File.Copy(resultFolder + "\\tcslisting.xml", WorkingDirectory + ModuleId + "\\" + defName.Substring(0, defName.Length - 4) + "_tcs.xml", true);
            }
            catch (Exception ex1)
            {
                MessageBox.Show(ex1.Message + ex1.StackTrace + ex1.Source);
            }
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            ReleaseToMlsManager();
        }
    }

    public class DefFileData
    {
        public string FileName { get; set; }
        public string FileSize { get; set; }
        public string BoardType { get; set; }
        public string PropertyClass { get; set; }
        public string Version { get; set; }
        public string CheckoutBy { get; set; }
        public string IsLatest { get; set; }
    }

    public class TfsFileData
    {
        public string FileName { get; set; }
        public string Length { get; set; }
        public string CheckoutBy { get; set; }
        public string Encoding { get; set; }
        public string IsAdd { get; set; }
        public string IsEdit { get; set; }
        public string IsLock { get; set; }
        public string IsLatest { get; set; }
    }

    public class AutoClosingMessageBox
    {
        readonly System.Threading.Timer _timeoutTimer;
        readonly string _caption;
        AutoClosingMessageBox(string text, string caption, int timeout)
        {
            _caption = caption;
            _timeoutTimer = new System.Threading.Timer(OnTimerElapsed,
                null, timeout, System.Threading.Timeout.Infinite);
            MessageBox.Show(text, caption);
        }
        public static void Show(string text, string caption, int timeout)
        {
            new AutoClosingMessageBox(text, caption, timeout);
        }
        void OnTimerElapsed(object state)
        {
            IntPtr mbWnd = FindWindow(null, _caption);
            if (mbWnd != IntPtr.Zero)
                SendMessage(mbWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            _timeoutTimer.Dispose();
        }
        const int WM_CLOSE = 0x0010;
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);
    }

    /// <summary>
    ///   Contains helper methods for UI, so far just one for showing a waitcursor
    /// </summary>
    public static class UiServices
    {

        /// <summary>
        ///   A value indicating whether the UI is currently busy
        /// </summary>
        private static bool IsBusy;

        /// <summary>
        /// Sets the busystate as busy.
        /// </summary>
        public static void SetBusyState()
        {
            SetBusyState(true);
        }

        /// <summary>
        /// Sets the busystate to busy or not busy.
        /// </summary>
        /// <param name="busy">if set to <c>true</c> the application is now busy.</param>
        private static void SetBusyState(bool busy)
        {
            if (busy != IsBusy)
            {
                IsBusy = busy;
                Mouse.OverrideCursor = busy ? Cursors.Wait : null;

                if (IsBusy)
                {
                    new DispatcherTimer(TimeSpan.FromSeconds(0), DispatcherPriority.ApplicationIdle, dispatcherTimer_Tick, Application.Current.Dispatcher);
                }
            }
        }

        /// <summary>
        /// Handles the Tick event of the dispatcherTimer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private static void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            var dispatcherTimer = sender as DispatcherTimer;
            if (dispatcherTimer != null)
            {
                SetBusyState(false);
                dispatcherTimer.Stop();
            }
        }
    }
}
