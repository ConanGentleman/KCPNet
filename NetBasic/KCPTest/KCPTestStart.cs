using System;
using System.Text;
using System.Threading;

namespace KCPTest
{
    class KCPTestStart
    {
        static void Main(string[] args)
        {
            const uint conv = 123;

            KCPItem kcpServer = new KCPItem(conv, "server");
            KCPItem kcpClient = new KCPItem(conv, "client");

            //随机数模拟丢包
            Random rd = new Random();

            kcpServer.SetOutCallback((Memory<byte> buffer) =>
            {
                //Console.WriteLine($"Send Pkg Succ:{GetByteString(buffer.ToArray())}");
                //服务器发送从Kcp处理后的数据，则将数据放入客户端
                kcpClient.InputData(buffer.Span);
            });
            kcpClient.SetOutCallback((Memory<byte> buffer) =>
            {
                int next = rd.Next(100);
                if (next >= 90) //发送成功（没丢包） 才会将数据方法服务器
                {
                    Console.WriteLine($"Send Pkg Succ:{GetByteString(buffer.ToArray())}");
                    //客户端发送从Kcp处理后的数据，则将数据放入服务器
                    kcpServer.InputData(buffer.Span);
                }
                else
                {
                    //发送失败， 由于调用了kcpServer.Update();和kcpClient.Update();对数据进行驱动（检测），如果发送后，收取错误，那么底层就会启动重传
                    Console.WriteLine("Send Pkg Miss");
                }
            });
            byte[] data = Encoding.ASCII.GetBytes("midoli");
            //0.客户端发送信息
            kcpClient.SendMsg(data);

            while (true)
            {
                kcpServer.Update();
                kcpClient.Update();
                Thread.Sleep(10);
            }
        }
        static string GetByteString(byte[] bytes)
        {
            string str = "";
            for (int i = 0; i < bytes.Length; i++)
            {
                str += string.Format("\n      [{0}]:{1}", i, bytes[i]);
            }
            return str;
        }
    }
}
