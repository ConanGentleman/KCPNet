using PENet;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
/// <summary>
/// 基于Kcp封装，实现可靠UDP
/// </summary>
namespace KCPNet
{
    public class KCPNet<T> where T:KCPSession,new()
    {
        UdpClient udp;
        IPEndPoint remotePoint;

        #region Client
        public T clientSession;
        //作为客户端启动
        public void StartAsClient(string ip,int port)
        {
            //参数不传，或为0.那么发送数据时操作系统会自动分配一个端口来发送
            udp = new UdpClient(0);
            remotePoint = new IPEndPoint(IPAddress.Parse(ip),port);
            KCPTool.ColorLog(KCPLogColor.Green,"Client Start....");
        }

        public void ConnectServer()
        {
            //在首次连接时，发送一个长度为4的字节，值全为0的消息，表明是初次连接，以便让服务器分配id
            //通过服务器接收到信息之后，也返回一个长度为4的字节，全为0的消息，并将id附在最后
            //之后每次发送信息都将id放在最前面进行发送
            SendUDPMsg(new byte[4], remotePoint);
        }
        async void ClientReceive()
        {
            UdpReceiveResult result;
            while (true)
            {
                try
                {
                    result = await udp.ReceiveAsync();

                    // 只接收目标主机的消息（这里是服务器）
                    if (Equals(remotePoint, result.RemoteEndPoint))//ip是否想等
                    {
                        //获得索引位置0的数据
                        uint sid = BitConverter.ToUInt32(result.Buffer, 0);
                        if (sid == 0)
                        {
                            //sid数据，说明刚开始建立连接
                            //由于udp通信成功性不确定，因此客户端有可能发起多次连接，服务器也有可能回应多次
                            //因此这里我们定义，只要接收到了其中某次sid确定的数据后，就把该次作为第一次接受到的sid为准
                            //忽略后来的
                            if(clientSession!=null && clientSession.isConnected())
                            {
                                //已经建立连接，初始化完成了，收到了多余的sid，直接忽略;
                                KCPTool.Warn("Client is Init Done. Sid Surplus");
                            }
                            else
                            {
                                //未初始化，收到服务器分配的sid数据，初始化一个客户端的session
                                sid = BitConverter.ToUInt32(result.Buffer, 4);
                                KCPTool.ColorLog(KCPLogColor.Green, "UDP Request Conv Sid:{0}", sid);

                                //会话处理
                                clientSession = new T();
                                clientSession.InitSession(sid, SendUDPMsg,remotePoint);
                            }
                        }
                        else
                        {
                            //说明已经建立了连接 因为KCP发送消息时，会把sid放在第一个
                            //TODO： 处理业务逻辑数据
                        }
                    }
                    else
                    {
                        KCPTool.Warn("Client Udp Receive illegal target Data.");
                    }
                }
                catch(Exception e)
                {
                    KCPTool.Warn("Client Udp Receive Data Exception:{0}", e.ToString());
                }
            }
        }
        #endregion
        void SendUDPMsg(byte[] bytes,IPEndPoint remotePoint)
        {
            if (udp != null)
            {
                udp.SendAsync(bytes, bytes.Length, remotePoint);
            }
        }
    }
}
