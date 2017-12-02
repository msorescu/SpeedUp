using System;
using System.Linq;
using System.Text;
using System.Net;
using System.Windows;
using System.Data.OleDb;
using System.Data;
using System.IO;
using Tcs.Mls;
using System.Configuration;

namespace SpeedUp.Helper
{
    public static class Util
    {
        public static void DownloadSharepointDocument(string sharePointPath, string saveToPath)
        {
            try
            {
                var fi = new FileInfo(saveToPath);
                if (fi.Directory != null && !fi.Directory.Exists)
                {
                    fi.Directory.Create();
                }

                if (File.Exists(saveToPath))
                    File.Delete(saveToPath);

                using (var wc = new WebClient())
                {
                    wc.Proxy = null;
                    wc.Credentials = CredentialCache.DefaultNetworkCredentials;
                    wc.DownloadFile(sharePointPath, saveToPath);
                }
            }
            catch (Exception ex)
            {
                    MessageBox.Show(ex.Message);
            }
        }

        public static string GetColumnValueByRowId(string rowName, string rowValue, string columnName, string fileName)
        {
            var dTable = new DataTable();

            //OleDbConnection con = new OleDbConnection("Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + fileName + ";Extended Properties=\"Excel 8.0;HDR=YES;IMEX=1\"");
            var con = new OleDbConnection("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + fileName + ";Extended Properties=Excel 12.0;");
            
            con.Open();
            try
            {
                var myCommand = new OleDbDataAdapter(" SELECT * FROM [Master$]", con);
                myCommand.Fill(dTable);
                con.Close();
            }
            catch
            {
            }
            finally
            {
                con.Close();
            }
            
            if (rowName == "Module ID")
            {
                var query = from p in dTable.AsEnumerable()
                        where p.Field<double?>(rowName) == Double.Parse(rowValue)
                        select new
                        {
                            name = p.Field<string>(columnName),
                        };
                return query.ToArray()[0].name;
            }
            else if (columnName == "Module ID")
            {
                var query1 = from p in dTable.AsEnumerable()
                             where String.Equals(p.Field<string>(rowName), rowValue, StringComparison.OrdinalIgnoreCase)
                        select new
                        {
                            name = p.Field<double?>(columnName),
                        };
                return query1.ToArray()[0].name.ToString();
            }
            else
            {
                var query2 = from p in dTable.AsEnumerable()
                             where String.Equals(p.Field<string>(rowName), rowValue, StringComparison.OrdinalIgnoreCase)
                             select new
                             {
                                 name = p.Field<string>(columnName),
                             };
                return query2.ToArray()[0].name.ToString();
            }
        }
        public static void MakeFileWritable(string filePath)
        {
            var attributes = File.GetAttributes(filePath);

            if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {
                // Make the file RW
                attributes = RemoveAttribute(attributes, FileAttributes.ReadOnly);
                File.SetAttributes(filePath, attributes);
            }
        }

        private static FileAttributes RemoveAttribute(FileAttributes attributes, FileAttributes attributesToRemove)
        {
            return attributes & ~attributesToRemove;
        }

        public static void RemoveComments(string filePath)
        {
            string[] inilines = File.ReadAllLines(filePath, Encoding.GetEncoding("iso-8859-1"));

            using (var sw = new StringWriter())
            {
                foreach (var line in inilines.Where(line => !line.StartsWith(";")))
                    sw.WriteLine(line);
                string iniClean = sw.ToString();
                File.WriteAllText(filePath, iniClean, Encoding.GetEncoding("iso-8859-1"));
            }
        }

        public static void ReplaceLoginUrl(string filePath, string loginUrl)
        {
            string[] inilines = File.ReadAllLines(filePath, Encoding.GetEncoding("iso-8859-1"));

            using (var sw = new StringWriter())
            {
                foreach (var line in inilines.Where(line => !line.StartsWith(";")))
                {
                    if (line.ToLower().StartsWith("transmit \"http:") && line.EndsWith("^M\""))
                    {
                        string newLine = "transmit \"" + loginUrl + "^M\"";
                        sw.WriteLine(newLine);
                    }
                    else
                        sw.WriteLine(line);
                }
                string iniClean = sw.ToString();
                File.WriteAllText(filePath, iniClean, Encoding.GetEncoding("iso-8859-1"));
            }
        }

        public static void ReplaceStringInFile(string filePath, string targetString, string destString )
        {
            string content = File.ReadAllText(filePath, Encoding.GetEncoding("iso-8859-1"));
            content = content.Replace(targetString, destString);
            File.WriteAllText(filePath, content, Encoding.GetEncoding("iso-8859-1"));
        }

        public static void AddCommentToIniFile(string filePath, string comment)
        {
            string content = File.ReadAllText(filePath, Encoding.GetEncoding("iso-8859-1"));
            content = comment + "\r\n" + content;
            File.WriteAllText(filePath, content, Encoding.GetEncoding("iso-8859-1"));
        }

        public static void DownloadMlsMetadata(string workingDirectory, string moduleId, string defPath, string loginName, string password, string userAgent, string uaPassword)
        {
            string resultFolder = "";
            try
            {
                string runningDirectory = string.Format(@"{0}\speedup",ConfigurationManager.AppSettings["Drive"]);
                string datetimeFolderName = DateTime.Now.ToString("yyyyMMddhhmmss");
                resultFolder = Path.Combine(Path.Combine(runningDirectory, "Result"), datetimeFolderName);

                var searchEngine = new SearchEngine();
                searchEngine.IsDebug = true;
                searchEngine.BoardID = 999999;

                Directory.CreateDirectory(resultFolder);
                string destDefPath = Path.Combine(resultFolder, Path.GetFileName(defPath));
                File.Copy(defPath, destDefPath, true);

                MakeFileWritable(destDefPath);
                var def = new IniFile(destDefPath);

                def.Write("GetmetaDataType", "?Type=METADATA-SYSTEM&ID=*", "TcpIp");
                //def.sav
                string message =
                    "<TCService priority=\"\"><Function>GetMLSMetadata</Function><Login><password>{0}</password><password>{1}</password><UserAgent>{2}</UserAgent><RetsUAPwd>{3}</RetsUAPwd></Login><Board BoardId=\"999999\" DefPath=\"{4}\"/><Search Log=\"0\" BypassARAuthentication=\"1\"/></TCService>";
                message = String.Format(message, ConvertStringToXml(loginName), ConvertStringToXml(password),
                                        ConvertStringToXml(userAgent), ConvertStringToXml(uaPassword), destDefPath);
                string result = searchEngine.RunClientRequest(message);

                File.Copy(resultFolder + "\\999999_Residential\\metadata.xml",
                          Path.Combine(Path.Combine(workingDirectory, moduleId), "metadata.xml"), true);
                var commnicationFile = new DirectoryInfo(resultFolder).GetFiles("*.log", SearchOption.AllDirectories);
                {
                    string fileName = commnicationFile.First().FullName;
                    File.Copy(fileName, Path.Combine(Path.Combine(workingDirectory, moduleId), "metadata.log"), true);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to download Mls metadata, please check the communication log for detail at " + resultFolder + "\\999999_Residential\r\n" + ex.Message);
            }
        }

        public static string ConvertStringToXml(string value)
        {
            var buffer = new System.Text.StringBuilder();

            int count = value.Length;
            for (int i = 0; i < count; i++)
            {
                char c = value[i];
                switch (c)
                {

                    case '&':
                        buffer.Append("&amp;");
                        break;

                    case '<':
                        buffer.Append("&lt;");
                        break;

                    case '>':
                        buffer.Append("&gt;");
                        break;

                    case '\'':
                        buffer.Append("&apos;");
                        break;

                    case '"':
                        buffer.Append("&quot;");
                        break;

                    default:
                        buffer.Append(c);
                        break;

                }
            }
            return buffer.ToString();
        }

        public static string GetValidName(string name)
        {
            var result = new StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                if ((name[i].CompareTo('0') >= 0 && name[i].CompareTo('9') <= 0) ||
                    (name[i].CompareTo('a') >= 0 && name[i].CompareTo('z') <= 0) ||
                    (name[i].CompareTo('A') >= 0 && name[i].CompareTo('Z') <= 0) ||
                    name[i] == '_')
                    result.Append(name[i]);
            }
            return result.ToString();
        }
    }
}
