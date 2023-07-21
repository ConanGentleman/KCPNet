using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server
{
    class ServerStart
    {
        //服务器和客户端 端口号需一致
        private const int port = 17332;
        static void Main(string[] args)
        {
            //CreateTCPServer();
            CreateUDPServer();
            //防止控制台自动终止
            Console.ReadKey();
        }
        //UDP 不进行连接  服务器用udpListener接受来自所有IP的信息
        static void CreateUDPServer()
        {
            //没有UdpListener
            UdpClient udpListener = new UdpClient(port);
            //将网络终结点表示为 IP 地址和端口号
            /*
             IPEndPoint类包含应用程序连接到主机上的服务所需的主机和本地或远程端口信息。
             通过将主机的 IP 地址和服务端口号组合在一起，类 IPEndPoint 会形成到服务的连接点。
             */
            //获取IP （谁发来的）
            IPEndPoint remoteIP = new IPEndPoint(IPAddress.Any, port);

            try
            {
                while (true)
                {
                    Console.WriteLine("Waiting for message...");
                    //获取对应IP的数据 
                    byte[] bytes = udpListener.Receive(ref remoteIP);
                    Console.WriteLine($"Recive msg form {remoteIP}:");
                    Console.WriteLine($"{Encoding.ASCII.GetString(bytes, 0, bytes.Length)}");
                }
            }
            catch(SocketException e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                udpListener.Close();
            }
        }
        //TCP 进行连接  对于每个客户端可以进行独立的操作
        static void CreateTCPServer()
        {
            //参数意义：接受所有IP的连接，端口
            TcpListener tcpListener = new TcpListener(IPAddress.Any, port);
            tcpListener.Start();

            Console.WriteLine("Waiting for connection...");

            while (true)
            {
                //循环监听 接受客户端TCP的连接 之后服务器和客户端通信就使用client
                TcpClient client = tcpListener.AcceptTcpClient();
                Console.WriteLine("connection accepted.");
                // 获取网络流 用以获取或发送数据
                NetworkStream ns = client.GetStream();

                // 连接后给服务器返回数据消息，以二进制的形式
                byte[] data = Encoding.ASCII.GetBytes("is connected");

                try
                {
                    //将数据写入网络流传入客户端  数据，偏移量，数据长度 
                    ns.Write(data, 0, data.Length);
                    ns.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }
    }
}
