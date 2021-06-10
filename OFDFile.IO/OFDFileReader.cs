using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OFDFile.IO
{
    public class OFDFileReader : IOBase
    {

        public OFDFileReader()
        {

        }

        public OFDFile ReadFile(string fileName)
        {
            var fileHeader = new OFDFileHeader();
            fileHeader.FileName = fileName;
            var fieldInfos = new List<OFDFieldInfo>();
            var datas = new List<byte[]>();
            var curFieldsDict = FieldInfoDict_V21;
            using (var sr = new StreamReader(File.OpenRead(fileName), GBEncoding))
            {

                #region 前9行文件头
                string OFDCFDAT = sr.ReadLine();
                fileHeader.FileVersion = sr.ReadLine().Trim();
                fileHeader.FileSender = sr.ReadLine().Trim();
                fileHeader.FileReceiver = sr.ReadLine().Trim();
                fileHeader.Date = sr.ReadLine().Trim();
                fileHeader.FileNo = sr.ReadLine().Trim();
                fileHeader.FileType = sr.ReadLine().Trim();
                fileHeader.DataSender = sr.ReadLine().Trim();
                fileHeader.DataReceiver = sr.ReadLine().Trim();
                #endregion
                int fileVersion = Convert.ToInt32(fileHeader.FileVersion);
                if (fileVersion >= 22)
                {
                    curFieldsDict = FieldInfoDict_V22;
                }
                var fieldCountStr = sr.ReadLine();
                int fieldCount = Convert.ToInt32(fieldCountStr);
                int rowByteCount = 0;
                //读取文件字段列表
                for (int i = 0; i < fieldCount; i++)
                {
                    string colName = sr.ReadLine().Trim().ToLower();
                    OFDFieldInfo fieldInfo;
                    if (curFieldsDict.TryGetValue(colName, out fieldInfo))
                    {
                        rowByteCount += fieldInfo.FieldSize;
                        fieldInfos.Add(fieldInfo);
                    }
                    else
                    {
                        throw new Exception(string.Format("{0}，文件字段{1}信息未配置", fileName, colName));
                    }
                }
                int rowCount = Convert.ToInt32(sr.ReadLine());
                //读取每行数据
                datas = new List<byte[]>(rowCount);
                for (int i = 0; i < rowCount; i++)
                {
                    var line = sr.ReadLine();
                    datas.Add(GBEncoding.GetBytes(line));
                }

                string OFDCFEND = sr.ReadLine().Trim();
                if (OFDCFEND != "OFDCFEND")
                {
                    throw new Exception(string.Format("{0}，文件结尾不为OFDCFEND", fileName));
                }
            }

            return new OFDFile(fileHeader, fieldInfos, datas);
        }

        public static object[] DeserilizeRowData(List<OFDFieldInfo> fieldProerties, byte[] content)
        {
            var dataArray = new object[fieldProerties.Count];
            int index = 0;
            for (int j = 0; j < fieldProerties.Count; j++)
            {
                var proerty = fieldProerties[j];
                byte[] tempBuffer;
                if (proerty.FieldType == "TEXT" || proerty.FieldSize == OFDFieldInfo.STRING_MAX_LENGTH)
                {
                    tempBuffer = new byte[content.Length - index];
                    Array.Copy(content, index, tempBuffer, 0, tempBuffer.Length);
                    dataArray[j] = GBEncoding.GetString(tempBuffer);
                    break;
                }
                if (proerty.FieldType == "N")
                {
                    int intSize = proerty.FieldSize - proerty.FieldSize2;
                    string value = GBEncoding.GetString(content, index, proerty.FieldSize).Insert(intSize, ".");
                    dataArray[j] = Convert.ToDecimal(value);
                }
                else
                {
                    string value = GBEncoding.GetString(content, index, proerty.FieldSize);
                    dataArray[j] = value.Trim();
                }
                index += proerty.FieldSize;
            }
            return dataArray;
        }
    }
}
