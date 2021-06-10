using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace OFDFile.IO
{
    public class OFDFileFastReader : IOBase
    {
        private static ThreadLocal<StringBuilder> LocalStringBuilder = new ThreadLocal<StringBuilder>(() => new StringBuilder(4000));

        private static byte B_Blank = 32;
        private static char C_Blank = (char)32;

        public OFDFileFastReader()
        {

        }

        public OFDFile ReadFile(string fileName)
        {
            var fileHeader = new OFDFileHeader();
            fileHeader.FileName = fileName;
            var fieldInfos = new List<OFDFieldInfo>();
            var datas = new List<byte[]>();
            var curFieldsDict = FieldInfoDict_V21;
            using (var fs = File.OpenRead(fileName))
            {
                var readBuffer = new byte[1024];
                int readBufferSize = fs.Read(readBuffer, 0, 1024);
                int postion = 0;
                int bufferPos = 0;

                #region 前9行文件头
                string OFDCFDAT = ReadLine(fs, ref readBuffer, ref readBufferSize, ref postion, ref bufferPos);
                fileHeader.FileVersion = ReadLine(fs, ref readBuffer, ref readBufferSize, ref postion, ref bufferPos).Trim();
                fileHeader.FileSender = ReadLine(fs, ref readBuffer, ref readBufferSize, ref postion, ref bufferPos).Trim();
                fileHeader.FileReceiver = ReadLine(fs, ref readBuffer, ref readBufferSize, ref postion, ref bufferPos).Trim();
                fileHeader.Date = ReadLine(fs, ref readBuffer, ref readBufferSize, ref postion, ref bufferPos).Trim();
                fileHeader.FileNo = ReadLine(fs, ref readBuffer, ref readBufferSize, ref postion, ref bufferPos).Trim();
                fileHeader.FileType = ReadLine(fs, ref readBuffer, ref readBufferSize, ref postion, ref bufferPos).Trim();
                fileHeader.DataSender = ReadLine(fs, ref readBuffer, ref readBufferSize, ref postion, ref bufferPos).Trim();
                fileHeader.DataReceiver = ReadLine(fs, ref readBuffer, ref readBufferSize, ref postion, ref bufferPos).Trim();
                #endregion
                int fileVersion = Convert.ToInt32(fileHeader.FileVersion);
                if (fileVersion >= 22)
                {
                    curFieldsDict = FieldInfoDict_V22;
                }
                var fieldCountStr = ReadLine(fs, ref readBuffer, ref readBufferSize, ref postion, ref bufferPos);
                int fieldCount = Convert.ToInt32(fieldCountStr);
                int rowByteCount = 0;
                //读取文件字段列表
                for (int i = 0; i < fieldCount; i++)
                {
                    string colName = ReadLine(fs, ref readBuffer, ref readBufferSize, ref postion, ref bufferPos).ToLower();
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
                int rowCount = Convert.ToInt32(ReadLine(fs, ref readBuffer, ref readBufferSize, ref postion, ref bufferPos));
                //读取每行数据
                datas = new List<byte[]>(rowCount);
                //重置流位置
                fs.Seek(postion, SeekOrigin.Begin);
                for (int i = 0; i < rowCount; i++)
                {
                    var lineBuffer = new byte[rowByteCount];
                    fs.Read(lineBuffer, 0, rowByteCount);
                    datas.Add(lineBuffer);
                    var rowEnd0 = fs.ReadByte();
                    var rowEnd1 = fs.ReadByte();
                    if (rowEnd0 != 13 || rowEnd1 != 10)
                    {
                        throw new Exception(string.Format("{0}，文件第{1}行长度不正确", fileName, 10 + fieldCount + 1 + i));
                    }
                }

                Array.Clear(readBuffer, 0, 1024);
                readBufferSize = fs.Read(readBuffer, 0, 1024);
                postion = 0;
                bufferPos = 0;
                string OFDCFEND = ReadLine(fs, ref readBuffer, ref readBufferSize, ref postion, ref bufferPos).Trim();
                if (OFDCFEND != "OFDCFEND")
                {
                    throw new Exception(string.Format("{0}，文件结尾不为OFDCFEND", fileName));
                }
            }

            return new OFDFile(fileHeader, fieldInfos, datas);
        }

        /// <summary>
        /// 从stream中读取一行，假设一行不超过1024字节
        /// </summary>
        /// <param name="bf"></param>
        /// <param name="readBuffer"></param>
        /// <param name="readBufferSize"></param>
        /// <param name="postion"></param>
        /// <param name="bufferPos"></param>
        /// <returns></returns>
        private static string ReadLine(Stream bf, ref byte[] readBuffer, ref int readBufferSize, ref int postion, ref int bufferPos)
        {
            int startP = bufferPos;
            int size = 0;
            while (true)
            {
                if (readBufferSize < 2)
                {
                    return GBEncoding.GetString(readBuffer, 0, readBufferSize);
                }
                //buffer尾，重置buffer
                if (bufferPos == readBufferSize - 2)
                {
                    var tempBuffer = new byte[1024];
                    int leftSize = 1024 - startP;
                    Array.Copy(readBuffer, startP, tempBuffer, 0, leftSize);
                    Array.Copy(tempBuffer, 0, readBuffer, 0, leftSize);
                    int readByteCount = bf.Read(readBuffer, leftSize, startP);
                    if (readByteCount == 0)
                    {
                        postion++;
                        bufferPos = 1024 - startP;
                        return GBEncoding.GetString(readBuffer, 0, readBufferSize - startP);
                    }
                    if (readByteCount != startP)
                    {
                        readBufferSize = readByteCount + leftSize;
                        Array.Clear(readBuffer, readBufferSize, 1024 - readBufferSize);
                    }
                    startP = 0;
                    bufferPos = leftSize - 2;
                    continue;
                }

                //换行
                if (readBuffer[bufferPos] == 13 && readBuffer[bufferPos + 1] == 10)
                {
                    string ret = GBEncoding.GetString(readBuffer, startP, size);

                    postion += 2;
                    bufferPos += 2;
                    return ret;
                }

                postion++;
                bufferPos++;
                size++;
            }
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
                    //数字长度大于18，会超过long的上限
                    if (proerty.FieldSize > 16)
                    {
                        int intSize = proerty.FieldSize - proerty.FieldSize2;
                        string value = GBEncoding.GetString(content, index, proerty.FieldSize).Insert(intSize, ".");
                        dataArray[j] = Convert.ToDecimal(value);
                    }
                    else
                    {
                        if (proerty.FieldSize2 == 0)
                        {
                            dataArray[j] = (decimal)Convert.ToInt64(FastByte2Long(content, index, proerty.FieldSize));
                        }
                        else
                        {
                            long tmpnum = Convert.ToInt64(FastByte2Long(content, index, proerty.FieldSize));
                            int low = (int)(tmpnum & uint.MaxValue);
                            int hi = (int)(tmpnum >> 32);
                            dataArray[j] = new decimal(low, hi, 0, false, (byte)proerty.FieldSize2);
                        }
                    }
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

        public static object[] DeserilizeRowData2(List<OFDFieldInfo> fieldProerties, byte[] content)
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
                    dataArray[j] = GBEncoding.GetString(tempBuffer).Trim();
                    break;
                }
                if (proerty.FieldType == "N")
                {
                    //数字长度大于18，会超过long的上限
                    if (proerty.FieldSize > 16)
                    {
                        int intSize = proerty.FieldSize - proerty.FieldSize2;
                        string value = FastReadASCII(content, index, proerty.FieldSize).Insert(intSize, ".");
                        dataArray[j] = Convert.ToDecimal(value);
                    }
                    else
                    {
                        if (proerty.FieldSize2 == 0)
                        {
                            dataArray[j] = (decimal)FastByte2Long(content, index, proerty.FieldSize);
                        }
                        else
                        {
                            dataArray[j] = FastByte2Decimal(content, index, proerty.FieldSize, proerty.FieldSize2);
                        }
                    }
                }
                else if (proerty.FieldType == "A")
                {
                    string value = FastReadASCII(content, index, proerty.FieldSize);
                    dataArray[j] = value.Trim();
                }
                else
                {
                    string value = FastReadGB18030(content, index, proerty.FieldSize).Trim();
                    dataArray[j] = value;
                }
                index += proerty.FieldSize;
            }
            return dataArray;
        }

        private static long FastByte2Long(byte[] content, int startIdx, int length)
        {
            long ret = 0;
            for (int i = 0; i < length; i++)
            {
                ret = ret * 10 + content[startIdx + i] - '0';
            }
            return ret;
        }

        private static decimal FastByte2Decimal(byte[] content, int startIdx, int length, int scale)
        {
            long tmpnum = FastByte2Long(content, startIdx, length);
            int low = (int)(tmpnum & uint.MaxValue);
            int hi = (int)(tmpnum >> 32);
            return new decimal(low, hi, 0, false, (byte)scale);
        }

        private static string FastReadASCII(byte[] content, int startIdx, int length)
        {
            return Encoding.ASCII.GetString(content, startIdx, length);
            if (content[startIdx] == B_Blank)
            {
                bool allBlank = true;
                for (int i = startIdx + 1; i < startIdx + length; i++)
                {
                    allBlank &= content[i] == B_Blank;
                }

                if (allBlank)
                {
                    return string.Empty;
                }
            }
            var sb = LocalStringBuilder.Value;
            lock (sb)
            {
                sb.Clear();
                for (int i = 0; i < length; i++)
                {
                    byte b = content[startIdx + i];
                    sb.Append((char)b);
                }
                return sb.ToString();
            }
        }

        private static string FastReadGB18030(byte[] content, int startIdx, int length)
        {
            if (content[startIdx] == B_Blank)
            {
                bool allBlank = true;
                for (int i = startIdx + 1; i < startIdx + length; i++)
                {
                    allBlank &= content[i] == B_Blank;
                }

                if (allBlank)
                {
                    return string.Empty;
                }
            }
            return GB18030Encoding.Decode(content, startIdx, length);
        }


    }
}
