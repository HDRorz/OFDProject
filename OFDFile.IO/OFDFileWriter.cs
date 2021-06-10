using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace OFDFile.IO
{
    public class OFDFileWriter : IOBase
    {

        /// <summary>
        /// 从原始 object[] 生成文件
        /// </summary>
        /// <param name="header"></param>
        /// <param name="fieldNameList"></param>
        /// <param name="datas"></param>
        /// <returns></returns>
        public byte[] CreateFile(OFDFileHeader header, List<string> fieldNameList, IEnumerable<object[]> datas)
        {
            var curFieldsDict = FieldInfoDict_V21;
            var fieldInfos = new List<OFDFieldInfo>();
            using (var ms = new MemoryStream())
            {
                using (var writer = new StreamWriter(ms, GBEncoding))
                {
                    writer.AutoFlush = true;
                    writer.NewLine = NewLine;
                    int fileVersion = Convert.ToInt32(header.FileVersion);
                    if (fileVersion >= 22)
                    {
                        curFieldsDict = FieldInfoDict_V22;
                    }
                    foreach (var fieldName in fieldNameList)
                    {
                        OFDFieldInfo fieldInfo;
                        if (curFieldsDict.TryGetValue(fieldName.ToLower(), out fieldInfo))
                        {
                            fieldInfos.Add(fieldInfo);
                        }
                        else
                        {
                            throw new Exception(string.Format("{0}，文件字段{1}信息未配置", header.FileName, fieldName));
                        }
                    }

                    WriteFileHeader(writer, header, fieldNameList);
                    WriteFileDatas(writer, fieldInfos, datas);

                    ms.Seek(0, SeekOrigin.Begin);

                    var retArr = new byte[ms.Length];
                    ms.Read(retArr, 0, retArr.Length);
                    return retArr;
                }
            }
        }

        /// <summary>
        /// 从DataTable型数据源生成文件
        /// </summary>
        /// <param name="header"></param>
        /// <param name="dt"></param>
        /// <returns></returns>
        public byte[] CreateFile(OFDFileHeader header, DataTable dt)
        {
            List<string> colNames = new List<string>();
            foreach (DataColumn col in dt.Columns)
            {
                string colName = col.ColumnName;
                colNames.Add(colName);
            }
            return CreateFile(header, colNames, dt.Rows.OfType<DataRow>().Select(dr => dr.ItemArray));
        }

        /// <summary>
        /// 从对象型数据源生成文件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="header"></param>
        /// <param name="datas"></param>
        /// <returns></returns>
        public byte[] CreateFile<T>(OFDFileHeader header, IEnumerable<T> datas)
        {
            List<string> fieldNames = new List<string>();
            var propertyInfos = typeof(T).GetProperties(BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in propertyInfos)
            {
                string fieldName = prop.Name;
                fieldNames.Add(fieldName);
            }
            var rawArrayDatas = datas.Select(x =>
            {
                var objs = new object[propertyInfos.Length];
                for (int i = 0; i < propertyInfos.Length; i++)
                {
                    objs[i] = propertyInfos[i].GetValue(x, null);
                }
                return objs;
            });
            return CreateFile(header, fieldNames, rawArrayDatas);
        }

        /// <summary>
        /// 从对象型数据源生成文件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="header"></param>
        /// <param name="fieldNameList"></param>
        /// <param name="datas"></param>
        /// <returns></returns>
        public byte[] CreateFile<T>(OFDFileHeader header, List<string> fieldNameList, IEnumerable<T> datas)
        {
            List<PropertyInfo> propertyInfos = new List<PropertyInfo>();
            Type tType = typeof(T);
            foreach (var fieldName in fieldNameList)
            {
                var prop = tType.GetProperty(fieldName, BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop == null)
                {
                    throw new Exception($"fieldName：{fieldName}，无法在{tType.Name}中找到对应属性");
                }
                propertyInfos.Add(prop);
            }
            var rawArrayDatas = datas.Select(x =>
            {
                var objs = new object[propertyInfos.Count];
                for (int i = 0; i < propertyInfos.Count; i++)
                {
                    objs[i] = propertyInfos[i].GetValue(x, null);
                }
                return objs;
            });
            return CreateFile(header, fieldNameList, rawArrayDatas);
        }

        /// <summary>
        /// 从字典型数据源生成文件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="header"></param>
        /// <param name="fieldNameList"></param>
        /// <param name="datas"></param>
        /// <returns></returns>
        public byte[] CreateFile<T>(OFDFileHeader header, List<string> fieldNameList, IEnumerable<Dictionary<string, object>> datas)
        {
            var rawArrayDatas = datas.Select(x =>
            {
                var objs = new object[fieldNameList.Count];
                for (int i = 0; i < fieldNameList.Count; i++)
                {
                    x.TryGetValue(fieldNameList[i], out object tmp);
                    objs[i] = tmp;
                }
                return objs;
            });
            return CreateFile(header, fieldNameList, rawArrayDatas);
        }

        private void WriteFileHeader(StreamWriter writer, OFDFileHeader header, List<string> fieldNameList)
        {
            writer.WriteLine("OFDCFDAT");
            writer.WriteLine(header.FileVersion.PadRight(8));
            writer.WriteLine(header.FileSender.PadRight(20));
            writer.WriteLine(header.FileReceiver.PadRight(20));
            writer.WriteLine(header.Date.PadRight(8));
            writer.WriteLine(header.FileNo.PadRight(8));
            writer.WriteLine(header.FileType.PadRight(8));
            writer.WriteLine(header.DataSender.PadRight(8));
            writer.WriteLine(header.DataReceiver.PadRight(8));
            writer.WriteLine(fieldNameList.Count.ToString().PadLeft(8, '0'));
            foreach (var fieldName in fieldNameList)
            {
                writer.WriteLine(fieldName);
            }
        }

        private void WriteFileDatas(StreamWriter writer, List<OFDFieldInfo> fieldInfos, IEnumerable<object[]> datas)
        {
            var dataList = datas.ToList();
            writer.WriteLine(dataList.Count.ToString().PadLeft(16, '0'));
            int rowSize = fieldInfos.Sum(x => x.FieldSize);
            byte[] blankRow = Enumerable.Repeat((byte)32, rowSize).ToArray();
            byte[] rowTemp = new byte[rowSize];
            foreach (var data in dataList)
            {
                Array.Copy(blankRow, rowTemp, rowSize);
                int index = 0;
                for (int i = 0; i < fieldInfos.Count; i++)
                {
                    var fieldInfo = fieldInfos[i];
                    if (fieldInfo.FiledDataType == typeof(string))
                    {
                        //填字符串
                        string value;
                        if (data[i] == null)
                        {
                            value = "";
                        }
                        else
                        {
                            value = data[i].ToString();
                        }
                        if (!string.IsNullOrEmpty(value))
                        {
                            var bytes = GBEncoding.GetBytes(data[i].ToString());
                            Array.Copy(bytes, 0, rowTemp, index, Math.Min(bytes.Length, fieldInfo.FieldSize));
                        }
                    }
                    else
                    {
                        //数字长度大于18，会超过long的上限
                        //原数据非decimal时直接ToString
                        if (fieldInfo.FieldSize > 16 || !(data[i] is decimal))
                        {
                            var value = data[i].ToString().Replace(".", "").PadLeft(fieldInfo.FieldSize, '0');
                            var bytes = Encoding.ASCII.GetBytes(value);
                            Array.Copy(bytes, 0, rowTemp, index, bytes.Length);
                        }
                        else
                        {
                            //数字先转long
                            var tmpFieldData = (decimal)data[i];
                            for (int times = 0; times < fieldInfo.FieldSize2; times++)
                            {
                                tmpFieldData = tmpFieldData * 10;
                            }
                            var longData = decimal.ToInt64(tmpFieldData);
                            var value = longData.ToString().PadLeft(fieldInfo.FieldSize, '0');
                            var bytes = Encoding.ASCII.GetBytes(value);
                            Array.Copy(bytes, 0, rowTemp, index, Math.Min(bytes.Length, fieldInfo.FieldSize));
                        }
                    }
                    index += fieldInfo.FieldSize;
                }
                writer.BaseStream.Write(rowTemp, 0, rowTemp.Length);
                writer.WriteLine();
            }
            writer.WriteLine("OFDCFEND");
        }

    }
}
