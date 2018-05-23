using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace TCPCopycat
{
    public class TaskTimer : Timer
    {
        public TCPCopycatPacket packetToSend;
        public TCPCopycatReceiveMessageCallback lambda;
        public int waitTime;

        public TaskTimer() : base()
        {
        }
    }
}

