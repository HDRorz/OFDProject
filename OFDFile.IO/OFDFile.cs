using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace OFDFile.IO
{
    public class OFDFile
    {
        /// <summary>
        /// 文件头信息
        /// </summary>
        public OFDFileHeader FileHeader { get; private set; }

        /// <summary>
        ///  文件字段个数
        /// </summary>
        public int FieldCount { get; private set; }

        /// <summary>
        /// 文件字段
        /// </summary>
        public List<OFDFieldInfo> FieldInfos { get; private set; }

        private Dictionary<string, int> FieldIndexDict = new Dictionary<string, int>();

        /// <summary>
        /// 文件数据行数
        /// </summary>
        public int RowCount { get; private set; }

        /// <summary>
        /// 文件原始数据（字节）
        /// </summary>
        public List<byte[]> RawDatas { get; private set; }

        private Lazy<List<object[]>> LazyDatas { get; set; }

        /// <summary>
        /// 文件数据
        /// </summary>
        public List<object[]> Datas { get { return LazyDatas.Value; } }

        /// <summary>
        /// 设置一个委托，可以替换每行数据的读取方法，默认为OFDFileReader.DeserilizeRowData
        /// 最快是OFDFileFastReader.DeserilizeRowData2
        /// </summary>
        public Func<List<OFDFieldInfo>, byte[], object[]> DeserilizeRowDataFunc = OFDFileReader.DeserilizeRowData;

        public OFDFile(OFDFileHeader header, List<OFDFieldInfo> fieldInfos, List<byte[]> datas)
        {
            FileHeader = header;
            FieldInfos = fieldInfos;
            FieldCount = fieldInfos.Count;
            RawDatas = datas;
            RowCount = datas.Count;

            LazyDatas = new Lazy<List<object[]>>(() =>
            {
                var retList = new List<object[]>();
                int i = 0;
                try
                {
                    for (i = 0; i < RawDatas.Count; i++)
                    {
                        retList.Add(DeserilizeRowDataFunc(FieldInfos, RawDatas[i]));
                    }
                    return retList;
                }
                catch (Exception e)
                {
                    int rownum = i + 10 + FieldCount + 1 + 1;
                    throw new Exception("第" + rownum + "行异常，" + e.Message, e);
                }
            }, LazyThreadSafetyMode.ExecutionAndPublication);

            for (int i = 0; i < FieldCount; i++)
            {
                var fi = FieldInfos[i];
                FieldIndexDict[fi.FieldName] = i;
                FieldIndexDict[fi.FieldName.ToLower()] = i;
                FieldIndexDict[fi.FieldName.ToUpper()] = i;
            }
        }

        /// <summary>
        /// 获取某个字段的顺序
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public int GetFieldIndex(string fieldName)
        {
            int index;
            if (FieldIndexDict.TryGetValue(fieldName, out index))
            {
                return index;
            }
            else
            {
                if (FieldIndexDict.TryGetValue(fieldName.ToLower(), out index))
                {
                    return index;
                }
                else
                {
                    throw new ArgumentOutOfRangeException("fieldName", fieldName, "文件不包含该字段");
                }
            }
        }

        /// <summary>
        /// 获取一行中有个字段值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="row"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public T GetFieldData<T>(object[] row, string fieldName)
        {
            try
            {
                int index = GetFieldIndex(fieldName);
                return (T)row[index];
            }
            catch (Exception)
            {
                if (typeof(T) == typeof(string))
                {
                    object ret = string.Empty;
                    return (T)(object)(string.Empty);
                }
                if (typeof(T) == typeof(decimal))
                {
                    return (T)(object)0m;
                }
                return (T)(object)null;
            }
        }

        /// <summary>
        /// 将一行转成字典
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public Dictionary<string, object> GetOneRow(object[] row)
        {
            var retDict = new Dictionary<string, object>();
            for (int i = 0; i < FieldCount; i++)
            {
                retDict[FieldInfos[i].FieldName] = row[i];
            }
            return retDict;
        }
    }
}
