using System;
using System.Net;
using System.Net.Sockets.Kcp;
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
    public abstract class KCPSession
    {
        protected uint m_sid;
        Action<byte[], IPEndPoint> m_udpSender;
        protected IPEndPoint m_remotePoint;
        protected SessionState m_sessionState=SessionState.None;

        public KCPHandle m_handle;
        public Kcp kcp;
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
            kcp = new Kcp(sid, m_handle);
            //常规设置
            kcp.NoDelay(1, 10, 2, 1);
            kcp.WndSize(64, 64);
            kcp.SetMtu(512);

            m_handle.Out=(Memory<byte> buffer)=>{
                byte[] bytes = buffer.ToArray();
                m_udpSender(bytes, m_remotePoint);
            };
        }
        public bool isConnected()
        {
            return m_sessionState == SessionState.Connected;
        }
    }

}
