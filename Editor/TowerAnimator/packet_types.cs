using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace serial_packet_protocolOld
{
    public static class packet_types
    {
        // important chars
        public const int ZERO_CHAR = 0x30;

        // the don't care 
        public const string DONT_CARE_STRING = "X";

        // the don't care
        public const char DONT_CARE_CHAR = 'X';

        // the null byte
        public const char NULL_CHAR = '\0';

        // start and stop
        public const char START_BYTE = '$';
        public const char STOP_BYTE = '%';

        // for checksum
        public const int CHECKSUM_BAD = 0;
        public const int CHECKSUM_OK = 1;

        // size of a word in bytes
        public const int WORD_SZ = 4;

        // for max packet size
        public const int MAX_TOTAL_PACKET_SZ = 256;

        // locations in the complete packet
        public const int PACKET_TYPE_A = 0;
        public const int PACKET_TYPE_B = 1;

        // storage for the payload
        public const int CHECK_SUM_A = 2;
        public const int CHECK_SUM_B = 3;
        public const int CHECK_SUM_C = 4;
        public const int CHECK_SUM_D = 5;

        // packet storage for the payload size
        public const int PAYLOAD_SIZE_A = 6;
        public const int PAYLOAD_SIZE_B = 7;
        public const int PAYLOAD_SIZE_C = 8;
        public const int PAYLOAD_SIZE_D = 9;

        // the size of the packet header
        public const int PACKET_HEADER_SZ = 10;

        //***********************************************
        //***********************************************
        //The packet types

        // get register packet data
        public const int SEND_BUFFER_TYPE = 100;



    }
}
