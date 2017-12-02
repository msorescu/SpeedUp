using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Objects;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Threading;
using System.ComponentModel;
using SpeedUp.Controls;
using System.IO;
using SpeedUp.Helper;
using SpeedUp.Model;
using SpeedUp.Controler;
using Path = System.IO.Path;

namespace SpeedUp
{
    /// <summary>
    /// Interaction logic for NewConvert.xaml
    /// </summary>
    public partial class NewConvert : Window
    {
        public ObservableCollection<string> VendorCollection
        {
            get { return _vendorCollection; }
        }

        private readonly ObservableCollection<string> _vendorCollection = new ObservableCollection<string>();

        public ObservableCollection<string> ModuleCollection
        {
            get { return _moduleCollection; }
        }

        public string ModuleId
        {
            get { return _moduleId; }
            set { _moduleId = value; }
        }

        private readonly ObservableCollection<string> _moduleCollection = new ObservableCollection<string>();

        private string _state = "";
        private string _moduleId = "";
        private bool _stopProgressDialog = false;

        public NewConvert()
        {
            InitializeComponent();
            LoadVendorList();
            LoadModuleList();
            ModuleIDTextBox.Text = "";// "1318";
            LoginNameTextBox.Text = "";//"d75312";// "abq.rets.realtordotcom";
            PasswordTextBox.Text = "";//"RLTR1";//blair-ling87";
            UserAgentTextBox.Text = "";//"d_RLTR/2.0";
            LoginURLTextBox.Text = "";//"http://rets172lax.raprets.com:6103/rapcoop/COOP/login.aspx";
            // "http://retsgw.flexmls.com/rets2_1/Login";
            TRPTxtSelectFileTextBox.Text = "";//@"C:\test\PalmDesert_TRP_7_31_2013";
        }

        private void LoadVendorList()
        {
            using (TCSEntitiesProduction ctx = new TCSEntitiesProduction())
            {
                var query = from f1 in ctx.cma_mls_boards
                            join f2 in ctx.tcs_indexed_datasources on f1.module_id equals f2.module_id
                            where f1.vendor_name.Length > 0
                            select new {VendorName = f1.vendor_name};
                            

                    //ctx.tcs_module_class_settings.Join(ctx.cma_mls_boards, cma_mls_boards => cma_mls_boards.module_id,
                    //                        tcs_module_class_settings => tcs_module_class_settings.module_id,
                    //                        (tcs_module_class_settings,cma_mls_boards) =>
                    //                        new {cma_mls_boards.vendor_name})
                    //   .Select(x => new {VendorName = x.vendor_name})
                    //   .Where(x => x.VendorName.Length > 0)
                    //   .Distinct()
                    //   .OrderBy(x => x.VendorName);
                foreach (var vendor in query.Distinct().OrderBy(x=>x.VendorName))
                {
                    _vendorCollection.Add(vendor.VendorName);
                }
                VendorListComboBox.SelectedIndex = 0;
            }
        }

        private void LoadModuleList()
        {
            _moduleCollection.Clear();
            string venderName = VendorListComboBox.SelectedValue.ToString();
            if (!string.IsNullOrEmpty(venderName))
            {
                using (var ctx = new TCSEntitiesProduction())
                {
                    var query =
                        ctx.cma_mls_modules.Join(ctx.cma_mls_boards, cma_mls_boards => cma_mls_boards.module_id,
                                                 cma_mls_modules => cma_mls_modules.module_id,
                                                 (cma_mls_modules, cma_mls_boards) =>
                                                 new
                                                     {
                                                         cma_mls_modules.module_id,
                                                         cma_mls_modules.module_name,
                                                         cma_mls_boards.vendor_name,
                                                         cma_mls_boards.type
                                                     })
                           .Where(
                               cma_mls_boards =>
                               cma_mls_boards.vendor_name == venderName && cma_mls_boards.type.Equals("Internet") &&
                               cma_mls_boards.module_id > 0)
                           .Distinct()
                           .OrderBy(x => x.module_name);
                    List<string> moduleWithNoOhClass = new List<string>();
                    foreach (var module in query)
                    {
                        if (module.module_id.ToString().Equals(ModuleIDTextBox.Text.Trim()))
                            continue;
                        var q =
                            ctx.cma_mls_boards.Join(ctx.cma_mls_board_connections,
                                                    cma_mls_board_connections => cma_mls_board_connections.board_id,
                                                    cma_mls_boards => cma_mls_boards.board_id,
                                                    (cma_mls_boards, cma_mls_board_connections) =>
                                                    new
                                                        {
                                                            cma_mls_boards.module_id,
                                                            cma_mls_board_connections.connection_name
                                                        })
                               .Where(
                                   x =>
                                   x.module_id == module.module_id && x.connection_name.ToLower().EndsWith("oh.sql"))
                               .Distinct();
                        
                        if (q.Any())
                            _moduleCollection.Add(module.module_name + "(" + module.module_id + ")");
                        else
                        {
                            moduleWithNoOhClass.Add(module.module_name + "(" + module.module_id + ")");
                        }
                    }
                    foreach (var item in moduleWithNoOhClass)
                        _moduleCollection.Add(item);
                    ModuleListComboBox.SelectedIndex = 0;
                }
            }
        }

