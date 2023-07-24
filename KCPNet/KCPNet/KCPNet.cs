using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
/// <summary>
/// 基于Kcp封装，实现可靠UDP
/// </summary>
namespace PENet
{
    [Serializable]
    public abstract class KCPMsg { }

    public class KCPNet<T,K> where T:KCPSession<K>,new() where K:KCPMsg,new()
    {
        UdpClient udp;
        IPEndPoint remotePoint;
        private CancellationTokenSource cts;
        private CancellationToken ct;

        public KCPNet()
        {
            cts = new CancellationTokenSource();
            ct = cts.Token;
        }

        #region Server
        private Dictionary<uint, T> sessionDic = null;
        /// <summary>
        /// 服务器ip及端口
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        public void StartAsServer(string ip,int port)
        {
            sessionDic = new Dictionary<uint, T>();

            //服务器的端口需要确定
            udp = new UdpClient(new IPEndPoint(IPAddress.Parse(ip),port));
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)){//如果在window平台强行关闭客户端，服务端控制台回车发送消息时会报错，在这里进行处理
                udp.Client.IOControl((IOControlCode)(-1744830452), new byte[] { 0, 0, 0, 0 }, null);
            }
            remotePoint = new IPEndPoint(IPAddress.Parse(ip), port);
            KCPTool.ColorLog(KCPLogColor.Green, "Server Start....");

