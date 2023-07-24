using System;
using System.Net;
using System.Net.Sockets.Kcp;
using System.Threading;
using System.Threading.Tasks;
/// <summary>
/// 处理数据收发（客户端和服务器使用）
/// </summary>
namespace PENet
{
    public enum SessionState
    {
        None,
        Connected,
        Disconnected
    }
    public abstract class KCPSession<T> where T:KCPMsg,new()
    {
        protected uint m_sid;
        Action<byte[], IPEndPoint> m_udpSender;
        protected IPEndPoint m_remotePoint;
        protected SessionState m_sessionState=SessionState.None;
        public Action<uint> OnSessionClose;

        public KCPHandle m_handle;
        public Kcp m_kcp;
        private CancellationTokenSource cts;
        private CancellationToken ct;

        /// <summary>
        /// id(conv)用于服务器跟客户端通信时，知道是哪个客户端发来的数据
        /// </summary>
        /// <param name="conv">客户端id(conv)</param>
        /// <param name="itemName"></param>
        public void InitSession(uint sid,Action<byte[],IPEndPoint> udpSender,IPEndPoint remotePoint)
        {
            m_sid = sid;
            m_udpSender = udpSender;
            m_remotePoint = remotePoint;
            m_sessionState = SessionState.Connected;

            m_handle = new KCPHandle();
            m_kcp = new Kcp(sid, m_handle);
            //常规设置
            m_kcp.NoDelay(1, 10, 2, 1);
            m_kcp.WndSize(64, 64);
            m_kcp.SetMtu(512);

            m_handle.Out=(Memory<byte> buffer)=>{
                byte[] bytes = buffer.ToArray();
                m_udpSender(bytes, m_remotePoint);
            };
            //把KCP接收的消息解压缩反序列化
            m_handle.Recv = (byte[] buffer) =>{
                buffer = KCPTool.DeCompress(buffer);
                T msg = KCPTool.DeSerialize<T>(buffer);
                if (msg != null)
                {  
                    OnReceiveMsg(msg);
                }
            };

            OnConnected();

            cts = new CancellationTokenSource();
            ct = cts.Token;
            //加入线程池
            Task.Run(Update,ct);
        }
        /// <summary>
        /// 从Udp里接收到的数据
        /// </summary>
        public void ReceiveData(byte[] buffer)
        {
            //kcp处理收到的数据
            m_kcp.Input(buffer.AsSpan());
           
        }

        /// <summary>
        /// 从UDP发送数据
        /// </summary>
        public void SendMsg(T msg)
        {
            if (isConnected())
            {
                byte[] bytes = KCPTool.Serialize(msg);
                if (bytes != null)
                {
                    SendMsg(bytes);
                }
                else
                {
                    KCPTool.Warn("Session Disconnnected.Can not send msg");
                }
            }
        }
        /// <summary>
        /// 重载发送数据函数 发送二进制数据 优化性能
        /// </summary>
        /// <param name=""></param>
        public void SendMsg(byte[] msg_bytes)
        {
            if (isConnected())
            {
                msg_bytes = KCPTool.Compress(msg_bytes);
                m_kcp.Send(msg_bytes.AsSpan());
            }
            else
            {
                KCPTool.Warn("Session Disconnnected.Can not send msg");
            }
        }
        /// <summary>
        /// 关闭Session
        /// </summary>
        public void CloseSession()
        {
            cts.Cancel();//调用后 ct.IsCancellationRequested会变成true

            //回调到KCPNet中的ct中，来取消在多线程中的数据接收
            OnDisConnected();

            //如果OnSessionClose不为空 则调用
            OnSessionClose?.Invoke(m_sid);
            OnSessionClose = null;

            m_sessionState = SessionState.Disconnected;
            m_remotePoint = null;
            m_udpSender = null;
            m_sid = 0;

            m_handle = null;
            m_kcp = null;
            cts = null;
        }

        /// <summary>
        ///  驱动kcp获取真实的数据
        /// </summary>
        async void Update()
        {
            try
            {
                while (true)
                {
                    DateTime now = DateTime.UtcNow;
                    OnUpdate(now);
                    if (ct.IsCancellationRequested)
                    {
                        KCPTool.ColorLog(KCPLogColor.Cyan, "SessionUpdate Task is Cancelled");
                        break;
                    }
                    else
                    {
                        m_kcp.Update(now);
                        int len;
                        while ((len = m_kcp.PeekSize()) > 0)
                        {
                            var buffer = new byte[len];
                            if (m_kcp.Recv(buffer) >= 0)
                            {
                                //从KCP中接收数据
                                m_handle.Receive(buffer);
                            }
                            await Task.Delay(10);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                KCPTool.Warn("Session update Exception:{0}", e.ToString());
            }
        }

        protected abstract void OnConnected();
        protected abstract void OnUpdate(DateTime now);
        protected abstract void OnReceiveMsg(T msg);
        /// <summary>
        /// Session中断时 抛出函数 以便在子类覆盖
        /// </summary>
        protected abstract void OnDisConnected();

        public override bool Equals(object obj)
        {
            if (obj is KCPSession<T>)
            {
                KCPSession<T> us = obj as KCPSession<T>;
                return m_sid == us.m_sid;
            }
            else return false;
        }
        public override int GetHashCode()
        {
            return m_sid.GetHashCode();
        }
        public uint GetSessionID()
        {
            return m_sid;
        }
        public bool isConnected()
        {
            return m_sessionState == SessionState.Connected;
        }
    }

}
