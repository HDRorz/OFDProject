using System;
using System.Collections.Generic;
using System.Text;

namespace OFDFile.IO
{
    public class OFDFileHeader
    {
        public string FileVersion { get; set; }

        public string FileSender { get; set; }

        public string FileReceiver { get; set; }

        public string Date { get; set; }

        public string FileNo { get; set; }

        public string FileType { get; set; }

        public string DataSender { get; set; }

        public string DataReceiver { get; set; }

        public string FileName { get; set; }
    }
}