            //多线程数据接受 （放入线程池） ct用于关闭客户端时终止线程
            Task.Run(ServerReceive, ct);
        }
        async void ServerReceive()
        {
            UdpReceiveResult result;
            while (true)
            {
                try
                {
                    //判断线程池中正在运行的任务是否已经被取消掉OnClientSessionClose 不在接受数据
                    if (ct.IsCancellationRequested)
                    {
                        KCPTool.ColorLog(KCPLogColor.Cyan, "ServerReceive Task is Cancelled");
                        break;
                    }
                    //接收的数据
                    result = await udp.ReceiveAsync();

                    // 服务器任何Ip都能接受，进行连接。因此无需判断ip是否相等
                    //获得索引位置0的数据
                    uint sid = BitConverter.ToUInt32(result.Buffer, 0);
                    if (sid == 0)//客户端发起连接请求，此时服务器要生成一个唯一的ID，传回给客户端
                    {
                        sid = GenerateUniqueSessionID();
                        byte[] sid_bytes = BitConverter.GetBytes(sid);
                        //前面4个也是0，后面4个是sid
                        byte[] conv_bytes = new byte[8];
                        Array.Copy(sid_bytes, 0, conv_bytes, 4, 4);

                        //发送数据
                        SendUDPMsg(conv_bytes, result.RemoteEndPoint);

                    }
                    else//sid已经分配给了客户端，并且客户端已经完成相关的初始化
                    {
                        if (!sessionDic.TryGetValue(sid,out T session))
                        {
                            session = new T();
                            session.InitSession(sid,SendUDPMsg,result.RemoteEndPoint);
                            //不可能把客户端的所有连接一直保存在字典内，那么字典的数据量就会越来越大
                            session.OnSessionClose = OnServerSessionClose;
                            lock (sessionDic)
                            {
                                sessionDic.Add(sid, session);
                            }
                        }
                        else //获取了唯一sid后的第一次通信
                        {
                            session = sessionDic[sid];
                        }
                        session.ReceiveData(result.Buffer);
                    }
                }
                catch (Exception e)
                {
                    KCPTool.Warn("Server Udp Receive Data Exception:{0}", e.ToString());
                }
            }
        }

        void OnServerSessionClose(uint sid)
        {
            if (sessionDic.ContainsKey(sid))
            {
                lock (sessionDic)
                {
                    sessionDic.Remove(sid);
                    KCPTool.Warn("Session:{0} remove from sessionDic", sid);
                }
            }
            else
            {
                KCPTool.Error("session:{0} cannot find in sessionDic", sid);
            }
        }
        /// <summary>
        /// 服务器关闭
        /// </summary>
        public void CloseServer()
        {
            foreach(var item in sessionDic)
            {
                item.Value.CloseSession();
            }
            sessionDic = null;
            if (udp != null)
            {
                udp.Close();
                udp = null;
                //取消udp的循环接收
                cts.Cancel();
            }
        }
        #endregion

        #region Client
        public T clientSession;
        //作为客户端启动
        public void StartAsClient(string ip,int port)
        {
            //参数不传，或为0.那么发送数据时操作系统会自动分配一个端口来发送
            udp = new UdpClient(0);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)){//如果在window平台强行关闭客户端，服务端控制台回车发送消息时会报错，在这里进行处理
                udp.Client.IOControl((IOControlCode)(-1744830452), new byte[] { 0, 0, 0, 0 }, null);
            }
            remotePoint = new IPEndPoint(IPAddress.Parse(ip),port);
            KCPTool.ColorLog(KCPLogColor.Green,"Client Start....");

            //多线程数据接受 （放入线程池） ct用于关闭客户端时终止线程
            Task.Run(ClientReceive,ct);
        }

        /// <summary>
        /// 间隔时间
        /// </summary>
        /// <param name="interval"></param>
        /// <param name="maxintervalSum"></param>
        /// <returns></returns>
        public Task<bool> ConnectServer(int interval,int maxintervalSum=5000)
        {
            //在首次连接时，发送一个长度为4的字节，值全为0的消息，表明是初次连接，以便让服务器分配id
            //通过服务器接收到信息之后，也返回一个长度为4的字节，全为0的消息，并将id附在最后
            //之后每次发送信息都将id放在最前面进行发送
            SendUDPMsg(new byte[4], remotePoint);
            int checkTime = 0;
            Task<bool> task = Task.Run(async() => {
                while (true) {  //循环检测
                    await Task.Delay(interval);
                    checkTime += interval;
                    if(clientSession!=null && clientSession.isConnected())
                    {
                        return true;
                    }
                    else
                    {
                        //如果检测时间超过规定的时间，索命连接断开（心跳机制）
                        if (checkTime > maxintervalSum)
                        {
                            return false;
                        }
                    }
                }
            });

            return task;
        }
        async void ClientReceive()
        {
            UdpReceiveResult result;
            while (true)
            {
                try
                {
                    //判断线程池中正在运行的任务是否已经被取消掉OnClientSessionClose 不在接受数据
                    if (ct.IsCancellationRequested)
                    {
                        KCPTool.ColorLog(KCPLogColor.Cyan, "ClientReceive Task is Cancelled");
                        break;
                    }
                    //接收的数据
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
                                //初始化Session关闭事件
                                clientSession.OnSessionClose = OnClientSessionClose;
                            }
                        }
                        else
                        {
                            //说明已经建立了连接 因为KCP发送消息时，会把sid放在第一个
                            //处理业务逻辑数据
                            if(clientSession!=null && clientSession.isConnected())
                            {
                                clientSession.ReceiveData(result.Buffer);
                            }
                            else
                            {
                                //没有初始化，且sid!=0，数据消息提前到了，直接丢弃（会重传）
                                //直到初始化完成，Kcp重传再开始处理
                                KCPTool.Warn("Client is Initing...");
                            }
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
        void OnClientSessionClose(uint sid)
        {
            //把多线程中死循环的接收终止
            cts.Cancel();
            if (udp != null)
            {
                udp.Close();
                udp = null;
            }
            KCPTool.Warn("Client Session Close,sid:{0}",sid);
        }
        /// <summary>
        /// 关闭客户端
        /// </summary>
        public void CloseClient()
        {
            if (clientSession != null)
            {
                clientSession.CloseSession();
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
        /// <summary>
        /// 消息广播
        /// </summary>
        /// <param name="msg"></param>
        public void BroadCastMsg(K msg)
        {
            byte[] bytes = KCPTool.Serialize<K>(msg);
            foreach(var item in sessionDic)
            {
                item.Value.SendMsg(bytes);
            }
        }
        private uint sid = 0;
        /// <summary>
        /// 生成唯一的uid
        /// </summary>
        /// <returns></returns>
        public uint GenerateUniqueSessionID()
        {
            lock (sessionDic)//使用锁，保证多线程时，往里添加id，不会有重复的id出现
            {
                while (true)
                {
                    ++sid;
                    if (sid == uint.MaxValue)
                    {
                        sid = 1;
                    }
                    if (!sessionDic.ContainsKey(sid))
                    {
                        break;
                    }
                }
            }
            return sid;
        }
    }
}
