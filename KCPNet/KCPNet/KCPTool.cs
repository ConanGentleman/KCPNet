
using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

namespace PENet
{
    public enum KCPLogColor
    {
        None,
        Red,
        Green,
        Blue,
        Cyan,
        Magenta,
        Yellow
    }
    /// <summary>
    /// KCPNet网络库工具类 
    /// </summary>
    public class KCPTool
    {
        public static Action<string> LogFunc;
        public static Action<KCPLogColor,string> ColorLogFunc;
        public static Action<string> WarnFunc;
        public static Action<string> ErrorFunc;
        public static void Log(string msg,params object[] args)
        {
            msg = string.Format(msg, args);
            if (LogFunc != null)
            {
                LogFunc(msg);
            }
            else
            {
                ConsoleLog(msg, KCPLogColor.None);
            }
        }
        public static void ColorLog(KCPLogColor color,string msg, params object[] args)
        {
            msg = string.Format(msg, args);
            if (ColorLogFunc != null)
            {
                ColorLogFunc(color,msg);
            }
            else
            {
                ConsoleLog(msg, color);
            }
        }
        public static void Warn(string msg, params object[] args)
        {
            msg = string.Format(msg, args);
            if (WarnFunc != null)
            {
                WarnFunc(msg);
            }
            else
            {
                ConsoleLog(msg, KCPLogColor.Yellow);
            }
        }
        public static void Error(string msg, params object[] args)
        {
            msg = string.Format(msg, args);
            if (ErrorFunc != null)
            {
                ErrorFunc(msg);
            }
            else
            {
                ConsoleLog(msg, KCPLogColor.Red);
            }
        }

        private static void ConsoleLog(string msg,KCPLogColor color)
        {
            int threadID = Thread.CurrentThread.ManagedThreadId;
            msg = string.Format("Thread:{0} {1}", threadID, msg);

            switch (color)
            {
                case KCPLogColor.Red:
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine(msg);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case KCPLogColor.Green:
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(msg);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case KCPLogColor.Blue:
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine(msg);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case KCPLogColor.Cyan:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine(msg);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case KCPLogColor.Magenta:
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine(msg);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case KCPLogColor.Yellow:
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine(msg);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case KCPLogColor.None:
                    break;
                default:
                    break;
            }
        }
        /// <summary>
        /// 序列化：把T类实例 转成字节数组
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static byte[] Serialize<T>(T msg)where T:KCPMsg
        {
            using(MemoryStream ms=new MemoryStream())
            {
                try
                {
                    //BinaryFormatter 是一个序列化和反序列化对象图形的对象。序列化是将对象的某种形式（状态信息）对其进行编码，
                    ////以便可以存储在磁盘上，或者可以通过网络连接传输到任何接收它的地方。
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(ms, msg);//使用bf对象的Serialize方法将msg对象序列化到ms对象中。
                    ms.Seek(0, SeekOrigin.Begin);//ms对象的Seek方法将ms对象的当前位置设置为开头。
                    return ms.ToArray();
                }
                catch (SerializationException e)
                {
                    Error("Failed to serialize.Reason:{0}", e.ToString());
                    throw ;
                }
            }
        }
        /// <summary>
        /// 反序列化：将字节数组转换为T实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static T DeSerialize<T>(byte[] bytes) where T : KCPMsg
        {
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                try
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    T msg = (T)bf.Deserialize(ms);
                    return msg;
                }
                catch (SerializationException e)
                {
                    Error("Failed to Deserialize.Reason:{0} bytesLen:{1}", e.ToString(),bytes.Length);
                    throw;
                }
            }
        }
        /// <summary>
        /// 数据压缩
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static byte[] Compress(byte[] input)
        {
            using(MemoryStream outMS=new MemoryStream())
            {
                using(GZipStream gzs=new GZipStream(outMS, CompressionMode.Compress, true))//如果在释放 GZipStream 对象之后打开流，则为 true；否则为 false
                {
                    gzs.Write(input, 0, input.Length);
                    gzs.Close();
                    return outMS.ToArray();
                }
            }
        }
        /// <summary>
        /// 解压数据
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static byte[] DeCompress(byte[] input)
        {
            using (MemoryStream inputMS = new MemoryStream(input)){
                using (MemoryStream outMS = new MemoryStream()) {
                    using (GZipStream gzs = new GZipStream(inputMS, CompressionMode.Decompress))
                    {
                        byte[] bytes = new byte[1024];
                        int len = 0;
                        while ((len = gzs.Read(bytes, 0, bytes.Length)) > 0)
                        {
                            outMS.Write(bytes, 0, len);
                        }
                        gzs.Close();
                        return outMS.ToArray();
                    }
                }
            }
        }

        static readonly DateTime utcStart = new DateTime(1970, 1, 1);
        /// <summary>
        /// 获取毫秒数
        /// </summary>
        /// <returns></returns>
        public static ulong GetUTCStartMillsecond()
        {
            TimeSpan ts = DateTime.UtcNow - utcStart;
            return (ulong)ts.TotalMilliseconds;
        }
    }
}
