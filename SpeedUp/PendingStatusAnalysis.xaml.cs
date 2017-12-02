

namespace SpeedUp
{using System;
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
    /// <summary>
    /// Interaction logic for PendingStatusAnalysis.xaml
    /// </summary>
    public partial class PendingStatusAnalysis : Window
    {
        private const string ReportHeader = "Module ID, Module Name, RDC Code,RDC Name, TCS Pending Statuses,RDC Pending Statuses,Pending gap,Active dif not in TCS,Active dif not in RDC,Sold dif not in TCS,Sold dif not in RDC,OM dif not in TCS,OM dif not in RDC,Test Result,Remaining Gap,Status has no listing,Active count before Pending change,Active count After Pending change,% increase in Active Counts,Pending Listings in RDC Count";
        //private const string ReportHeader = "Module ID, Module Name, RDC Code,RDC Name, TCS Pending Statuses,RDC Pending Statuses, Pending gap, Tcs Active, RDC Active,Active dif not in TCS, Active dif not in RDC,Tcs Sold, RDC Sold, Sold dif not in TCS,Sold dif not in RDC, Tcs OM, Rdc OM, OM dif not in TCS,OM dif not in RDC";

        public PendingStatusAnalysis()
        {
            InitializeComponent();
        }

        private void GenerteGapReport_OnClick(object sender, RoutedEventArgs e)
        {
            TCSEntitiesProduction tcsDb = null;
            try
            {
                tcsDb = new TCSEntitiesProduction();

                var moduleList = tcsDb.tcs_module_id_to_data_source_id.Join(tcsDb.cma_mls_modules, md => md.module_id,
                                                                            m => m.module_id,
                                                                            (md, d) =>
                                                                            new
                                                                                {
                                                                                    ModuleId = md.module_id,
                                                                                    ModuleName = d.module_name,
                                                                                    RdcCode = md.data_source_id
                                                                                }).ToList();//.Where(x =>x.ModuleId == 1129 || x.ModuleId == 1148)
                tcsDb.Dispose();
                var manager = new PsaManager();
                using(var sw = new StreamWriter(string.Format(@"{0}\speedup\PendingStatusReport" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".csv", ConfigurationManager.AppSettings["Drive"]),false))
                {
                    sw.WriteLine(ReportHeader);
                    foreach (var module in moduleList)
                    {
                        manager.CompareTcsRdcActivePendingStatus(new PendingStatusCompareResult
                            {
                                ModuleId = module.ModuleId,
                                RdcCode = module.RdcCode.Trim(),
                                ModuleName = module.ModuleName.Trim()
                            }, sw);
                    }
                }
            }
            catch (Exception ex)
            {
                if (tcsDb != null) tcsDb.Dispose();
            }
        }

    }
}
