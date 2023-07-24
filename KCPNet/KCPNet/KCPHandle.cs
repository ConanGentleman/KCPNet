using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net.Sockets.Kcp;
using System.Text;
/// <summary>
/// KCP数据处理器
/// </summary>
namespace PENet
{
    public class KCPHandle : IKcpCallback
    {
        public Action<Memory<byte>> Out;

        //KCP自己发送出去的数据
        public void Output(IMemoryOwner<byte> buffer, int avalidLength)
        {
            /*
             IMemoryOwner<T>类型通常是用于管理内存分配的接口。在该方法中，
            使用using语句确保在buffer对象不再需要时，正确释放它所分配的内存空间
             */
            using (buffer)
            {
                Out(buffer.Memory.Slice(0, avalidLength));
            }
            //throw new NotImplementedException();
        }

        public Action<byte[]> Recv;
        /// <summary>
        /// 处理KCP接收的数据
        /// </summary>
        /// <param name="buffer"></param>
        public void Receive(byte[] buffer)
        {
            Recv(buffer);
        }
    }
}
