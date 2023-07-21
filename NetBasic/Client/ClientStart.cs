using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Client
{
    class ClientStart
    {
        //服务器和客户端 端口号需一致
        private const int port = 17332;
        static void Main(string[] args)
        {
            //CreateTCPClient();
            //防止控制台自动终止
            CreateUDPClient();
            Console.ReadKey();
        }
        //UDP
        static void CreateUDPClient()
        {
            //没有指定端口号时 ，操作系统会自动分配一个端口号进行使用
            UdpClient client = new UdpClient();
            byte[] data = Encoding.ASCII.GetBytes("is connected.(UDP)");
            //客户端需要指定发送到服务器指定的端口和IP
            IPEndPoint remoteIP = new IPEndPoint(IPAddress.Parse("10.18.120.179"), port);
            //发送data数据到指定IP和端口，长度为data.length 
            client.Send(data, data.Length, remoteIP);
            Console.WriteLine("message send to remote address");
        }
        //TCP
        static void CreateTCPClient()
        {
            try
            {
                //在服务器上通过 cmd 命令ipconfig /all 查看 IPv4 地址 建立连接
                //TcpClient做了多层封装 而UdpClient只是一个类
                var client = new TcpClient("10.18.120.179",port);
                //拿到网络流
                NetworkStream ns = client.GetStream();
                byte[] data = new byte[1024];
                //读取数据  存到哪个变量，偏移量，读多少
                int len=ns.Read(data,0,data.Length);
                //发送来的是二进制 因此对数据进行转换 (数据，偏移量，长度）
                Console.WriteLine(Encoding.ASCII.GetString(data,0,len));
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
