using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OFDFile.IO
{
    public class IOBase
    {
        protected static Encoding GBEncoding;

        protected static string NewLine = "\r\n";

        protected Dictionary<string, OFDFieldInfo> FieldInfoDict_V21;
        protected Dictionary<string, OFDFieldInfo> FieldInfoDict_V22;

        static IOBase()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            GBEncoding = Encoding.GetEncoding("GB18030");
        }

        internal IOBase()
        {
            Init();
        }

        private void Init()
        {
            string defaultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FieldInfo.txt");
            string v21Path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FieldInfo21.txt");
            string v22Path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FieldInfo22.txt");
            if (!File.Exists(v22Path))
            {
                FieldInfoDict_V21 = InitFieldConfig(defaultPath);
            }
            else
            {
                FieldInfoDict_V21 = InitFieldConfig(v21Path);
                FieldInfoDict_V22 = InitFieldConfig(v22Path);
            }
        }

        private Dictionary<string, OFDFieldInfo> InitFieldConfig(string fileName)
        {
            var dict = new Dictionary<string, OFDFieldInfo>();
            var contentLines = File.ReadAllLines(fileName, GBEncoding);
            foreach (var line in contentLines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }
                var items = line.Split(',');
                dict.Add(items[2].ToLower(), new OFDFieldInfo(items[2], items[3], items[4], items[5]));
            }

            return dict;
        }

    }
}
