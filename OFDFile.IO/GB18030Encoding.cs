using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace OFDFile.IO
{
    public static partial class GB18030Encoding
    {

        static char[] Map2Byte2Unicode = new char[65536];
        static char[] Map4Byte2Unicode = new char[2097152];
        static ThreadLocal<StringBuilder> LocalStringBuilder = new ThreadLocal<StringBuilder>(() => new StringBuilder(4000));
        static Encoding SystemEncoding = Encoding.GetEncoding("GB18030");

        static GB18030Encoding()
        {
            for (int i = 0; i < OriginMap.Length; i++)
            {
                var unicode = OriginMap[i];
                i++;
                var gb18030code = OriginMap[i];

                if (gb18030code <= ushort.MaxValue)
                {
                    Map2Byte2Unicode[gb18030code] = (char)unicode;
                }
                else
                {
                    /* 
                     * byte0 81-FE:126
                     * byte1 30-39:10
                     * byte2 81-FE:126
                     * byte3 30-39:10
                     * 4字节压缩3字节
                    */
                    uint b0 = gb18030code >> 24 & 0xFF;
                    uint b1 = gb18030code >> 16 & 0xFF;
                    uint b2 = gb18030code >> 8 & 0xFF;
                    uint b3 = gb18030code & 0xFF;

                    #region 压缩 
                    uint newCode = ((b0 - 0x80) << 16) | ((b1 - 0x30) << 12) | ((b2 - 0x80) << 4) | (b3 - 0x30);

                    uint c0 = (newCode >> 16) & 0xFF;
                    uint c1 = (newCode >> 12) & 0xF;
                    uint c2 = (newCode >> 4) & 0xFF;
                    uint c3 = newCode & 0xF;
                    uint originCode = ((c0 + 0x80) << 24) | ((c1 + 0x30) << 16) | ((c2 + 0x80) << 8) | (c3 + 0x30);
                    #endregion

                    Map4Byte2Unicode[newCode] = (char)unicode;
                }
            }
        }

        public static char[] Decode2CharArray(byte[] content)
        {
            return Decode2CharArray(content, 0, content.Length);
        }

        public static string Decode(byte[] content)
        {
            return Decode(content, 0, content.Length);
        }

        public static char[] Decode2CharArray(byte[] content, int index, int length)
        {
            char[] buf = new char[length];
            int charLen = 0;
            uint c = 0;
            for (int i = 0; i < length; i++)
            {
                byte b0 = content[index + i];
                if (c == 0 && b0 <= 128)
                {
                    c = b0;
                    buf[charLen] = (char)c;
                    charLen++;
                    c = 0;
                    continue;
                }

                i++;
                byte b1 = content[index + i];
                c = ((uint)b0 << 8 | b1);
                if (b0 > 128 && b0 <= 254)
                {
                    //双字节
                    if ((b1 >= 64 && b1 <= 126)
                        || (b1 >= 128 && b1 <= 254))
                    {
                        c = GBcode2Unicode(c);
                        buf[charLen] = (char)c;
                        charLen++;
                        c = 0;
                        continue;
                    }

                    // 四字节
                    if (b1 >= 48 && b1 <= 57)
                    {
                        i++;
                        byte b2 = content[index + i];
                        if (b2 >= 129 && b2 <= 254)
                        {
                            i++;
                            byte b3 = content[index + i];
                            if (b3 >= 48 && b3 <= 57)
                            {
                                c = (char)((uint)c << 16 | (uint)b2 << 8 | b3);
                                c = GBcode2Unicode(b0, b1, b2, b3);
                                buf[charLen] = (char)c;
                                charLen++;
                                c = 0;
                                continue;
                            }
                        }
                    }
                }

                if (c != 0)
                {
                    if (c <= ushort.MaxValue)
                    {
                        buf[charLen] = (char)c;
                        charLen++;
                    }
                    else
                    {
                        buf[charLen] = (char)(c >> 16);
                        charLen++;
                        buf[charLen] = (char)(c & 0xFFFF);
                        charLen++;
                    }
                    c = 0;
                }
            }
            var retArr = new char[charLen];
            Array.Copy(buf, retArr, charLen);
            return retArr;
        }

        public static string Decode(byte[] content, int index, int length)
        {
            uint c = 0;
            var sb = LocalStringBuilder.Value;
            lock (sb)
            {
                sb.Clear();
                for (int i = 0; i < length; i++)
                {
                    byte b0 = content[index + i];
                    if (c == 0 && b0 <= 128)
                    {
                        c = b0;
                        sb.Append((char)c);
                        c = 0;
                        continue;
                    }

                    i++;
                    byte b1 = content[index + i];
                    c = ((uint)b0 << 8 | b1);
                    if (b0 > 128 && b0 <= 254)
                    {
                        //双字节
                        if ((b1 >= 64 && b1 <= 126)
                            || (b1 >= 128 && b1 <= 254))
                        {
                            c = GBcode2Unicode(c);
                            sb.Append((char)c);
                            c = 0;
                            continue;
                        }

                        // 四字节
                        if (b1 >= 48 && b1 <= 57)
                        {
                            i++;
                            byte b2 = content[index + i];
                            if (b2 >= 129 && b2 <= 254)
                            {
                                i++;
                                byte b3 = content[index + i];
                                if (b3 >= 48 && b3 <= 57)
                                {
                                    c = (char)((uint)c << 16 | (uint)b2 << 8 | b3);
                                    c = GBcode2Unicode(b0, b1, b2, b3);
                                    sb.Append((char)c);
                                    c = 0;
                                    continue;
                                }
                            }
                        }
                    }

                    if (c != 0)
                    {
                        if (c <= ushort.MaxValue)
                        {
                            sb.Append((char)c);
                        }
                        else
                        {
                            sb.Append((char)(c >> 16));
                            sb.Append((char)(c & 0xFFFF));
                        }
                        c = 0;
                    }
                }
                return sb.ToString();
            }
        }

        public static char GBcode2Unicode(uint code)
        {
            if (code <= ushort.MaxValue)
            {
                return Map2Byte2Unicode[code];
            }
            else
            {
                uint b0 = code >> 24 & 0xFF;
                uint b1 = code >> 16 & 0xFF;
                uint b2 = code >> 8 & 0xFF;
                uint b3 = code & 0xFF;

                return GBcode2Unicode((byte)b0, (byte)b1, (byte)b2, (byte)b3);
            }
        }

        private static char GBcode2Unicode(byte b0, byte b1, byte b2, byte b3)
        {
            long newCode = ((b0 - 0x80) << 16) | ((b1 - 0x30) << 12) | ((b2 - 0x80) << 4) | (b3 - 0x30);
            var ret = Map4Byte2Unicode[newCode];
            if (ret == 0)
            {
                try
                {
                    ret = SystemEncoding.GetChars(new byte[] { b0, b1, b2, b3 })[0];
                }
                finally
                {
                    ret = (char)((b2 << 8) | b3);
                }
            }
            return ret;
        }

    }
}
