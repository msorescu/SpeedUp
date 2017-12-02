using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.OleDb;
using System.Net;
using System.IO;
using System.Xml;
using System.ComponentModel;
using System.Windows.Forms;
using System.Configuration;

namespace SpeedUp.Helper
{
    public class SharepointXlsHelper
    {
        public const string TcsStandardFieldDocSharePointFilePath = @"http://topix/sites/TopConnector/StandardFields/MOVE-Standard-Fields-Data-Dictionary.xls";
        //public const string TcsStandardFieldDocLocalFilePath = 
       
        public static string TcsStandardFieldDocLocalFilePath
        {
            get{return string.Format(@"{0}\SpeedUp\MOVE-Standard-Fields-Data-Dictionary.xls",ConfigurationManager.AppSettings["Drive"]);}    
        }

        public SharepointXlsHelper()
        {
        }
        public static void RefreshDataDictionary()
        {
            Util.DownloadSharepointDocument(TcsStandardFieldDocSharePointFilePath, TcsStandardFieldDocLocalFilePath);
        }

        public static SortedDictionary<int, string> GetMasterCategories()
        {

            bool failed = false;
            DataTable category = new DataTable();
            string filename = CommonUtilities.DataPath  + @"\movestandardfields.xls";
            OleDbConnection con = new OleDbConnection("Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + filename + ";Extended Properties=\"Excel 8.0;HDR=NO\"");
            con.Open();
            try
            {
                OleDbDataAdapter myCommand = new OleDbDataAdapter(" SELECT * FROM [Categories$]", con);
                myCommand.Fill(category);
                if (!SkipHeadNotes(category, "Type"))
                {
                    category.Clear();
                    myCommand = new OleDbDataAdapter(" SELECT * FROM [Categories (2)$]", con);
                    myCommand.Fill(category);
                    SkipHeadNotes(category, "Type");
                }

                con.Close();
            }
            catch
            {
                failed = true;
            }
            finally
            {
                con.Close();
            }

            if (failed)
            {
                MessageBox.Show("Cannot read Data Dictionary file from local computer! Don't load any data.");
                return null;
            }

            SortedDictionary<int, string> cats = new SortedDictionary<int, string>();

            int order = 0;
            for (int i = 0; i < category.Rows.Count; i++)
            {
                string sname = category.Rows[i]["System Name"].ToString();
                if (string.IsNullOrEmpty(sname))
                    continue;
                bool isRequired = (category.Rows[i]["Required"].ToString().ToLower().StartsWith("y"));
                cats.Add(order, sname + "|" + category.Rows[i]["Display Name"].ToString() + "|" + (isRequired ? "y" : ""));
                order++;
            }

            return cats;
        }
        public static DataTable ReadStandardFieldFromExcel()
        {
            DataTable listing = new DataTable();

            string filename = CommonUtilities.DataPath  + @"\movestandardfields.xls";
            OleDbConnection con = new OleDbConnection("Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + filename + ";Extended Properties=\"Excel 8.0;HDR=YES;IMEX=1\"");
            con.Open();
            try
            {
                OleDbDataAdapter myCommand = new OleDbDataAdapter(" SELECT * FROM [Listing$]", con);
                myCommand.Fill(listing);
                con.Close();
            }
            catch
            {
            }
            finally
            {
                con.Close();
            }
            SkipHeadNotes(listing, "Origin");

            return listing;
        }
        public static DataTable ReadAgentFieldFromExcel()
        {
            DataTable listing = new DataTable();

            string filename = CommonUtilities.DataPath + @"\movestandardfields.xls";
            OleDbConnection con = new OleDbConnection("Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + filename + ";Extended Properties=\"Excel 8.0;HDR=YES;IMEX=1\"");
            con.Open();
            try
            {
                OleDbDataAdapter myCommand = new OleDbDataAdapter(" SELECT * FROM [Agent$]", con);
                myCommand.Fill(listing);
                con.Close();
            }
            catch
            {
            }
            finally
            {
                con.Close();
            }
            SkipHeadNotes(listing, "Origin");

            return listing;
        }
        public static DataTable ReadOfficeFieldFromExcel()
        {
            DataTable listing = new DataTable();

            string filename = CommonUtilities.DataPath + @"\movestandardfields.xls";
            OleDbConnection con = new OleDbConnection("Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + filename + ";Extended Properties=\"Excel 8.0;HDR=YES;IMEX=1\"");
            con.Open();
            try
            {
                OleDbDataAdapter myCommand = new OleDbDataAdapter(" SELECT * FROM [Office$]", con);
                myCommand.Fill(listing);
                con.Close();
            }
            catch
            {
            }
            finally
            {
                con.Close();
            }
            SkipHeadNotes(listing, "Origin");

            return listing;
        }
        public static DataTable ReadOpenHouseFieldFromExcel()
        {
            DataTable listing = new DataTable();

            string filename = CommonUtilities.DataPath + @"\movestandardfields.xls";
            OleDbConnection con = new OleDbConnection("Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + filename + ";Extended Properties=\"Excel 8.0;HDR=YES;IMEX=1\"");
            con.Open();
            try
            {
                OleDbDataAdapter myCommand = new OleDbDataAdapter(" SELECT * FROM [OpenHouse$]", con);
                myCommand.Fill(listing);
                con.Close();
            }
            catch
            {
            }
            finally
            {
                con.Close();
            }
            SkipHeadNotes(listing, "Origin");

            return listing;
        }
        private static bool SkipHeadNotes(DataTable dt, string firstColumnHeader)
        {
            int noteRow = 0;
            bool found = false;
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (dt.Rows[i][0].ToString().Equals(firstColumnHeader))
                {
                    for (int j = 0; j < dt.Columns.Count; j++)
                    {
                        try
                        {
                            dt.Columns[j].ColumnName = dt.Rows[i][j].ToString();
                        }
                        catch
                        {
                            dt.Columns[j].ColumnName = Guid.NewGuid().ToString();
                        }
                    }
                    noteRow++;
                    found = true;
                    break;
                }
                else
                    noteRow++;
            }
            for (int j = noteRow - 1; j >= 0; j--)
                dt.Rows.RemoveAt(j);

            return found;
        }
    }
}
