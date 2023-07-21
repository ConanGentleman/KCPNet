using System;
using System.Collections.Generic;
using System.Net.Sockets.Kcp;
using System.Text;

namespace KCPTest
{
    public class KCPItem
    {
        public string itemName;
        public KCPHandle handle;
        public Kcp kcp;
        /// <summary>
        /// id(conv)用于服务器跟客户端通信时，知道是哪个客户端发来的数据
        /// </summary>
        /// <param name="conv">客户端id(conv)</param>
        /// <param name="itemName"></param>
        public KCPItem(uint conv,string itemName)
        {
            handle = new KCPHandle();
            
            kcp = new Kcp(conv, handle);
            //常规设置
            kcp.NoDelay(1, 10, 2, 1);
            kcp.WndSize(64, 64);
            kcp.SetMtu(512);

            this.itemName = itemName;
        }
        //存放数据
        public void InputData(Span<byte> data)
        {
            //Span<T> 是C#语言中的一个结构体，其主要用于在内存安全的情况下进行内存操作。这在处理大量数据或需要在不创建副本的情况下操作数据时尤为重要。
            //使用 Span<T>，可以避免在.NET程序中创建不必要的数据副本，提高程序的效率。
            kcp.Input(data);
        }
        public void SetOutCallback(Action<Memory<byte>> itemSender)
        {
            handle.Out = itemSender;
        }
        //通过Kcp来发送数据
        public void SendMsg(byte[] data)
        {
            Console.WriteLine($"{itemName} 输入数据：{GetByteString(data)}");
            //kcp对发送的数据添加控制信息后再发送
            kcp.Send(data.AsSpan());
        }
        //不断对kcp中的数据进行检测
        public void Update()
        {
            kcp.Update(DateTime.UtcNow);
            //读数据
            int len;
            //PeekSize() 去队列里去检查有多少的消息
            while ((len = kcp.PeekSize()) > 0)
            {
                //取数据
                var buffer = new byte[len];
                if (kcp.Recv(buffer) >= 0)
                {
                    //收到的数据已经是经过kcp处理后的不带有控制信息的数据
                    Console.WriteLine($"{itemName} 收到数据：{GetByteString(buffer)}");
                }
            }
        }
        static string GetByteString(byte[] bytes)
        {
            string str = "";
            for(int i = 0; i < bytes.Length; i++)
            {
                str += string.Format("\n      [{0}]:{1}", i, bytes[i]);
            }
            return str;
        }
    }
}
