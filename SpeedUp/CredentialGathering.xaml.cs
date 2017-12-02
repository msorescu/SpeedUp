using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SpeedUp.Model;
using System.Configuration;


namespace SpeedUp
{
    /// <summary>
    /// Interaction logic for CredentialGathering.xaml
    /// </summary>
    public partial class CredentialGathering : Window
    {
        private const string ReportHeader = "Module ID, Module Name, Login Type, RETS URL, Username, Password, UserAgent, UserAgentPass";
        private const string Query = @"SELECT distinct cmm.module_id as ModuleId, module_name as ModuleName,cmb.board_id as BoardId FROM [TCS].[dbo].[cma_mls_modules] cmm, tcs.dbo.cma_mls_boards cmb, tcs.dbo.prosoft_modules pm
    where cmm.module_id=cmb.module_id and cmm.module_id = pm.ShippingID order by ModuleId";

        public CredentialGathering()
        {
            InitializeComponent();
        }

        private void StartReportListings_Click(object sender, RoutedEventArgs e)
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
                var moduleList = tcsDb.ExecuteStoreQuery<ModuleEntity>(Query, null);
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

                using (var sw = new StreamWriter(string.Format(@"{0}\speedup\CredentialReport" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".csv",ConfigurationManager.AppSettings["Drive"]), false))
                {
                    sw.WriteLine(ReportHeader);
                    ListhubSearch ls = new ListhubSearch();
                    int i = 0;
                    foreach (var module in moduleList)
                    {
                        //if (module.module_id > 1100) break;
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
                }
            }
            catch (Exception ex)
            {
                
            }
        }

        private void StartReportRosters_Click(object sender, RoutedEventArgs e)
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
                var moduleList = tcsDb.ExecuteStoreQuery<ModuleEntity>(Query, null);
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
                    ls.DmqlFilePath = string.Format(@"{0}\listhub\DMQLRoster-",ConfigurationManager.AppSettings["Drive"]);
                    ls.LoginFilePath = string.Format(@"{0}\listhub\CleanLoginsRoster-",ConfigurationManager.AppSettings["Drive"]);
                    int i = 0;
                    foreach (var module in moduleList)
                    {
                        //if (module.module_id > 1100) break;
                        var manager = new CredentialGatheringManager();
                        BoardCredentials BoardInfo = new BoardCredentials();
                        if (module.ModuleId == lastmodule) continue; ;
                        if (module.ModuleId == 0) continue;
                        lastmodule = module.ModuleId;
                        BoardInfo.ModuleId = module.ModuleId;
                        BoardInfo.ModuleName = module.ModuleName.Replace(",", "-");
                        BoardInfo = manager.getRDCCredentials(BoardInfo, sw);
                        if (!BoardInfo.IsValidCredential) BoardInfo = manager.getCredentials(BoardInfo, sw);
                        if (true)// (BoardInfo.IsValidCredential)
                        {
                            ls.rosterSearch(Convert.ToString(BoardInfo.BoardId), Convert.ToString(BoardInfo.ModuleId), BoardInfo);
                            i++;
                            //if (i > 80)
                            //    break;
                        }
                    }
                    ls.WriteToFile();
                }
            }
            catch (Exception ex)
            {
                if (tcsDb != null) tcsDb.Dispose();
            }
        }
        
    }
}
