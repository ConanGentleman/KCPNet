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
    /// .net framework 客户端Session连接
    /// </summary>
    public class ClientSession : KCPSession<NetMsg>
    {
        protected override void OnConnected()
        {
        }

        protected override void OnDisConnected()
        {
        }

        protected override void OnReceiveMsg(NetMsg msg)
        {
            KCPTool.ColorLog(KCPLogColor.Red, "Sid:{0},RevServer:{1}", m_sid,msg.info);
        }

        protected override void OnUpdate(DateTime now)
        {
        }
    }
}
