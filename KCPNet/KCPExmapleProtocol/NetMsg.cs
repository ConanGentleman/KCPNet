using PENet;
using System;

namespace KCPExmapleProtocol
{
    /// <summary>
    /// 网络通信数据协议
    /// </summary>
    [Serializable]
    public class NetMsg : KCPMsg
    {
        //通过cmd来区分消息类型
        public CMD cmd;

        //消息嵌套
        public NetPing netPing;

        public string info;

        ////消息嵌套
        //public ReqLogin reqLogin;
    }

    /// <summary>
    /// 用于心跳机制
    /// </summary>
    [Serializable]
    public class NetPing
    {
        // 是否结束连接
        public bool isOver;
    }


    //[Serializable]
    //public class ReqLogin
    //{
    //    public string acct;
    //    public string pass;
    //}

    public enum CMD
    {
        None,
        ReqLogin,
        NetPing
    }
}
