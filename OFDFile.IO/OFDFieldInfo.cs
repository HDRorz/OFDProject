using System;
using System.Collections.Generic;
using System.Text;

namespace OFDFile.IO
{
    public class OFDFieldInfo
    {
        public static int STRING_MAX_LENGTH = 4000;

        public string FieldName { get; set; }

        public string FieldType { get; set; }

        public int FieldSize { get; set; }

        public int FieldSize2 { get; set; }

        public Type FiledDataType { get; set; }

        public string FieldDesc { get; set; }

        public OFDFieldInfo(string name, string fieldType, string fieldSize, string fieldSize2, string fieldDesc = "")
        {
            FieldName = name;
            FieldType = fieldType;
            if (fieldSize == "TEXT")
            {
                FieldSize = STRING_MAX_LENGTH;
            }
            else
            {
                FieldSize = Convert.ToInt32(fieldSize);
            }
            if (string.IsNullOrEmpty(fieldSize2))
            {
                FieldSize2 = 0;
            }
            else
            {
                FieldSize2 = Convert.ToInt32(fieldSize2);
            }
            FiledDataType = GetDataType();
            FieldDesc = fieldDesc;
        }

        private Type GetDataType()
        {
            switch (FieldType)
            {
                case "N":
                    return typeof(decimal);
                case "A":
                case "C":
                case "TEXT":
                default:
                    return typeof(string);
            }
        }


    }
}
