using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

// Change the namespace to match your program's normal namespace
namespace SpeedUp
{
    class IniFile
    {
        public string Path;
        static string EXE = Assembly.GetExecutingAssembly().GetName().Name;
        private string _content;

        [DllImport("kernel32")]
        static extern long WritePrivateProfileString(string Section, string Key, string Value, string FilePath);

        [DllImport("kernel32")]
        static extern int GetPrivateProfileString(string Section, string Key, string Default, StringBuilder RetVal, int Size, string FilePath);

        [DllImport("kernel32")]
        static extern int GetPrivateProfileString(int Section, string Key,
               string Value, [MarshalAs(UnmanagedType.LPArray)] byte[] Result,
               int Size, string FileName);

        [DllImport("kernel32")]
        static extern int GetPrivateProfileString(string Section, int Key,
               string Value, [MarshalAs(UnmanagedType.LPArray)] byte[] Result,
               int Size, string FileName);

        public IniFile(string IniPath = null)
        {
            Path = new FileInfo(IniPath != null ? IniPath : EXE + ".ini").FullName.ToString();
        }

        public string Read(string Key, string Section = null)
        {
            StringBuilder RetVal = new StringBuilder(255);
            GetPrivateProfileString(Section != null ? Section : EXE, Key, "", RetVal, 255, Path);
            return RetVal.ToString();
        }

        public void Write(string Key, string Value, string Section = null)
        {
            WritePrivateProfileString(Section != null ? Section : EXE, Key, Value, Path);
        }

        public void DeleteKey(string Key, string Section = null)
        {
            Write(Key, null, Section != null ? Section : EXE);
        }

        public void DeleteSection(string Section = null)
        {
            Write(null, null, Section != null ? Section : EXE);
        }

        public bool KeyExists(string Key, string Section = null)
        {
            return Read(Key, Section).Length > 0 ? true : false;
        }

        // The Function called to obtain the SectionHeaders,
        // and returns them in an Dynamic Array.
        public string[] GetSectionNames()
        {
            //    Sets the maxsize buffer to 500, if the more
            //    is required then doubles the size each time.
            for (int maxsize = 500; true; maxsize *= 2)
            {
                //    Obtains the information in bytes and stores
                //    them in the maxsize buffer (Bytes array)
                byte[] bytes = new byte[maxsize];
                int size = GetPrivateProfileString(0, "", "", bytes, maxsize, Path);

                // Check the information obtained is not bigger
                // than the allocated maxsize buffer - 2 bytes.
                // if it is, then skip over the next section
                // so that the maxsize buffer can be doubled.
                if (size < maxsize - 2)
                {
                    // Converts the bytes value into an ASCII char. This is one long string.
                    string Selected = Encoding.ASCII.GetString(bytes, 0,
                                               size - (size > 0 ? 1 : 0));
                    // Splits the Long string into an array based on the "\0"
                    // or null (Newline) value and returns the value(s) in an array
                    return Selected.Split(new char[] { '\0' });
                }
            }
        }
        // The Function called to obtain the EntryKey's from the given
        // SectionHeader string passed and returns them in an Dynamic Array
        public string[] GetEntryNames(string section)
        {
            //    Sets the maxsize buffer to 500, if the more
            //    is required then doubles the size each time. 
            for (int maxsize = 500; true; maxsize *= 2)
            {
                //    Obtains the EntryKey information in bytes
                //    and stores them in the maxsize buffer (Bytes array).
                //    Note that the SectionHeader value has been passed.
                byte[] bytes = new byte[maxsize];
                int size = GetPrivateProfileString(section, 0, "", bytes, maxsize, Path);

                // Check the information obtained is not bigger
                // than the allocated maxsize buffer - 2 bytes.
                // if it is, then skip over the next section
                // so that the maxsize buffer can be doubled.
                if (size < maxsize - 2)
                {
                    // Converts the bytes value into an ASCII char.
                    // This is one long string.
                    string entries = Encoding.ASCII.GetString(bytes, 0,
                                              size - (size > 0 ? 1 : 0));
                    // Splits the Long string into an array based on the "\0"
                    // or null (Newline) value and returns the value(s) in an array
                    return entries.Split(new char[] { '\0' });
                }
            }
        }

        public string GetSectionContent(string sectionName)
        {
            try
            {
                if(string.IsNullOrEmpty(_content))
                    _content = File.ReadAllText(Path, Encoding.GetEncoding("iso-8859-1"));

                if (sectionName.ToLower().Equals("comments"))
                {
                    int pos = _content.IndexOf("[Common]", 0, StringComparison.CurrentCultureIgnoreCase);
                    if (pos > 0)
                        return _content.Substring(0, pos - 1).Trim();
                    else
                    {
                        return "";
                    }
                }
                int start = _content.IndexOf("[" + sectionName + "]", 0, StringComparison.CurrentCultureIgnoreCase);
                if (start > -1)
                {
                    start = start + sectionName.Length + 2;
                    int end = _content.IndexOf("\r\n\r\n", start);
                    if(!sectionName.ToLower().Equals("resultscript"))
                        return _content.Substring(start, end - start + 1).Trim();
                    else
                    {
                        return _content.Substring(start).Trim();
                    }
                }
                else
                {
                    return "";
                }
            }
            catch (Exception)
            {
                return "";
            }

        }

    }
}