using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpeedUp.Model;
using System.IO;

using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using System.Configuration;
using SpeedUp.Controls;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Net.Mail;
using System.Reflection;

namespace SpeedUp
{
    class SpeedUpCLI
    {
        private const string ReportHeader = "Module ID, Module Name, Login Type, RETS URL, Username, Password, UserAgent, UserAgentPass";
        private const string Query = @"SELECT distinct cmm.module_id as ModuleId, module_name as ModuleName,cmb.board_id as BoardId, {0} as DatasourceId FROM [TCS].[dbo].[cma_mls_modules] cmm, tcs.dbo.cma_mls_boards cmb, {1} pm
    where cmm.module_id=cmb.module_id and cmm.module_id = pm.ShippingID order by ModuleId,DatasourceId";

        static string s3Bucket = ConfigurationManager.AppSettings["AWSBucketName"];
        //static string keyName = "*** key name when object is created ***";
        //static string filePath = ConfigurationManager.AppSettings["FilePath"];
        static string accessKey = ConfigurationManager.AppSettings["AccessKey"];
        static string secretAccessKey = ConfigurationManager.AppSettings["SecretAccessKey"];
        static string serviceUrl = ConfigurationManager.AppSettings["ServiceUrl"];

        static string SMTPServer = ConfigurationManager.AppSettings["SMTPServer"];
        static string SMTPPort = ConfigurationManager.AppSettings["SMTPPort"];

        static string MailFrom = ConfigurationManager.AppSettings["MailFrom"];
        static string[] MailTo = ConfigurationManager.AppSettings["MailTo"].Split(';');
        static string[] MailCC = ConfigurationManager.AppSettings["MailCC"].Split(';');

        const String SUBJECT1 =
                "Notification Email Of Deleted Data Sources";

        const String SUBJECT2 =
                "Notification Email Of Empty DMQL reports";
        
        // The body of the email
        const String BODYHEADER =
            "<h1>Notification Email</h1>" +
            "<p>The following data source(s) have been deleted from TRACE:</p>";

        const String TABLEHEADER = @"<table style='width:100%'>
                              <tr>
                                <th align='left'>Data Source</th>
                                <th align='left'>Module ID</th> 
                                <th align='left'>Module Name</th>
                              </tr>";

        const string TABLELINE = @"<tr>
                                    <td>{0}</td>
                                    <td>{1}</td> 
                                    <td>{2}</td>
                                  </tr>";

        const string TABLEFOOTER = @"</table>";

        const string LINE = "<li><b>{0}</b> of module <i>{1} - {2}</i></li>";
                

        public static void Main(string[] args)
        {

            string reportType = ParseStringArg(args, "/type");
                        
            List<string> filePathsList = new List<string>();
            StringBuilder sbDeletedDataSourcesInfo = new StringBuilder();
                        
            switch (reportType)
            {
                case "listing":
                    StartListingReport(ref filePathsList);
                    break;
                case "roster":
                    StartRosterReport(ref filePathsList);
                    break;                
                case "rdclisting":
                    StartRDCListingReport(ref filePathsList, ref sbDeletedDataSourcesInfo);
                    break;
                case "rdcaotherlisting":
                    StartRDCOtherListingReport(ref filePathsList, ref sbDeletedDataSourcesInfo);
                    break;
                case "rdcroster":
                    StartRDCRosterReport(ref filePathsList, ref sbDeletedDataSourcesInfo);
                    break;
                case "rdcnontcslisting":
                    StartRDCNonTCSListingReport(ref filePathsList);
                    break;
                case "rdcnontcsroster":
                    StartRDCNonTCSRosterReport(ref filePathsList);
                    break;
                case "openhouse":
                    StartOpenHouseReport(ref filePathsList, ref sbDeletedDataSourcesInfo);
                    break;                    
                default:
                    StartListingReport(ref filePathsList);
                    break;
            }

            if (filePathsList.Count > 0)                
            {                
                //create aws object
                S3 s3 = new S3(accessKey, secretAccessKey, serviceUrl);

                foreach (var filePath in filePathsList)
                {
                    Console.WriteLine("Uploading the file {0}",filePath);
                    string newFileName = Path.GetFileName(filePath);
                    // Create new FileInfo object and get the Length.
                    FileInfo fInfo = new FileInfo(filePath);
                    if (fInfo.Length < 500)
                    {
                        Decimal fileSize = fInfo.Length;
                        SendEmail(SUBJECT2, String.Format("<h1>Notification Email</h1> <p>This <b>{0}</b> file report of {1:##.##} bytes length must be empty and will not be loaded in S3 bucket until it has been fixed.</p>", newFileName, fileSize), MailFrom, MailTo, MailCC);
                        break;
                    }
                    s3.UploadFile(filePath, s3Bucket, newFileName, false);
                }
                
            }

            // notify for deleted data sources
            if (!string.IsNullOrWhiteSpace(sbDeletedDataSourcesInfo.ToString()))
            {
                SendEmail(SUBJECT1, sbDeletedDataSourcesInfo.ToString(), MailFrom, MailTo, MailCC, BODYHEADER, TABLEHEADER, TABLEFOOTER);
            }
            
            //clean up the logs
            //Array.ForEach(Directory.GetDirectories(string.Format(@"{0}\SpeedUp\", ConfigurationManager.AppSettings["Drive"])), delegate(string path) { Array.ForEach(Directory.GetFiles(path), delegate(string pathFile) { try { File.Delete(pathFile); } catch {}  }); });
            
        }
        
