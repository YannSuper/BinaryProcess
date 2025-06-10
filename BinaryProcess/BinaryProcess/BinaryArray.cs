using System;
using System.IO;
using System.IO.Compression;

namespace BinaryProcess
{
    /// <summary>
    /// 处理数组
    /// </summary>
    public static class BinaryArray
    {
        private const byte Float = 0x01;
        private const byte Double = 0x02;
        private const byte Int = 0x03;
        private const int IntLength = 4;
        private const int FloatLength = 4;
        private const int DoubleLength = 8;

        private readonly static byte[] HEADER = new byte[] { 0x64, 0x61, 0x74, 0x61 };

        #region 保存数据

        /// <summary>
        /// 保存数据
        /// </summary>
        /// <param name="filepath"></param>
        /// <param name="datas"></param>
        public static void Save(string filepath, Array datas)
        {
            if (datas != null)
            {
                if (Verify(datas))
                {
                    //转字节
                    byte[] bytes = ToByte(datas);

                    File.WriteAllBytes(filepath, bytes);
                }
                else
                {
                    throw new FormatException("不支持的数组格式");
                }
            }
            else
            {
                throw new ArgumentNullException(nameof(datas), "参数不能为 null");
            }
        }

        #endregion

        #region 读取数据

        /// <summary>
        /// 读取数据
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns></returns>
        public static T Read<T>(string filepath)
        {
            if (File.Exists(filepath))
            {
                byte[] dataBytes = File.ReadAllBytes(filepath);

                return ToArray<T>(dataBytes);
            }
            else
            {
                return default(T);
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 判断是否相等
        /// </summary>
        /// <param name="array1"></param>
        /// <param name="array2"></param>
        /// <returns></returns>
        private static bool equal(byte[] array1, byte[] array2)
        {
            if (array1.Length != array2.Length)
                return false;

            for (int i = 0; i < array1.Length; i++)
            {
                if (array1[i] != array2[i])
                    return false;
            }
            return true;
        }

        private static bool Verify(Array datas)
        {

            Type elType = datas.GetType().GetElementType();

            if (!(elType == typeof(int) || elType == typeof(float) || elType == typeof(double)))
            {
                return false;
            }

            if (!(datas.GetType() == elType.MakeArrayType() || datas.GetType() == elType.MakeArrayType(2)))
            {
                return false;
            }

            return true;
        }
        #endregion

        #region 删除文件
        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="filepath"></param>
        public static bool Delete(string filepath)
        {
            if (File.Exists(filepath))
                File.Delete(filepath);
            return true;
        }
        #endregion

        #region 处理数组和字节之间的转换
        /// <summary>
        /// 数组转字节
        /// </summary>
        /// <param name="datas"></param>
        /// <returns></returns>
        public static byte[] ToByte(Array datas)
        {

            Type elType = datas.GetType().GetElementType();
            byte etype = 0x00;
            int byteLength = 0;
            if (elType == typeof(int))
            {
                etype = Int;
                byteLength = IntLength;
            }
            else if (elType == typeof(float))
            {
                etype = Float;
                byteLength = FloatLength;
            }
            else if (elType == typeof(double))
            {
                etype = Double;
                byteLength = DoubleLength;
            }
            int row = 1;
            int col = 0;
            if (datas.GetType() == elType.MakeArrayType())
            {
                row = 1;
                col = datas.Length;
            }
            else if (datas.GetType() == elType.MakeArrayType(2))
            {
                row = datas.GetLength(0);
                col = datas.GetLength(1);
            }

            int dataLength = datas.Length;
            //数据格式:header 类型 行 列 数据
            int allLenght = HEADER.Length + 1 + 4 + 4 + dataLength * byteLength;
            byte[] bytes = new byte[allLenght];
            //头
            Array.Copy(HEADER, bytes, HEADER.Length);
            bytes[HEADER.Length] = etype;
            byte[] rowBytes = BitConverter.GetBytes(row);
            byte[] colBytes = BitConverter.GetBytes(col);
            Array.Copy(rowBytes, 0, bytes, HEADER.Length + 1, rowBytes.Length);
            Array.Copy(colBytes, 0, bytes, HEADER.Length + 1 + rowBytes.Length, rowBytes.Length);
            int dStart = HEADER.Length + 1 + rowBytes.Length + rowBytes.Length;
            if (datas.GetType() == elType.MakeArrayType())
            {
                for (int j = 0; j < col; j++)
                {
                    byte[] dBytes = new byte[0];

                    if (etype == Int)
                    {
                        dBytes = BitConverter.GetBytes((int)datas.GetValue(j));
                    }
                    else if (etype == Float)
                    {
                        dBytes = BitConverter.GetBytes((float)datas.GetValue(j));
                    }
                    else if (etype == Double)
                    {
                        dBytes = BitConverter.GetBytes((double)datas.GetValue(j));
                    }

                    Array.Copy(dBytes, 0, bytes, dStart, dBytes.Length);
                    dStart += dBytes.Length;
                }
            }
            else if (datas.GetType() == elType.MakeArrayType(2))
            {
                for (int i = 0; i < row; i++)
                {
                    for (int j = 0; j < col; j++)
                    {
                        byte[] dBytes = new byte[0];
                        if (etype == Int)
                        {
                            dBytes = BitConverter.GetBytes((int)datas.GetValue(i, j));
                        }
                        else if (etype == Float)
                        {
                            dBytes = BitConverter.GetBytes((float)datas.GetValue(i, j));
                        }
                        else if (etype == Double)
                        {
                            dBytes = BitConverter.GetBytes((double)datas.GetValue(i, j));
                        }

                        Array.Copy(dBytes, 0, bytes, dStart, dBytes.Length);
                        dStart += dBytes.Length;
                    }
                }
            }

            //压缩数组 并返回
            return Compress(bytes);
        }

        /// <summary>
        /// 字节转数组
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataBytes"></param>
        /// <returns></returns>
        public static T ToArray<T>(byte[] dataBytes)
        {
            //解压缩
            dataBytes = Decompress(dataBytes);

            Type elType = typeof(T).GetElementType();
            byte etype = 0x00;
            int byteLength = 0;
            if (elType == typeof(int))
            {
                etype = Int;
                byteLength = IntLength;
            }
            else if (elType == typeof(float))
            {
                etype = Float;
                byteLength = FloatLength;
            }
            else if (elType == typeof(double))
            {
                etype = Double;
                byteLength = DoubleLength;
            }

            byte[] hbytes = new byte[HEADER.Length];
            //效验
            Array.Copy(dataBytes, hbytes, hbytes.Length);
            //验证格式
            if (!equal(hbytes, HEADER))
            {
                return default(T);
            }

            byte type = dataBytes[hbytes.Length];
            if (!(type == etype))
            {
                return default(T);
            }

            byte[] rowBytes = new byte[4];
            byte[] colBytes = new byte[4];
            Array.Copy(dataBytes, hbytes.Length + 1, rowBytes, 0, 4);
            Array.Copy(dataBytes, hbytes.Length + 1 + 4, colBytes, 0, 4);
            int row = BitConverter.ToInt32(rowBytes);
            int col = BitConverter.ToInt32(colBytes);
            int allLenght = HEADER.Length + 1 + 4 + 4 + row * col * byteLength;
            if (allLenght != dataBytes.Length)
            {
                return default(T);
            }
            if (typeof(T) == elType.MakeArrayType())
            {

                Array result = Array.CreateInstance(elType, col);
                int dStart = HEADER.Length + 1 + 4 + 4;

                for (int j = 0; j < col; j++)
                {
                    byte[] dBytes = new byte[byteLength];
                    Array.Copy(dataBytes, dStart, dBytes, 0, byteLength);
                    if (etype == Int)
                    {
                        result.SetValue(BitConverter.ToInt32(dBytes), j);
                    }
                    else if (etype == Float)
                    {
                        result.SetValue(BitConverter.ToSingle(dBytes), j);
                    }
                    else if (etype == Double)
                    {
                        result.SetValue(BitConverter.ToDouble(dBytes), j);
                    }

                    dStart += dBytes.Length;
                }
                return (T)(object)result;
            }
            else if (typeof(T) == elType.MakeArrayType(2))
            {
                Array result = Array.CreateInstance(elType, row, col);
                int dStart = HEADER.Length + 1 + 4 + 4;


                for (int i = 0; i < row; i++)
                {
                    for (int j = 0; j < col; j++)
                    {
                        byte[] dBytes = new byte[byteLength];
                        Array.Copy(dataBytes, dStart, dBytes, 0, byteLength);

                        if (etype == Int)
                        {
                            result.SetValue(BitConverter.ToInt32(dBytes), i, j);
                        }
                        else if (etype == Float)
                        {
                            result.SetValue(BitConverter.ToSingle(dBytes), i, j);
                        }
                        else if (etype == Double)
                        {
                            result.SetValue(BitConverter.ToDouble(dBytes), i, j);
                        }

                        dStart += dBytes.Length;
                    }
                }

                return (T)(object)result;
            }
            return default(T);
        }
        #endregion

        #region 字节压缩

        /// <summary>
        /// 字节压缩
        /// </summary>
        /// <param name="datas"></param>
        /// <returns></returns>
        private static byte[] Compress(byte[] datas)
        {
            using (var outputStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(outputStream, CompressionMode.Compress, true))
                {
                    gzipStream.Write(datas, 0, datas.Length);
                }
                return outputStream.ToArray();
            }
        }

        /// <summary>
        /// 字节解压缩
        /// </summary>
        /// <param name="datas"></param>
        /// <returns></returns>
        private static byte[] Decompress(byte[] datas)
        {
            using (var inputStream = new MemoryStream(datas))
            {
                using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
                {
                    using (var outputStream = new MemoryStream())
                    {
                        gzipStream.CopyTo(outputStream);
                        return outputStream.ToArray();
                    }
                }
            }
        }
        #endregion
    }
}
