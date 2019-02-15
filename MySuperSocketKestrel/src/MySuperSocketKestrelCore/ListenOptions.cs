using System;

namespace MySuperSocketKestrelCore
{
    public class ListenOptions
    {
        public string Ip { get; set; }

        public int Port  { get; set; }

        public int BackLog { get; set; }

        public bool NoDelay { get; set; }


        public Int32 MaxRecvPacketSize { get; set; }

        public Int32 MaxReceivBufferSize { get; set; }

        public Int32 MaxSendPacketSize { get; set; }

        //TODO Ƚ���� ����  �ؾ� �ϳ� ???
        public Int32 MaxSendingSize { get; set; }

        public Int32 MaxSendReTryCount { get; set; }
    }
}