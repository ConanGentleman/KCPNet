using KCPExmapleProtocol;
using PENet;
using System;

namespace KCPExampleServer
{
    /// <summary>
    /// .net core 控制台服务端
    /// </summary>
    public class ServerStart
    {
        static void Main(string[] args)
        {
            string ip = "113.54.218.219";
            KCPNet<ServerSession, NetMsg> server = new KCPNet<ServerSession, NetMsg>();
            server.StartAsServer(ip, 17666);

            while (true)
            {
                string ipt = Console.ReadLine();
                if (ipt == "quit")
                {
                    server.CloseServer();
                    break;
                }
                else
                {
                    server.BroadCastMsg(new NetMsg { info = ipt });
                }
            }

            Console.ReadKey();
        }
    }
}
