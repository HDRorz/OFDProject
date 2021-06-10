using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OFDFile.IO
{
    public class OFDIndexFileWriter : IOBase
    {

        /// <summary>
        /// 创建索引文件，fileNames会排序
        /// </summary>
        /// <param name="fileVersion"></param>
        /// <param name="fileCreator"></param>
        /// <param name="fileReceiver"></param>
        /// <param name="date"></param>
        /// <param name="fileNames"></param>
        /// <returns></returns>
        public static byte[] CreateFile(string fileVersion, string fileCreator, string fileReceiver, DateTime date, List<string> fileNames)
        {
            using (var ms = new MemoryStream())
            {
                using (var writer = new StreamWriter(ms, GBEncoding))
                {
                    writer.AutoFlush = true;
                    writer.NewLine = NewLine;

                    writer.WriteLine("OFDCFIDX");
                    writer.WriteLine(fileVersion.PadRight(8));
                    writer.WriteLine(fileCreator.PadRight(20));
                    writer.WriteLine(fileReceiver.PadRight(20));
                    writer.WriteLine(date.ToString("yyyyMMdd").PadRight(8));
                    writer.WriteLine(fileNames.Count.ToString().PadLeft(8, '0'));

                    foreach (var fileName in fileNames.OrderBy(s => s))
                    {
                        writer.WriteLine(fileName);
                    }
                    writer.WriteLine("OFDCFEND");

                    ms.Seek(0, SeekOrigin.Begin);

                    var retArr = new byte[ms.Length];
                    ms.Read(retArr, 0, retArr.Length);
                    return retArr;
                }
            }
        }

    }
}
