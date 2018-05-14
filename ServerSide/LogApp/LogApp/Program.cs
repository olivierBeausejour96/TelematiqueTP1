using ServerApp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogApp
{
    class Program
    {
        private const int listenPort = 11001;
        static void Main(string[] args)
        {
            Console.WriteLine("LogApp");
            TCPCopyCatInterface instance = new TCPCopyCatInterface();
            instance.initializeListener(listenPort, false);
        }
    }
}