        private void VendorListComboBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            LoadModuleList();
        }

        private void GenerateDefFilesButton_Click(object sender, RoutedEventArgs e)
        {

            if (!IsValidInputFields())
            {
                return;
            }
            _stopProgressDialog = false;
            ModuleId = ModuleIDTextBox.Text;
            try
            {
                ProgressDialogResult result = ProgressDialog.Execute(this, "Loading data", (bw, we) =>
                    {

                        //1.Creating Def files from template def files
                        if (ProgressDialog.ReportWithCancellationCheck(bw, we, 10,
                                                                       "Creating Def files from template def files...",
                                                                       1))
                            return;
                        Thread.Sleep(1000);
                        this.Dispatcher.Invoke((Action) (CreateNewDefFiles));
                        if (_stopProgressDialog)
                        {
                            ProgressDialog.CloseProgressDialog(bw, we);
                            return;
                        }
                        //2.Download Metadata
                        if (ProgressDialog.ReportWithCancellationCheck(bw, we, 20, "Downloading Metadata...", 2))
                            return;
                        Thread.Sleep(1000);

                        this.Dispatcher.Invoke((Action) (DownloadMlsMetadata));
                        if (_stopProgressDialog)
                        {
                            ProgressDialog.CloseProgressDialog(bw, we);
                            return;
                        }

                        //6.Generate search field
                        if (ProgressDialog.ReportWithCancellationCheck(bw, we, 60, "Generating Def Files...", 9))
                            return;
                        Thread.Sleep(1000);
                        this.Dispatcher.Invoke((Action) (GenerateSearchFields));
                        if (_stopProgressDialog)
                        {
                            ProgressDialog.CloseProgressDialog(bw, we);
                            return;
                        }
                        if (ProgressDialog.ReportWithCancellationCheck(bw, we, 100, "Done", 10))
                            return;
                        Thread.Sleep(1000);


                        //7.Status
                        //8.Property type

                        // So this check in order to avoid default processing after the Cancel button has been pressed.
                        // This call will set the Cancelled flag on the result structure.
                        ProgressDialog.CheckForPendingCancellation(bw, we);

                    }, new ProgressDialogSettings(true, true, false));

                if (result.Cancelled)
                    MessageBox.Show("Failed to generate Def files.");
                else if (result.OperationFailed)
                    MessageBox.Show("ProgressDialog failed.");
                else
                {
                    MessageBox.Show("Def files have been successfully generated.");

                }
                if (!_stopProgressDialog)
                {
                    //Add Rets account info to db

                    using (TCSEntities tcsDb = new TCSEntities())
                    {
                        short moduleId = Convert.ToInt16(ModuleIDTextBox.Text);
                        if (!tcsDb.tcs_rets_connection_info.Any(x => x.module_id == moduleId))
                        {
                            tcs_rets_connection_info retsInfo = new tcs_rets_connection_info();
                            retsInfo.module_id = moduleId;
                            retsInfo.account_type = 1;
                            retsInfo.rdc_code = "N/A";
                            retsInfo.user_name = LoginNameTextBox.Text;
                            retsInfo.password = PasswordTextBox.Text;
                            retsInfo.user_agent = UserAgentTextBox.Text;
                            retsInfo.ua_password = UAPasswordTextBox.Text;
                            retsInfo.login_url = LoginURLTextBox.Text;

                            tcsDb.AddTotcs_rets_connection_info(retsInfo);
                            tcsDb.SaveChanges();
                        }
                    }

                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to generate Def files. \r\n" + ex.Message);
            }
        }

        private void GenerateDefFile(string defFilePath, Model.Metadata metaData, string traceFodler,
                                     string currentClassName)
        {
            var defParser = new DefController(defFilePath, metaData,
                                              traceFodler);
            if (!string.IsNullOrEmpty(currentClassName))
                defParser.DefFile.ClassName = currentClassName;
            string mlsClassName = defParser.DefFile.ClassName;
            if (defFilePath.ToLower().EndsWith(".sql"))
            {
                var classList = metaData.GetMlsMetadataClassList(mlsClassName);
                if (!classList.Any())
                {
                    if (defFilePath.ToLower().EndsWith("oh.sql"))
                    {
                        classList = metaData.GetMlsMetadataClassList("OpenHouse");
                        if (!classList.Any())
                        {
                            classList = metaData.GetMlsMetadataClassList("OPEN_HOUSE");
                        }
                    }
                }

                if (classList.Any())
                {
                    mlsClassName = classList.FirstOrDefault();
                    defParser.DefFile.ClassName = mlsClassName;
                    defParser.DefFile.ResourceName = metaData.GetResourceNameByClassName(mlsClassName);
                }
            }
            if (defFilePath.ToLower().EndsWith(".def"))
            {    
                defParser.LoadTraceFile();
            }
            defParser.GenerateStandardResultField();
            if (defFilePath.ToLower().EndsWith(".def"))
                defParser.UpdateAllSearchField();

            defParser.GenerateSearchSections();
            
            if (defFilePath.ToLower().EndsWith(".def"))
                defParser.GenerateNonStandardResultField();
            defParser.GenerateMlsRecordExSection();
            defParser.GenerateResultFieldSections();

            
            if (defFilePath.ToLower().EndsWith(".def"))
            {
                defParser.GenerateCategorySections();
                defParser.GenerateSortingSection();
                defParser.GeneratePictureScriptSection();
            }
            defParser.GenerateSecListSeciton();
            defParser.SaveDefFile();
            if(defFilePath.ToLower().EndsWith(".def"))
            {
                defParser.DefFile.SetKeyValue("BoardType", metaData.GetMlsMetadataClassVisibleName(mlsClassName),
                                              "Common");
                defParser.DefFile.SetKeyValue("Tcpip", "9", "Common");
            }
            defParser.DefFile.SetKeyValue("MetaDataversion", metaData.MetaDataVersion, "Common");
            defParser.DefFile = null;
            defParser = null;

        }

        private string SetDefName(string defFilePath, string visibleName)
        {
            string fileName = Path.GetFileName(defFilePath);
            string folderPath = defFilePath.Substring(0, defFilePath.LastIndexOf("\\"));
            if (visibleName.IndexOf("land", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
                fileName = fileName.Substring(0, 6) + "la.def";
            else if (visibleName.IndexOf("farm", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
                fileName = fileName.Substring(0, 6) + "fa.def";
            else if (visibleName.IndexOf("mul", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
                fileName = fileName.Substring(0, 6) + "mf.def";
            else if (visibleName.IndexOf("mobile", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
                fileName = fileName.Substring(0, 6) + "mb.def";
            else if (visibleName.IndexOf("Residential Income", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
                fileName = fileName.Substring(0, 6) + "ri.def";
            else if (visibleName.IndexOf("Residential Income", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
                fileName = fileName.Substring(0, 6) + "ri.def";
            else if (visibleName.IndexOf("Residential Income", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
                fileName = fileName.Substring(0, 6) + "ri.def";
            else if (visibleName.IndexOf("All Proper", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
                fileName = fileName.Substring(0, 6) + "re.def";
            else if (visibleName.IndexOf("Commercial", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
                fileName = fileName.Substring(0, 6) + "cm.def";
            else if (visibleName.IndexOf("condo", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
                fileName = fileName.Substring(0, 6) + "co.def";
            else if (visibleName.IndexOf("Commercial Lease", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
                fileName = fileName.Substring(0, 6) + "cl.def";
            else if (visibleName.IndexOf("Rental", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
                fileName = fileName.Substring(0, 6) + "rn.def";
            else if (visibleName.IndexOf("Lease", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
                fileName = fileName.Substring(0, 6) + "rl.def";
            else if (visibleName.IndexOf("Single Family Attached", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
                fileName = fileName.Substring(0, 6) + "co.def";
            else if (visibleName.IndexOf("Single Family", 0, StringComparison.CurrentCultureIgnoreCase) > -1)
                fileName = fileName.Substring(0, 6) + "re.def";
            else
            {
                Regex rgx = new Regex("[^a-zA-Z0-9]");
                visibleName = rgx.Replace(visibleName, " ");

                var words = visibleName.Split(' ').Where(x=>x.Length>1).ToArray();

                if (words.Length == 1)
                {
                    fileName = fileName.Substring(0, 6) + visibleName.Substring(0,2) + ".def";
                }
                else if (words.Length > 1)
                {
                    fileName = fileName.Substring(0, 6) + words[0].Trim().Substring(0, 1) +
                                   words[1].Trim().Substring(0, 1) + ".def";
                }

            }
            fileName = fileName.ToLower();
            if (File.Exists(Path.Combine(folderPath, fileName)))
                fileName =  fileName.Substring(0, 8) + "_changemelater" + (new Random().Next(1,100)) + ".def";
            File.Copy(defFilePath, Path.Combine(folderPath,fileName));

            return Path.Combine(folderPath, fileName);
        }

        private void GenerateSearchFields()
        {
            try
            {
                var di =
                    new DirectoryInfo(Path.Combine(Properties.Settings.Default.TFSLocalFolder, ModuleIDTextBox.Text));
                SharepointXlsHelper.RefreshDataDictionary();
                var metaData = new Model.Metadata(Path.Combine(di.FullName, "metadata.xml"));
                HashSet<string> mlsClassList = metaData.GetMlsMetadataClassList("Property");
                if(mlsClassList.Count==0)
                    mlsClassList = metaData.GetMlsMetadataClassList("PropertyResource");
                foreach (var fi in di.GetFiles())
                {
                    if (fi.Name.ToLower().EndsWith(".sql"))
                    {
                        if (fi.Name.ToLower().EndsWith("ag.sql"))
                        {
                            bool agentClassFound = false;
                            var classList = metaData.GetMlsMetadataClassList("ActiveAgent");
                            if (classList.Any())
                            {
                                GenerateDefFile(fi.FullName, metaData, TRPTxtSelectFileTextBox.Text, "ActiveAgent");
                                agentClassFound = true;
                            }
                            classList = metaData.GetMlsMetadataClassList("Agent");
                            if (classList.Any())
                            {
                                var agentFilePath = fi.FullName.ToLower().Replace("ag.sql", "Agentag.sql");
                                File.Copy(fi.FullName, agentFilePath);
                                GenerateDefFile(agentFilePath, metaData, TRPTxtSelectFileTextBox.Text, "Agent");
                                agentClassFound = true;
                            }
                            if (!agentClassFound)
                            {
                                classList = metaData.GetMlsMetadataClassList("User");
                                if (classList.Any())
                                {
                                    GenerateDefFile(fi.FullName, metaData, TRPTxtSelectFileTextBox.Text, "User");
                                }
                            }
                            
                        }else if (fi.Name.ToLower().EndsWith("of.sql"))
                        {

                            var classList = metaData.GetMlsMetadataClassList("ActiveOffice");
                            if (classList.Any())
                            {
                                GenerateDefFile(fi.FullName, metaData, TRPTxtSelectFileTextBox.Text, "ActiveOffice");
                            }
                            classList = metaData.GetMlsMetadataClassList("Office");
                            if (classList.Any())
                            {
                                var filePath = fi.FullName.ToLower().Replace("of.sql", "Officeof.sql");
                                File.Copy(fi.FullName, filePath);
                                GenerateDefFile(filePath, metaData, TRPTxtSelectFileTextBox.Text, "Office");
                            }
                        }
                        else if (!fi.Name.ToLower().EndsWith("oh.sql"))
                            GenerateDefFile(fi.FullName, metaData, TRPTxtSelectFileTextBox.Text, "");
                    }
                }
                if (mlsClassList.Count > 0)
                {
                    string filePath = "";
                    foreach (var item in mlsClassList)
                    {
                        foreach (var fi in di.GetFiles().Where(fi => fi.Name.ToLower().EndsWith(".def")))
                        {
                            filePath = fi.FullName;
                            break;
                        }
                    }
                    
                    foreach(var item in mlsClassList)
                    {
                        string visibleName = metaData.GetMlsMetadataClassVisibleName(item);
                        string newFilePath = SetDefName(filePath, visibleName);
                        GenerateDefFile(newFilePath, metaData, TRPTxtSelectFileTextBox.Text, item);
                    }
                    File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace + ex.Source);
                _stopProgressDialog = true;
            }
        }

        private void DownloadMlsMetadata()
        {
            string tempDef = "";
            try
            {
                string defFilesLocation = System.IO.Path.Combine(Properties.Settings.Default.TFSLocalFolder,
                                                                 ModuleIDTextBox.Text);
                string defPath = "";
                var dir = new DirectoryInfo(defFilesLocation);
                foreach (var fi in dir.GetFiles())
                {
                    if (fi.Name.ToLower().EndsWith(".def"))
                    {
                        defPath = fi.FullName;
                    }
                }

                tempDef = defPath.Substring(0, defPath.Length - 4) + "_temp.def";
                File.Copy(defPath,tempDef);
                IniFile iniFile = new IniFile(tempDef);
                iniFile.Write("Tcpip", "9", "Common");

                string content = File.ReadAllText(tempDef);
                int start = content.IndexOf("\r\n[MainScript]", 0, StringComparison.CurrentCultureIgnoreCase);
                int end = content.IndexOf("\r\n[", start + 6, StringComparison.CurrentCulture);

                string mainscript = "\r\n[MainScript]\r\ntransmit \"" + LoginURLTextBox.Text +
                                    "^M\"\r\ntransmit \"username=" + LoginNameTextBox.Text + "&password=" +
                                    PasswordTextBox.Text + "^M\"\r\n";

                content = content.Substring(0, start) + mainscript + content.Substring(end);
                File.WriteAllText(tempDef,content);
                Util.DownloadMlsMetadata(Properties.Settings.Default.TFSLocalFolder, ModuleIDTextBox.Text, tempDef,
                                         LoginNameTextBox.Text, PasswordTextBox.Text, UserAgentTextBox.Text,
                                         UAPasswordTextBox.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                _stopProgressDialog = true;
            }
            finally
            {
                if(File.Exists(tempDef))
                    File.Delete(tempDef);
            }
        }

        private bool IsValidInputFields()
        {
            if (!string.IsNullOrEmpty(ModuleIDTextBox.Text) && ModuleIDTextBox.Text.Length == 4)
            {
                if (BoardIDTextBox.Text.Length > 0 && ModuleNameTextBox.Text.Length > 0 &&
                    TRPTxtSelectFileTextBox.Text.Length > 0)
                {
                    if (!Directory.Exists(TRPTxtSelectFileTextBox.Text))
                    {
                        MessageBox.Show("TRP folder does not exist.");
                        return false;
                    }
                }
            }
            else
            {
                MessageBox.Show("Invalid module ID");
                return false;
            }
            if (LoginNameTextBox.Text.Length == 0)
            {
                MessageBox.Show("Login name can't be blank");
                return false;
            }
            if (PasswordTextBox.Text.Length == 0)
            {
                MessageBox.Show("Password can't be blank");
                return false;
            }
            if (UserAgentTextBox.Text.Length == 0)
            {
                MessageBox.Show("User agent can't be blank");
                return false;
            }
            //MessageBox.Show("Please check your input");
            return true;
        }

        private void CreateNewDefFiles()
        {
            //a.Get latest file version of template file from TFS
            var templateModuleId = ModuleListComboBox.SelectedValue.ToString();
            templateModuleId =
                templateModuleId.Substring(
                    startIndex: templateModuleId.LastIndexOf("(", System.StringComparison.Ordinal) + 1).Replace(")", "");
            try
            {
                var templateModuleDefFilePath = System.IO.Path.Combine(Properties.Settings.Default.TFSLocalFolder, templateModuleId);
                var tfs = new TFSOperations("vstfsrv01", templateModuleDefFilePath);
                tfs.GetLatestVersion(templateModuleDefFilePath, TFSOperations.PathType.Directory);

                using (var ctx = new TCSEntitiesProduction())
                {
                    short moduleId = Convert.ToInt16(ModuleIDTextBox.Text);
                    var query = ctx.cma_mls_boards.Where(x => x.module_id == moduleId).Select(x => new { StateName = x.state_name }).Distinct();
                
                    var firstOrDefault = query.FirstOrDefault();
                    if (firstOrDefault != null) _state = firstOrDefault.StateName;
                }

                string workingFodlerPath=System.IO.Path.Combine(Properties.Settings.Default.TFSLocalFolder, ModuleIDTextBox.Text);
                if (!Directory.Exists(workingFodlerPath))
                    Directory.CreateDirectory(workingFodlerPath);
                else
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(workingFodlerPath);
                    foreach (FileInfo file in dirInfo.GetFiles())
                    {
                        file.Delete();
                    }
                }

                var di = new DirectoryInfo(templateModuleDefFilePath);
                string defNamePrefix = VendorListComboBox.SelectedValue.ToString().Substring(0, 2) +
                                       ModuleNameTextBox.Text.Substring(0, 2) + _state.Substring(0, 2);
                defNamePrefix = defNamePrefix.ToLower();
                foreach (var fi in di.GetFiles())
                {
                    if ((fi.Name.ToLower().EndsWith(".def") || fi.Name.ToLower().EndsWith(".sql")) && !fi.Name.ToLower().EndsWith("da.sql"))
                    {
                        string desFilePath = "";
                        if(fi.Name.ToLower().EndsWith(".def"))
                        {
                            desFilePath = System.IO.Path.Combine(workingFodlerPath,
                                                                    defNamePrefix + "te.def");
                        }
                        else
                        {
                            desFilePath = System.IO.Path.Combine(workingFodlerPath,
                                                                    defNamePrefix + 
                                                                    fi.Name.Substring(fi.Name.Length - 6));
                        }
                        if (File.Exists(defNamePrefix + "te.def"))
                            continue;
                        var templateDef = new IniFile(fi.FullName);
                        string templateModuleName = templateDef.Read("BoardName", "Common");
                        File.Copy(fi.FullName, desFilePath, true);
                        Util.MakeFileWritable(desFilePath);
                        Util.RemoveComments(desFilePath);
                        Util.ReplaceLoginUrl(desFilePath, LoginURLTextBox.Text);
                        Util.ReplaceStringInFile(desFilePath, templateModuleName, ModuleNameTextBox.Text);
                        string comment = ";" + DateTime.Now.ToString("MMMM dd, yyyy") +
                                         " Auto generated by SpeedUp tool";
                        Util.AddCommentToIniFile(desFilePath, comment);
                        var def = new IniFile(desFilePath);
                        def.Write("BoardName", ModuleNameTextBox.Text, "Common");
                        def.Write("NameOfOrigin", VendorListComboBox.SelectedValue.ToString(), "Common");
                        def.Write("MLSSearchTimeZone", "Z", "Common");
                        def.Write("MLSResultsTimeZone", "Z", "Common");
                        def.Write("Version", "1", "Common");
                        def.Write("LastModified", DateTime.Now.ToString("MMM d, yyyy"), "Common");
                        def.DeleteSection("MLSRules");
                        def.DeleteSection("ResultScript");
                    }
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
                _stopProgressDialog = true;
            }

            //b.Modify the login URL
            //c.Clean up sections
        }

        private void ModuleIDTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (ModuleIDTextBox.Text.Length == 4)
            {
                try
                {
                    using (TCSEntitiesProduction tcsDB = new TCSEntitiesProduction())
                    {
                        tcsDB.Connection.Open();
                        short moduleID = Convert.ToInt16(ModuleIDTextBox.Text);
                        ObjectResult<tcs_GetBoardID_Result> boardIDResult = tcsDB.tcs_GetBoardID(moduleID);
                        foreach (var boardInfo in boardIDResult)
                        {
                            BoardIDTextBox.Text = boardInfo.board_id.ToString();
                        }
                        var query =
                            tcsDB.cma_mls_modules.Where(x => x.module_id == moduleID)
                                 .Select(x => new {ModuleName = x.module_name});
                        ModuleNameTextBox.Text = query.FirstOrDefault().ModuleName;
                    }
                }
                catch(Exception ex)
                {
                    MessageBox.Show("Invalid Module ID. Module ID is not found in TCS Qa1 server.\r\n" + ex.Message);
                }
            }

        }
    }
}
