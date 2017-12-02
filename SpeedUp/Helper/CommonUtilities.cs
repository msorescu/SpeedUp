using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.ComponentModel;
using System.Globalization;
using System.Data;
using System.Text.RegularExpressions;


namespace SpeedUp.Helper
{
    public class CommonUtilities
    {
        public static string DataPath
        {
            get
            {
//                string dataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\HandyMapper";
                string dataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\HandyMapper";
                if (!Directory.Exists(dataPath))
                    Directory.CreateDirectory(dataPath);

                return dataPath;
            }
        }
        public static string ProgramPath
        {
            get
            {
                return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            }
        }
        public static void saveBinaryFile(string fileName, byte[] bData)
        {
            Stream theStream;
            theStream = null;
            try
            {
                theStream = System.IO.File.OpenWrite(fileName);
                theStream.Write(bData, 0, bData.Length);
            }
            catch 
            {
            }
            finally
            {
                if (theStream != null)
                    theStream.Close();
            }
        }
        public static void saveDefFile(string fileName, string content)
        {
            try
            {
                if (content.Length < 16) //string.IsNullOrWhiteSpace(content)
                {
                    MessageBox.Show("Save DEF Failded: The saved content is empty.");
                    return; 
                }

                if (File.Exists(fileName))
                    File.Delete(fileName);

                using (StreamWriter outfile = new StreamWriter(fileName,true,System.Text.Encoding.Default))
                {
                    outfile.Write(content);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Save DEF Error: \r\n" + ex.Message);
            }
        }
        public static void saveTxtFile(string fileName, string content, bool overWrite)
        {
            try
            {
                if (overWrite)
                {
                    if (File.Exists(fileName))
                        File.Delete(fileName);
                }

                using (StreamWriter outfile = new StreamWriter(fileName, true, System.Text.Encoding.Default))
                {
                    outfile.Write(content);
                }
            }
            catch
            {
            }
        }
        public static byte[] readBinaryFile(string fileName)
        {
            Stream theStream;
            theStream = null;
            byte[] buffer;
            try
            {
                theStream = System.IO.File.OpenRead(fileName);
                FileInfo fi = new FileInfo(fileName);
                int byteCount = (int)fi.Length;
                buffer = new Byte[byteCount];
                theStream.Read(buffer, 0, byteCount);
            }
            catch 
            {
                buffer = null;
            }
            finally
            {
                if (theStream != null)
                    theStream.Close();
            }
            return buffer;
        }
        public static string loadTXTFile(string filename)
        {
            System.IO.FileStream stream = null;
            System.IO.StreamReader reader = null;
            System.IO.StreamReader buffered_reader = null;
            System.Text.StringBuilder def_file = new System.Text.StringBuilder();

            try
            {
                //UPGRADE_TODO: Constructor 'java.io.FileInputStream.FileInputStream' was converted to 'System.IO.FileStream.FileStream' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javaioFileInputStreamFileInputStream_javalangString'"
                stream = new System.IO.FileStream(filename, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                reader = new System.IO.StreamReader(stream, System.Text.Encoding.Default);
                //UPGRADE_TODO: The differences in the expected value  of parameters for constructor 'java.io.BufferedReader.BufferedReader'  may cause compilation errors.  "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1092'"
                buffered_reader = new System.IO.StreamReader(reader.BaseStream, reader.CurrentEncoding);

                System.String buf;

                while ((buf = buffered_reader.ReadLine()) != null)
                {
                    def_file.Append(buf);
                    def_file.Append("\r\n");
                }
            }
            catch 
            {
            }
            finally
            {
                try
                {
                    if (buffered_reader != null)
                        buffered_reader.Close();
                }
                catch 
                {
                }
                try
                {
                    if (reader != null)
                        reader.Close();
                }
                catch 
                {
                }
                try
                {
                    if (stream != null)
                        stream.Close();
                }
                catch 
                {
                }
            }
            return def_file.ToString();
        }
        public static void SetComboxCell(DataGridViewComboBoxCell cell, string curVal)
        {
            if (cell.Items.Contains(curVal))
                cell.Value = curVal;
            else
                cell.Value = "";
        }
        public static string GetCellValue(DataGridViewCell cell,string defVal)
        {
            string result = defVal;
            if (cell is DataGridViewComboBoxCell)
            {
                if (cell.Value != null)
                    result = cell.Value.ToString();
            }
            else if (cell is DataGridViewTextBoxCell)
            {
                if (cell.Value != null)
                    result = cell.Value.ToString();
            }
            else if (cell is DataGridViewCheckBoxCell)
            {
                if (cell.Value != null)
                    result = (bool)cell.Value?"Y":"N";
            }
            return result;
        }
        public static void SetYNfieldCell(DataGridViewComboBoxCell cell, string curVal, bool clear)
        {
            string defaultText = "";
            cell.Items.Clear();

            if (!clear)
            {
                cell.Items.Add(defaultText);
                cell.Items.Add("Y");
                cell.Items.Add("N");
                if (curVal.Equals("Y") || curVal.Equals("N"))
                    cell.Value = curVal;
                else
                    cell.Value = "";
            }
        }
        public static void SetFLDTypeCell(DataGridViewComboBoxCell cell, string curVal, bool clear)
        {
            string defaultText = "";
            cell.Items.Clear();

            if (!clear)
            {
                cell.Items.Add(defaultText);
                cell.Items.Add("S");
                cell.Items.Add("I");
                cell.Items.Add("L");
                cell.Items.Add("D");
                cell.Items.Add("DT");
                cell.Items.Add("B");
                if (cell.Items.Contains(curVal))
                    cell.Value = curVal;
                else
                    cell.Value = "";
            }
        }
        //public static void SetDisplayRuleCell(DataGridViewComboBoxCell cell, string curVal, bool clear)
        //{
        //    (new DisplayRule()).InitializeCell(cell, curVal, clear);
        //}
        public static string GetCSVField(string val, bool lineEnd)
        {
            if (val.IndexOf('\"') > -1)
            {
                val = "\"" + val.Replace("\"", "\"\"") + "\"";
            }
            else
            {
                char[] chs = { ',', '\r', '\n' };
                if (val.LastIndexOfAny(chs) > -1)
                    val = "\"" + val + "\"";
            }
            if (lineEnd)
                val = val + "\r\n";
            else
                val = val + ",";
            return val;
        }
        public static void ExportCSV(DataGridView grid)
        {
            SaveFileDialog saveDlg = new SaveFileDialog();
            saveDlg.Filter = "CSV file(*.csv)|*.csv";
            saveDlg.DefaultExt = "csv";
            saveDlg.Title = "Save csv file.";
            if (saveDlg.ShowDialog() == DialogResult.OK)
            {
                string fileName = saveDlg.FileName;

                try
                {
                    //header
                    StreamWriter saveFile = new StreamWriter(fileName);
                    int lastColumn = grid.Columns.Count - 1;
                    for (int i = lastColumn; i >=0; i--)
                    {
                        if (grid.Columns[i].Visible)
                        {
                            lastColumn = i;
                            break;
                        }
                    }
                    for (int i = 0; i < lastColumn; i++)
                    {
                        if (grid.Columns[i].Visible) 
                            saveFile.Write(GetCSVField(grid.Columns[i].HeaderText, false));
                    }
                    saveFile.Write(GetCSVField(grid.Columns[lastColumn].HeaderText, true));
                    //contents
                    int j = 0;
                    while (j < grid.Rows.Count)
                    {
                        DataGridViewRow dr = grid.Rows[j++];
                        if (!dr.Visible) continue;

                        for (int i = 0; i < lastColumn; i++)
                        {
                            if (grid.Columns[i].Visible)
                                saveFile.Write(GetCSVField(dr.Cells[i].Value.ToString(), false));
                        }
                        saveFile.Write(GetCSVField(dr.Cells[lastColumn].Value.ToString(), true));
                    }
                    saveFile.Close();
                }
                catch
                {
                }
            }
        }
        public static DataGridViewRow CloneRowWithValues(DataGridViewRow row)
        {
            DataGridViewRow clonedRow = (DataGridViewRow)row.Clone();
            for (Int32 index = 0; index < row.Cells.Count; index++)
            {
                clonedRow.Cells[index].Value = row.Cells[index].Value;
            }
            return clonedRow;
        }
        public static void ReplaceCellValuesByColumn(DataGridView grid, string column, string original, string now)
        {
            for (int i = 0; i < grid.Rows.Count; i++)
            {
                if (!string.IsNullOrEmpty(original))
                {
                    object columnValues = grid.Rows[i].Cells[column].Value;
                    if (columnValues != null)
                    {
                        string[] oriStrings = columnValues.ToString().Split(',');
                        for (int k = 0; k < oriStrings.Length; k++)
                        {
                            if (original.Equals(oriStrings[k].Trim()))
                                oriStrings[k] = now;
                        }
                        string result = "";
                        for (int k = 0; k < oriStrings.Length; k++)
                        {
                            if (!string.IsNullOrEmpty(oriStrings[k].Trim()))
                                result += oriStrings[k].Trim() + ",";
                        }
                        if (!string.IsNullOrEmpty(result))
                            result = result.Substring(0, result.Length - 1);
                        grid.Rows[i].Cells[column].Value = result;
                    }
                }
            }
        }
        public static List<DataGridViewRow> CheckValueUniqueInGrid(DataGridView grid, string field, string val)
        {
            List<DataGridViewRow> lRow = new List<DataGridViewRow>();

            if (string.IsNullOrEmpty(val))
                return lRow;


           for (int i = 0; i < grid.Rows.Count; i++)
            {
                string moveField = CommonUtilities.GetCellValue(grid.Rows[i].Cells[field], "");
                if (val.Equals(moveField))
                    lRow.Add(grid.Rows[i]);
            }
           return lRow;
        }
        public static DataGridViewRow GetRowByFieldAndValue(DataGridView grid, string field, string val)
        {
            for (int i = 0; i < grid.Rows.Count; i++)
            {
                string v = grid.Rows[i].Cells[field].Value.ToString();
                if (v.Equals(val))
                    return grid.Rows[i];
            }
            return null;
        }
        public static string GetStringFormRow(DataRow row, string field)
        {
            if (row.Table.Columns.Contains(field))
                return row[field].ToString();
            return "";
        }
        public static double GetSafeDouble(string st)
        {
            if (st.Length == 0) return 0;
            try
            {
                return Convert.ToDouble(st);
            }
            catch (Exception e)
            {
                return 0;
            }
        }
        public static int GetSafeInt(string st)
        {
            return GetSafeInt(st, 0);
        }
        public static int GetSafeInt(string st, int def)
        {
            try
            {
                return Convert.ToInt32(st);
            }
            catch (Exception e)
            {
                return def;
            }
        }
        public static string GetSafeSubString(string st, int start)
        {
            try
            {
                return st.Substring(start);
            }
            catch (Exception e)
            {
                return st;
            }
        }
        public static string GetSafeSubString(string st, int start, int len)
        {
            try
            {
                return st.Substring(start, len);
            }
            catch (Exception e)
            {
                return GetSafeSubString(st, start);
            }
        }
        public static DateTime GetSafeDate(string inputDate)
        {
            int MM = 0;
            int dd = 0;
            int yyyy = 0;
            string temp = inputDate;
            try
            {
                int index = temp.IndexOf("/");
                if (index > 1)
                {
                    MM = GetSafeInt(temp.Substring(0, index));
                    temp = temp.Substring(index + 1);
                    index = temp.IndexOf("/");
                    if (index > 1)
                    {
                        dd = GetSafeInt(temp.Substring(0, index));
                        yyyy = GetSafeInt(temp.Substring(index + 1));
                    }
                }
                if (yyyy == 0) yyyy = 2000;
                if (MM == 0 || dd == 0)
                    return DateTime.Now;
                else
                    return new DateTime(yyyy, MM, dd);
            }
            catch (Exception ex)
            {
                return DateTime.MinValue;
            }
        }
        public static string GetSafeTextFromSubNode(XmlNode node, string subName, string def)
        {
            try
            {
                return node.SelectSingleNode(subName).InnerText;
            }
            catch { }
            return def;
        }
        public static string EncodeForXml(string val)
        {
            Regex badAmpersand = new Regex("&(?![a-zA-Z]{2,6};|#[0-9]{2,4};)");
            val = badAmpersand.Replace(val, "&amp;");
            return val.Replace("<", "&lt;").Replace("\"", "&quot;").Replace(">", "&gt;").Replace("'", "&apos;");
        }
        public static string HTMLDecode(string html)
        {
            /*
            &amp;	&
            &gt;	>
            &lt;	<
            &quot;	"
            &apos;	'
             */

            int i = 0;
            string result = html;

            while (true)
            {
                i = result.IndexOf('&', i);
                if (i == -1)
                    break;
                if (result.Length < (i + 6))
                    break;

                if (result.Substring(i, 6).Equals("&nbsp;"))
                    result = result.Substring(0, i) + " " + result.Substring(i + 6);
                else if (result.Substring(i, 6).Equals("&quot;"))
                    result = result.Substring(0, i) + "\"" + result.Substring(i + 6);
                else if (result.Substring(i, 6).Equals("&apos;"))
                    result = result.Substring(0, i) + "'" + result.Substring(i + 6);
                else if (result.Substring(i, 5).Equals("&amp;"))
                    result = result.Substring(0, i) + "&" + result.Substring(i + 5);
                else if (result.Substring(i, 4).Equals("&lt;"))
                    result = result.Substring(0, i) + "<" + result.Substring(i + 4);
                else if (result.Substring(i, 4).Equals("&gt;"))
                    result = result.Substring(0, i) + ">" + result.Substring(i + 4);
                i++;
            }

            return result;
        }
    }
}