        private static void  StartListingReport(ref List<string> filePaths)
        {
            short lastmodule = 0;
            TCSEntities tcsDb = null;            
            try
            {
                //                string query = @"SELECT distinct cmm.module_id as ModuleId, module_name as ModuleName,cmb.board_id as BoardId FROM [TCS].[dbo].[cma_mls_modules] cmm, tcs.dbo.cma_mls_boards cmb, tcs.dbo.prosoft_modules pm
                //    where cmm.module_id=cmb.module_id and cmm.module_id = pm.ShippingID order by ModuleId";// and pm.ShippingID=1796
                //                string query = @"SELECT distinct cmb.module_id as ModuleId, module_name as ModuleName,cmb.board_id as BoardId FROM [TCS].[dbo].[tcs_request_log] trl, tcs.dbo.cma_mls_boards cmb, tcs.dbo.cma_mls_modules cmm
                //  where trl.board_id = cmb.board_id and cmm.module_id=cmb.module_id and (client_name = 'tpo' or client_name = 'tmk' or client_name= 'ORCA')";
                //                string query = @"SELECT distinct cmb.module_id as ModuleId, module_name as ModuleName, cmb.board_id as BoardId
                //  FROM tcs.dbo.tcs_request_log trl inner join tcs.dbo.cma_mls_boards cmb on trl.board_id=cmb.board_id inner join tcs.dbo.cma_mls_modules cmm on cmm.module_id = cmb.module_id
                //  left join tcs.dbo.tcs_error_log tel on trl.request_id=tel.request_id
                //  where (client_name = 'tpo' or client_name = 'tmk') and tel.error_code is null and module_name not like '%Orca%Only%' and cmb.module_id not in (1686, 2333, 1101) and cmb.module_id in (1015,1115)
                //  order by ModuleId";// and cmm.module_id=1015";
                tcsDb = new TCSEntities();

                //var args = new DbParameter[] { new SqlParameter { } };
                var moduleList = tcsDb.ExecuteStoreQuery<ModuleEntity>(string.Format(Query,"''", "tcs.dbo.prosoft_modules"), null);
                //var UniqueUsers = from trl in tcsproddb.tcs_request_log
                //                  join cmb in tcsproddb.cma_mls_boards on trl.board_id equals cmb.board_id
                //                  join cmm in tcsproddb.cma_mls_modules on cmb.module_id equals cmm.module_id
                //                  where (users.client_name == "TMK" || users.client_name == "TPO")
                //                      && (users.board_id == Board) && tel.error_code != 60310
                //                  orderby users.when_created descending
                //                  group users by users.mls_user_name into uTable
                //                  select new { USER = uTable.Key, LOCATION = uTable.Max(x => x.location_path) };
                //var moduleList = from boards in tcsDb.cma_mls_boards
                //                 join modules in tcsDb.cma_mls_modules on boards.module_id equals modules.module_id
                //                 where (boards.board_status_id == 1)
                //                 && (boards.type == "Internet")
                //                 select new { boards.board_id, boards.module_id, modules.module_name };
                //tcsDb.Dispose();

                using (var sw = new StreamWriter(string.Format(@"{0}\speedup\CredentialReport" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".csv", ConfigurationManager.AppSettings["Drive"]), false))
                {
                    sw.WriteLine(ReportHeader);
                    ListhubSearch ls = new ListhubSearch();
                    int i = 0;
                    var helper = new PsaHelper();
                    List<string> validModuleIDList = helper.GetValidModuleIDs();
                    foreach (var module in moduleList)
                    {
                        if (!validModuleIDList.Contains(module.ModuleId.ToString())) continue; 
                        var manager = new CredentialGatheringManager();
                        BoardCredentials BoardInfo = new BoardCredentials();                        
                        if (module.ModuleId == lastmodule) continue;
                        if (module.ModuleId == 0) continue;
                        lastmodule = module.ModuleId;
                        BoardInfo.ModuleId = module.ModuleId;
                        BoardInfo.ModuleName = module.ModuleName.Replace(",", "-");
                        BoardInfo = manager.getCredentials(BoardInfo, sw);
                        if (true)// (BoardInfo.IsValidCredential)
                        {
                            ls.search(Convert.ToString(BoardInfo.BoardId), Convert.ToString(BoardInfo.ModuleId), BoardInfo);
                            i++;
                            //if (i > 80)
                            //    break;
                        }
                    }
                    ls.WriteToFile();                    
                    filePaths.Add(ls.LoginFilePath);
                    filePaths.Add(ls.DmqlFilePath);
 
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error message:{0}", ex.Message);
                if (tcsDb != null) tcsDb.Dispose();
            }            
        }

        private static void StartRosterReport(ref List<string> filePaths)
        {
            short lastmodule = 0;
            TCSEntities tcsDb = null;            
            try
            {
                //                string query = @"SELECT distinct cmm.module_id as ModuleId, module_name as ModuleName,cmb.board_id as BoardId FROM [TCS].[dbo].[cma_mls_modules] cmm, tcs.dbo.cma_mls_boards cmb, tcs.dbo.prosoft_modules pm
                //    where cmm.module_id=cmb.module_id and cmm.module_id = pm.ShippingID order by ModuleId";// and pm.ShippingID=1796
                //                string query = @"SELECT distinct cmb.module_id as ModuleId, module_name as ModuleName,cmb.board_id as BoardId FROM [TCS].[dbo].[tcs_request_log] trl, tcs.dbo.cma_mls_boards cmb, tcs.dbo.cma_mls_modules cmm
                //  where trl.board_id = cmb.board_id and cmm.module_id=cmb.module_id and (client_name = 'tpo' or client_name = 'tmk' or client_name= 'ORCA')";
                //                string query = @"SELECT distinct cmb.module_id as ModuleId, module_name as ModuleName, cmb.board_id as BoardId
                //  FROM tcs.dbo.tcs_request_log trl inner join tcs.dbo.cma_mls_boards cmb on trl.board_id=cmb.board_id inner join tcs.dbo.cma_mls_modules cmm on cmm.module_id = cmb.module_id
                //  left join tcs.dbo.tcs_error_log tel on trl.request_id=tel.request_id
                //  where (client_name = 'tpo' or client_name = 'tmk') and tel.error_code is null and module_name not like '%Orca%Only%' and cmb.module_id not in (1686, 2333, 1101) and cmb.module_id in (1015,1115)
                //  order by ModuleId";// and cmm.module_id=1015";
                tcsDb = new TCSEntities();

                //var args = new DbParameter[] { new SqlParameter { } };
                var moduleList = tcsDb.ExecuteStoreQuery<ModuleEntity>(string.Format(Query, "''", "tcs.dbo.prosoft_modules"), null);
                //var UniqueUsers = from trl in tcsproddb.tcs_request_log
                //                  join cmb in tcsproddb.cma_mls_boards on trl.board_id equals cmb.board_id
                //                  join cmm in tcsproddb.cma_mls_modules on cmb.module_id equals cmm.module_id
                //                  where (users.client_name == "TMK" || users.client_name == "TPO")
                //                      && (users.board_id == Board) && tel.error_code != 60310
                //                  orderby users.when_created descending
                //                  group users by users.mls_user_name into uTable
                //                  select new { USER = uTable.Key, LOCATION = uTable.Max(x => x.location_path) };
                //var moduleList = from boards in tcsDb.cma_mls_boards
                //                 join modules in tcsDb.cma_mls_modules on boards.module_id equals modules.module_id
                //                 where (boards.board_status_id == 1)
                //                 && (boards.type == "Internet")
                //                 select new { boards.board_id, boards.module_id, modules.module_name };
                //tcsDb.Dispose();
                                
                using (var sw = new StreamWriter(string.Format(@"{0}\speedup\CredentialReportRoster" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".csv",ConfigurationManager.AppSettings["Drive"]), false))
                {
                    sw.WriteLine(ReportHeader);
                    //ListhubSearch ls = new ListhubSearch("module_id,mls_name,data_source_id,resource_type,resource,class,search_limit,search_format,count_records,dqml_query\r\n");
                    ListhubSearch ls = new ListhubSearch();
                    ls.DmqlFilePath = string.Format(@"{0}\listhub\DMQLRoster-", ConfigurationManager.AppSettings["Drive"]);
                    ls.LoginFilePath = string.Format(@"{0}\listhub\CleanLoginsRoster-", ConfigurationManager.AppSettings["Drive"]); 
                    int i = 0;
                    var helper = new PsaHelper();
                    List<string> validModuleIDList = helper.GetValidModuleIDs();
                    foreach (var module in moduleList)
                    {
                        if (!validModuleIDList.Contains(module.ModuleId.ToString())) continue; 
                        var manager = new CredentialGatheringManager();
                        BoardCredentials BoardInfo = new BoardCredentials();                        
                        if (module.ModuleId != 1676 && 
                            module.ModuleId != 1972 &&
                            module.ModuleId != 1774 &&
                            module.ModuleId != 1751 &&
                            module.ModuleId != 1461 &&
                            module.ModuleId != 2842 &&
                            module.ModuleId != 2631 &&
                            module.ModuleId != 2357 &&
                            module.ModuleId != 2034 &&
                            module.ModuleId != 2038 &&
                            module.ModuleId != 2407 &&
                            module.ModuleId != 2022 &&
                            module.ModuleId != 2076 &&
                            module.ModuleId != 1966 &&
                            module.ModuleId != 2504 &&
                            module.ModuleId != 2656 &&
                            module.ModuleId != 2620 &&
                            module.ModuleId != 2485 &&
                            module.ModuleId != 1910 &&
                            module.ModuleId != 1765 &&
                            module.ModuleId != 1832 &&
                            module.ModuleId != 1335) continue; 
                        if (module.ModuleId == lastmodule) continue; 
                        if (module.ModuleId == 0) continue;
                        lastmodule = module.ModuleId;
                        BoardInfo.ModuleId = module.ModuleId;
                        BoardInfo.ModuleName = module.ModuleName.Replace(",", "-");
                        //BoardInfo = manager.getRosterCredentials(BoardInfo, sw);
                        //if (!BoardInfo.IsValidCredential) BoardInfo = manager.getCredentials(BoardInfo, sw);
                        BoardInfo = manager.getCredentials(BoardInfo, sw);
                        if (true)// (BoardInfo.IsValidCredential)
                        {
                            ls.rosterSearch(Convert.ToString(BoardInfo.BoardId), Convert.ToString(BoardInfo.ModuleId), BoardInfo);
                            i++;
                            //if (i > 80)
                            //    break;
                        }
                    }
                    ls.WriteToFile();                    
                    filePaths.Add(ls.LoginFilePath);
                    filePaths.Add(ls.DmqlFilePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error message:{0}", ex.Message);
                if (tcsDb != null) tcsDb.Dispose();
            }            
        }

        private static void StartRDCListingReport(ref List<string> filePaths, ref StringBuilder sbDeletedDataSources)
        {
            string lastdatasourceid = "";
            string lastdatasourceiddeleted = "";
            bool previousIsValidCredential = false;
                
            TCSEntities tcsDb = null;
            try
            {
                tcsDb = new TCSEntities();
                                
                var moduleList = tcsDb.ExecuteStoreQuery<ModuleEntity>(string.Format(Query,"pm.DSID", "tcs.dbo.rdc_modules"), null);
                
                using (var sw = new StreamWriter(string.Format(@"{0}\speedup\CredentialRDCReport" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".csv", ConfigurationManager.AppSettings["Drive"]), false))
                {
                    sw.WriteLine(ReportHeader);
                    ListhubSearch ls = new ListhubSearch("module_id,mls_name,data_source_id,resource_type,resource,class,search_limit,search_format,count_records_active,count_records_sold,count_records_offmarket,last_modified_date_field,last_modified_date_format,sold_date_field,sold_date_format,key_field,status_active_dqml_query,status_sold_dqml_query,status_offmarket_dqml_query,photo_collected_yn,photo_type,photo_count_field,photo_last_modified_field,photo_last_modified_format\r\n");
                    ls.DmqlFilePath = string.Format(@"{0}\listhub\DMQLRDC-", ConfigurationManager.AppSettings["Drive"]);
                    ls.LoginFilePath = string.Format(@"{0}\listhub\CleanLoginsRDC-", ConfigurationManager.AppSettings["Drive"]); 
                    int i = 0;
                    var helper = new PsaHelper();
                    List<string> validModuleIDList = helper.GetValidModuleIDs();
                    foreach (var module in moduleList)
                    {
                        if (!validModuleIDList.Contains(module.ModuleId.ToString())) continue; 

                        if (!helper.ExistDataSource(module.DatasourceId))
                        {
                            string datasourceidtodelete = helper.GetExactDataSourceId(module.ModuleId.ToString(), module.DatasourceId);
                            //notify by email
                            if (!string.IsNullOrWhiteSpace(datasourceidtodelete) && !datasourceidtodelete.Equals(lastdatasourceiddeleted))
                            {
                                lastdatasourceiddeleted = datasourceidtodelete;
                                sbDeletedDataSources.AppendFormat(TABLELINE, module.DatasourceId, module.ModuleId, module.ModuleName);
                                sbDeletedDataSources.AppendLine(); 
                            }
                            continue;
                        }
                        //if (module.module_id > 1100) break;
                        var manager = new CredentialGatheringManager();
                        BoardCredentials BoardInfo = new BoardCredentials();
                        if (module.DatasourceId.Equals(lastdatasourceid) && previousIsValidCredential) continue;
                        if (string.IsNullOrWhiteSpace(module.DatasourceId)) continue;
                        lastdatasourceid = module.DatasourceId;
                        BoardInfo.DatasourceId = module.DatasourceId;
                        BoardInfo.ModuleName = module.ModuleName.Replace(",", "-");
                        BoardInfo = manager.getRDCCredentials(BoardInfo, sw);
                        previousIsValidCredential = BoardInfo.IsValidCredential;
                        if (true)// (BoardInfo.IsValidCredential)
                        {
                            ls.rdcSearch(Convert.ToString(BoardInfo.BoardId), Convert.ToString(BoardInfo.ModuleId), Convert.ToString(BoardInfo.DatasourceId), BoardInfo);
                            i++;
                            //if (i > 80)
                            //    break;
                        }
                    }
                    ls.WriteToFile();
                    filePaths.Add(ls.LoginFilePath);
                    filePaths.Add(ls.DmqlFilePath);                    
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error message:{0}", ex.Message);
                if (tcsDb != null) tcsDb.Dispose();
            }
        }

        private static void StartRDCOtherListingReport(ref List<string> filePaths, ref StringBuilder sbDeletedDataSources)
        {
            string lastdatasourceid = "";
            string lastdatasourceiddeleted = "";
            bool previousIsValidCredential = false;

            TCSEntities tcsDb = null;
            try
            {
                tcsDb = new TCSEntities();

                var moduleList = tcsDb.ExecuteStoreQuery<ModuleEntity>(string.Format(Query, "pm.DSID", "tcs.dbo.rdc_othermodules"), null);
                
                using (var sw = new StreamWriter(string.Format(@"{0}\speedup\CredentialRDCReport" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".csv", ConfigurationManager.AppSettings["Drive"]), false))
                {
                    sw.WriteLine(ReportHeader);
                    ListhubSearch ls = new ListhubSearch("module_id,mls_name,data_source_id,resource_type,resource,class,search_limit,search_format,count_records_active,count_records_sold,count_records_offmarket,last_modified_date_field,last_modified_date_format,sold_date_field,sold_date_format,key_field,status_active_dqml_query,status_sold_dqml_query,status_offmarket_dqml_query,photo_collected_yn,photo_type,photo_count_field,photo_last_modified_field,photo_last_modified_format\r\n");
                    ls.DmqlFilePath = string.Format(@"{0}\listhub\DMQLOtherRDC-", ConfigurationManager.AppSettings["Drive"]);
                    ls.LoginFilePath = string.Format(@"{0}\listhub\CleanLoginsOtherRDC-", ConfigurationManager.AppSettings["Drive"]);
                    int i = 0;
                    var helper = new PsaHelper();
                    Dictionary<string, string> dictDSTranslator = helper.GetDataListSoldToActiveTranslation();
                    List<string> validModuleIDList = helper.GetValidModuleIDs();
                    foreach (var module in moduleList)
                    {                        
                        if (!validModuleIDList.Contains(module.ModuleId.ToString())) continue; 

                        if (!helper.ExistDataSource(module.DatasourceId))
                        {
                            string datasourceidtodelete = helper.GetExactDataSourceId(module.ModuleId.ToString(), module.DatasourceId);
                            //notify by email
                            if (!string.IsNullOrWhiteSpace(datasourceidtodelete) && !datasourceidtodelete.Equals(lastdatasourceiddeleted))
                            {
                                lastdatasourceiddeleted = datasourceidtodelete;
                                sbDeletedDataSources.AppendFormat(TABLELINE, module.DatasourceId, module.ModuleId, module.ModuleName);
                                sbDeletedDataSources.AppendLine();
                            }
                            continue;
                        }
                        var manager = new CredentialGatheringManager();
                        BoardCredentials BoardInfo = new BoardCredentials();
                        if (module.DatasourceId.Equals(lastdatasourceid) && previousIsValidCredential) continue;
                        if (string.IsNullOrWhiteSpace(module.DatasourceId)) continue;
                        lastdatasourceid = module.DatasourceId;
                        BoardInfo.DatasourceId = module.DatasourceId;
                        BoardInfo.ModuleName = module.ModuleName.Replace(",", "-");
                        BoardInfo = manager.getRDCOtherCredentials(BoardInfo, sw);
                        previousIsValidCredential = BoardInfo.IsValidCredential;
                        if (true)// (BoardInfo.IsValidCredential)
                        {
                            BoardInfo.MainDatasourceId = dictDSTranslator.ContainsKey(BoardInfo.DatasourceId.Trim()) ? dictDSTranslator[BoardInfo.DatasourceId.Trim()] : "";    
                            ls.rdcOtherSearch(Convert.ToString(BoardInfo.BoardId), Convert.ToString(BoardInfo.ModuleId), Convert.ToString(BoardInfo.DatasourceId), BoardInfo);
                            i++;
                            //if (i > 80)
                            //    break;
                        }
                    }
                    ls.WriteToFile();
                    filePaths.Add(ls.LoginFilePath);
                    filePaths.Add(ls.DmqlFilePath);

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error message:{0}", ex.Message);
                if (tcsDb != null) tcsDb.Dispose();
            }
        }

        private static void StartRDCRosterReport(ref List<string> filePaths , ref StringBuilder sbDeletedDataSources)
        {
            string lastdatasourceid = "";
            string lastdatasourceiddeleted = "";
            bool previousIsValidCredential = false;

            TCSEntities tcsDb = null;
            try
            {
                //                string query = @"SELECT distinct cmm.module_id as ModuleId, module_name as ModuleName,cmb.board_id as BoardId FROM [TCS].[dbo].[cma_mls_modules] cmm, tcs.dbo.cma_mls_boards cmb, tcs.dbo.prosoft_modules pm
                //    where cmm.module_id=cmb.module_id and cmm.module_id = pm.ShippingID order by ModuleId";// and pm.ShippingID=1796
                //                string query = @"SELECT distinct cmb.module_id as ModuleId, module_name as ModuleName,cmb.board_id as BoardId FROM [TCS].[dbo].[tcs_request_log] trl, tcs.dbo.cma_mls_boards cmb, tcs.dbo.cma_mls_modules cmm
                //  where trl.board_id = cmb.board_id and cmm.module_id=cmb.module_id and (client_name = 'tpo' or client_name = 'tmk' or client_name= 'ORCA')";
                //                string query = @"SELECT distinct cmb.module_id as ModuleId, module_name as ModuleName, cmb.board_id as BoardId
                //  FROM tcs.dbo.tcs_request_log trl inner join tcs.dbo.cma_mls_boards cmb on trl.board_id=cmb.board_id inner join tcs.dbo.cma_mls_modules cmm on cmm.module_id = cmb.module_id
                //  left join tcs.dbo.tcs_error_log tel on trl.request_id=tel.request_id
                //  where (client_name = 'tpo' or client_name = 'tmk') and tel.error_code is null and module_name not like '%Orca%Only%' and cmb.module_id not in (1686, 2333, 1101) and cmb.module_id in (1015,1115)
                //  order by ModuleId";// and cmm.module_id=1015";
                tcsDb = new TCSEntities();

                //var args = new DbParameter[] { new SqlParameter { } };
                var moduleList = tcsDb.ExecuteStoreQuery<ModuleEntity>(string.Format(Query, "pm.DSID","tcs.dbo.rdc_modules"), null);
                //var UniqueUsers = from trl in tcsproddb.tcs_request_log
                //                  join cmb in tcsproddb.cma_mls_boards on trl.board_id equals cmb.board_id
                //                  join cmm in tcsproddb.cma_mls_modules on cmb.module_id equals cmm.module_id
                //                  where (users.client_name == "TMK" || users.client_name == "TPO")
                //                      && (users.board_id == Board) && tel.error_code != 60310
                //                  orderby users.when_created descending
                //                  group users by users.mls_user_name into uTable
                //                  select new { USER = uTable.Key, LOCATION = uTable.Max(x => x.location_path) };
                //var moduleList = from boards in tcsDb.cma_mls_boards
                //                 join modules in tcsDb.cma_mls_modules on boards.module_id equals modules.module_id
                //                 where (boards.board_status_id == 1)
                //                 && (boards.type == "Internet")
                //                 select new { boards.board_id, boards.module_id, modules.module_name };
                //tcsDb.Dispose();

                using (var sw = new StreamWriter(string.Format(@"{0}\speedup\CredentialReportRDCRoster" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".csv", ConfigurationManager.AppSettings["Drive"]), false))
                {
                    sw.WriteLine(ReportHeader);
                    ListhubSearch ls = new ListhubSearch("module_id,mls_name,data_source_id,resource_type,resource,class,search_limit,search_format,count_records_active,count_records_sold,count_records_offmarket,last_modified_date_field,sold_date_field,key_field,status_active_dqml_query,status_sold_dqml_query,status_offmarket_dqml_query\r\n");
                    ls.DmqlFilePath = string.Format(@"{0}\listhub\DMQLRDCRoster-", ConfigurationManager.AppSettings["Drive"]);
                    ls.LoginFilePath = string.Format(@"{0}\listhub\CleanLoginsRDCRoster-", ConfigurationManager.AppSettings["Drive"]);
                    int i = 0;
                    var helper = new PsaHelper();
                    List<string> validModuleIDList = helper.GetValidModuleIDs();
                    foreach (var module in moduleList)
                    {
                        if (!validModuleIDList.Contains(module.ModuleId.ToString())) continue; 
                        if (!helper.ExistDataSource(module.DatasourceId))
                        {
                            string datasourceidtodelete = helper.GetExactDataSourceId(module.ModuleId.ToString(), module.DatasourceId);
                            //notify by email
                            if (!string.IsNullOrWhiteSpace(datasourceidtodelete) && !datasourceidtodelete.Equals(lastdatasourceiddeleted))
                            {
                                lastdatasourceiddeleted = datasourceidtodelete;
                                sbDeletedDataSources.AppendFormat(TABLELINE, module.DatasourceId, module.ModuleId, module.ModuleName);
                                sbDeletedDataSources.AppendLine();
                            }
                            continue;
                        }
                        var manager = new CredentialGatheringManager();
                        BoardCredentials BoardInfo = new BoardCredentials();
                        if (module.DatasourceId.Equals(lastdatasourceid) && previousIsValidCredential) continue;
                        if (string.IsNullOrWhiteSpace(module.DatasourceId)) continue;
                        lastdatasourceid = module.DatasourceId;
                        BoardInfo.DatasourceId = module.DatasourceId;
                        BoardInfo.ModuleName = module.ModuleName.Replace(",", "-");
                        BoardInfo = manager.getRDCRosterCredentials(BoardInfo, sw);
                        previousIsValidCredential = BoardInfo.IsValidCredential;
                        if (true)// (BoardInfo.IsValidCredential)
                        {
                            ls.rosterRDCSearch(Convert.ToString(BoardInfo.BoardId), Convert.ToString(BoardInfo.ModuleId), Convert.ToString(BoardInfo.DatasourceId), BoardInfo);
                            i++;
                            //if (i > 80)
                            //    break;
                        }
                    }
                    ls.WriteToFile();
                    filePaths.Add(ls.LoginFilePath);
                    filePaths.Add(ls.DmqlFilePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error message:{0}", ex.Message);
                if (tcsDb != null) tcsDb.Dispose();
            }
        }

        private static void StartOpenHouseReport(ref List<string> filePaths, ref StringBuilder sbDeletedDataSources)
        {
            string lastdatasourceid = "";
            string lastdatasourceiddeleted = "";
            bool previousIsValidCredential = false;

            TCSEntities tcsDb = null;
            try
            {
                tcsDb = new TCSEntities();

                var moduleList = tcsDb.ExecuteStoreQuery<ModuleEntity>(string.Format(Query, "pm.DSID", "tcs.dbo.rdc_modules"), null);

                using (var sw = new StreamWriter(string.Format(@"{0}\speedup\CredentialReportOpenHouse" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".csv", ConfigurationManager.AppSettings["Drive"]), false))
                {
                    sw.WriteLine(ReportHeader);
                    ListhubSearch ls = new ListhubSearch("module_id,mls_name,data_source_id,resource_type,resource,class,search_limit,search_format,count_records,key_field,dqml_query,start_datetime_field_name,start_datetime_field_format_low,start_datetime_field_format_delimiter,start_datetime_field_format_high,end_datetime_field_name,end_datetime_field_format_low,end_datetime_field_format_delimiter,end_datetime_field_format_high,datetime_field_operation,raw_dmql_query\r\n");
                    ls.DmqlFilePath = string.Format(@"{0}\listhub\DMQLOpenHouse-", ConfigurationManager.AppSettings["Drive"]);
                    ls.LoginFilePath = string.Format(@"{0}\listhub\CleanLoginsOpenHouse-", ConfigurationManager.AppSettings["Drive"]);
                    int i = 0;
                    var helper = new PsaHelper();
                    List<string> validModuleIDList = helper.GetValidModuleIDs();
                    foreach (var module in moduleList)
                    {
                        if (!validModuleIDList.Contains(module.ModuleId.ToString())) continue; 
                        if (!helper.ExistDataSource(module.DatasourceId))
                        {
                            string datasourceidtodelete = helper.GetExactDataSourceId(module.ModuleId.ToString(), module.DatasourceId);
                            //notify by email
                            if (!string.IsNullOrWhiteSpace(datasourceidtodelete) && !datasourceidtodelete.Equals(lastdatasourceiddeleted))
                            {
                                lastdatasourceiddeleted = datasourceidtodelete;
                                sbDeletedDataSources.AppendFormat(TABLELINE, module.DatasourceId, module.ModuleId, module.ModuleName);
                                sbDeletedDataSources.AppendLine();
                            }
                            continue;
                        }
                        var manager = new CredentialGatheringManager();
                        BoardCredentials BoardInfo = new BoardCredentials();                        
                        if (module.DatasourceId.Equals(lastdatasourceid) && previousIsValidCredential) continue;
                        if (string.IsNullOrWhiteSpace(module.DatasourceId)) continue;
                        lastdatasourceid = module.DatasourceId;
                        BoardInfo.DatasourceId = module.DatasourceId;
                        BoardInfo.ModuleName = module.ModuleName.Replace(",", "-");
                        BoardInfo = manager.getOpenHouseCredentials(BoardInfo, sw);
                        //if (!BoardInfo.IsValidCredential) BoardInfo = manager.getRDCOtherCredentials(BoardInfo, sw); 
                        previousIsValidCredential = BoardInfo.IsValidCredential;
                        if (true)// (BoardInfo.IsValidCredential)
                        {
                            ls.openhouseSearch(Convert.ToString(BoardInfo.BoardId), Convert.ToString(BoardInfo.ModuleId), Convert.ToString(BoardInfo.DatasourceId),  BoardInfo);
                            i++;
                            //if (i > 80)
                            //    break;
                        }
                    }
                    ls.WriteToFile();
                    filePaths.Add(ls.LoginFilePath);
                    filePaths.Add(ls.DmqlFilePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error message:{0}", ex.Message);
                if (tcsDb != null) tcsDb.Dispose();
            }
        }

        private static void StartRDCNonTCSListingReport(ref List<string> filePaths)
        {
            try
            {
                using (var sw = new StreamWriter(string.Format(@"{0}\speedup\CredentialRDCNonTCSReport" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".csv", ConfigurationManager.AppSettings["Drive"]), false))
                {
                    sw.WriteLine(ReportHeader);
                    ListhubSearch ls = new ListhubSearch("module_id,mls_name,data_source_id,resource_type,resource,class,search_limit,search_format,count_records_active,count_records_sold,count_records_offmarket,last_modified_date_field,sold_date_field,key_field,status_active_dqml_query,status_sold_dqml_query,status_offmarket_dqml_query,photo_collected_yn,photo_type,photo_count_field,photo_last_modified_field,photo_last_modified_format\r\n");
                    ls.DmqlFilePath = string.Format(@"{0}\listhub\DMQLRDCNONTCS-", ConfigurationManager.AppSettings["Drive"]);
                    ls.LoginFilePath = string.Format(@"{0}\listhub\CleanLoginsRDCNONTCS-", ConfigurationManager.AppSettings["Drive"]);
                    PsaHelper helper = new PsaHelper();
                    Dictionary<string, string> dictDSTranslator = helper.GetDataListSoldToActiveTranslation();
                    ls.GetNonTCSRETSLoginInfo(dictDSTranslator);
                    ls.GetNonTCSRETSListingConfigInfo(dictDSTranslator);
                    ls.WriteToFile();
                    filePaths.Add(ls.LoginFilePath);
                    filePaths.Add(ls.DmqlFilePath);

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error message:{0}", ex.Message);
            }
        }

        private static void StartRDCNonTCSRosterReport(ref List<string> filePaths)
        {
            try
            {
                using (var sw = new StreamWriter(string.Format(@"{0}\speedup\CredentialRDCNonTCSRosterReport" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".csv", ConfigurationManager.AppSettings["Drive"]), false))
                {
                    sw.WriteLine(ReportHeader);
                    ListhubSearch ls = new ListhubSearch("module_id,mls_name,data_source_id,resource_type,resource,class,search_limit,search_format,count_records_active,count_records_sold,count_records_offmarket,last_modified_date_field,sold_date_field,key_field,status_active_dqml_query,status_sold_dqml_query,status_offmarket_dqml_query\r\n");
                    ls.DmqlFilePath = string.Format(@"{0}\listhub\DMQLRDCNONTCSRoster-", ConfigurationManager.AppSettings["Drive"]);
                    //ls.GetNonTCSRETSLoginInfo();
                    PsaHelper helper = new PsaHelper();
                    Dictionary<string, string> dictDSTranslator = helper.GetDataListSoldToActiveTranslation();
                    ls.GetNonTCSRETSRosterConfigInfo(dictDSTranslator);
                    ls.WriteToFile();
                    //filePaths.Add(ls.LoginFilePath);
                    filePaths.Add(ls.DmqlFilePath);

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error message:{0}", ex.Message);
            }
        }

        private static String ParseStringArg(string[] args, String argName)
        {
            try
            {
                foreach (String arg in args)
                {
                    if (arg.StartsWith(argName + "=", StringComparison.OrdinalIgnoreCase))
                    {
                        return arg.Substring(argName.Length + 1).Trim();
                    }
                }
            }
            catch { }

            return null;
        }

        private static bool SendEmail(string Subject, string Body, string FromAddress, string[] ToAddresses, string[] CCAddresses, string BodyHeader = "",  string TableHeader = "", string TableFooter = "")
        {
            bool emailSuccess = false;

            try
            {
                MailMessage message = new MailMessage();

                message.From = new MailAddress(FromAddress);

                if (ToAddresses != null)
                {
                    foreach (string ToAddress in ToAddresses)
                    {
                        if (!string.IsNullOrEmpty(ToAddress))
                        {
                            message.To.Add(new MailAddress(ToAddress));
                        }
                    }
                }

                if (CCAddresses != null)
                {
                    foreach (string CCAddress in CCAddresses)
                    {
                        if (!string.IsNullOrEmpty(CCAddress))
                        {
                            message.CC.Add(new MailAddress(CCAddress));
                        }
                    }
                }

                //message.Headers.Add("X-SES-CONFIGURATION-SET", "ConfigSet");
                message.Subject = Subject;

                message.Body = string.Format("{0}{1}{2}{3}<p>Please proceed with updates as necessary.</p>", BodyHeader, TableHeader, Body, TableFooter);

                message.IsBodyHtml = true;

                SmtpClient client = new SmtpClient();
                client.Host = SMTPServer;
                client.Port = int.Parse(SMTPPort);                
                client.Send(message);

                emailSuccess = true;
            }
            catch (Exception ex)
            {
                string errMsg = String.Format("An unexpected error occurred in {0}.\n{1}", MethodInfo.GetCurrentMethod().Name, ex.ToString());
                Console.WriteLine(errMsg);
            }

            return emailSuccess;
        }
    }
}
