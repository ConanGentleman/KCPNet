using KCPExmapleProtocol;
using PENet;
using System;
using System.Collections.Generic;
using System.Text;

namespace KCPExampleServer
{
    /// <summary>
    /// .net core 服务端session连接
    /// </summary>
    class ServerSession : KCPSession<NetMsg>
    {
        protected override void OnConnected()
        {
            KCPTool.ColorLog(KCPLogColor.Green, "Client OnLine. Sid:{0}",m_sid);
        }

        protected override void OnDisConnected()
        {
            KCPTool.ColorLog(KCPLogColor.Yellow, "Client OffLine. Sid:{0}", m_sid);
        }

        protected override void OnReceiveMsg(NetMsg msg)
        {
            KCPTool.ColorLog(KCPLogColor.Red, "Sid:{0}.RevClient,CMD:{1} {2}", m_sid,msg.cmd.ToString(),msg.info);

            //如果服务器收到来自客户端的消息是心跳消息
            if (msg.cmd == CMD.NetPing)
            {
                if (msg.netPing.isOver)//如果isOver为true，关闭
                {
                    CloseSession();
                }
                else//如果为false 则服务器需要回应一条心跳消息
                {
                    //如果收到了ping请求，则重置检测计数，并回复ping消息到客户端
                    checkCounter = 0;
                    NetMsg pingMsg = new NetMsg
                    {
                        cmd = CMD.NetPing,
                        netPing = new NetPing
                        {
                            isOver = false
                        }
                    };
                    SendMsg(pingMsg);
                }
            }
        }


        private int checkCounter=0;
        //每隔5秒
        DateTime checkTime = DateTime.UtcNow.AddSeconds(5);//表示服务器在启动后5秒开始检测
        protected override void OnUpdate(DateTime now)
        {
            if (now > checkTime)//当前时间与检测时间做对比
            {
                checkTime = now.AddSeconds(5);
                checkCounter++;
                if (checkCounter > 3)//连续3次都未收到发送过来的消息进行网络ping，那么就说明这次连接断开，清理掉数据
                {
                    NetMsg pingMsg = new NetMsg
                    {
                        cmd = CMD.NetPing,
                        netPing = new NetPing { isOver = true }
                    };
                    OnReceiveMsg(pingMsg);//3次超时以后，就模拟发送本地的关闭消息
                }
            }
        }
    }
}
