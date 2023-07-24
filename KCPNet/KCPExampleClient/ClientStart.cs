using KCPExmapleProtocol;
using PENet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KCPExampleClient
{
    /// <summary>
    /// .net framework 控制台客户端
    /// </summary>
    public class ClientStart
    {
        static KCPNet<ClientSession, NetMsg> client;
        //心跳检测
        static Task<bool> checkTask=null;
        static void Main(string[] args)
        {
            string ip = "113.54.218.219";
            client = new KCPNet<ClientSession, NetMsg>();
            client.StartAsClient(ip,17666);
            //每隔200ms进行检测 超过5000
            checkTask=client.ConnectServer(200,5000);
            Task.Run(ConnectCheck);

            while (true)
            {
                string ipt = Console.ReadLine();//读取控制台的输入  控制关闭;
                if (ipt == "quit")
                {
                    client.CloseClient();
                    break;
                }
                else
                {
                    //发送消息
                    client.clientSession.SendMsg(new NetMsg
                    {
                        info = ipt
                    });
                }
            }
            Console.ReadKey();
        }
        /// <summary>
        /// 有多少次没有建立连接成功
        /// </summary>
        private static int counter = 0;
        static async void ConnectCheck()
        {
            while (true)
            {
                await Task.Delay(3000);
                if (checkTask != null && checkTask.IsCompleted)
                {
                    if (checkTask.Result)
                    {
                        //已经建立连接
                        KCPTool.ColorLog(KCPLogColor.Green, "ConnectServer Success.");
                        checkTask = null;
                        //发送心跳数据
                        await Task.Run(SendPingMsg);
                    }
                    else
                    {
                        ++counter;
                        if (counter > 4)//连接4次还没有连接上
                        {
                            KCPTool.Error(string.Format("Connect Failed {0} Time.Check Your Network Connection.", counter));
                            checkTask = null;
                            break;
                        }
                        else
                        {
                            KCPTool.Warn(string.Format("Connect Faild {0} Times.Retry...", counter));
                            //重新发起连接
                            checkTask = client.ConnectServer(200, 5000);
                        }
                    }
                }
            }
        }
        static async void SendPingMsg()
        {
            //心跳数据是一直都在发送 ，所以为循环
            while (true)
            {
                await Task.Delay(5000);
                if(client !=null && client.clientSession != null)
                {
                    client.clientSession.SendMsg(new NetMsg
                    {
                        cmd = CMD.NetPing,
                        netPing = new NetPing
                        {
                            isOver=false
                        }
                    });
                    KCPTool.ColorLog(KCPLogColor.Green, "Client Send Ping Message.");
                }
                else
                {
                    KCPTool.ColorLog(KCPLogColor.Green, "Ping task Cancel");
                    break;
                }
            }
        }
    }
}
