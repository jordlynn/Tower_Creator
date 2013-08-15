using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Windows.Forms;

namespace serial_packet_protocol
{
        //************************
        //************************
        // For converting byte arrays
        // to standard strings
        public enum EncodingType
        {
            ASCII,
            Unicode,
            UTF7,
            UTF8
        } 


        //************************
        //************************
        // possible baud rates
        public enum spp_BaudRates
        {
            BAUD_2400 = 0,
            BAUD_4800 = 1,
            BAUD_9600 = 2,
            BAUD_19200 = 3,
            BAUD_28800 = 4,
            BAUD_57600 = 5,
            BAUD_115200 = 6,
            BAUD_230400 = 7
        }

        //************************
        //************************
        // for the possible stop bits
        enum spp_StopBits
        {
            STOPBIT_NONE = 0,
            STOPBIT_ONE = 1,
            STOPBIT_TWO = 2,
            STOPBIT_ONEPOINTFIVE = 3
        }


        //************************
        //************************
        // for parity
        enum spp_Parity
        {
            PARITY_NONE = 0,
            PARITY_ODD = 1,
            PARITY_EVEN = 2,
            PARITY_MARK = 3,
            PARITY_SPACE = 4
        }

        //************************
        //************************
        // for com port name
        public enum spp_COMPorts
        {
            PORT_COM1 = 0,
            PORT_COM2 = 1,
            PORT_COM3 = 2,
            PORT_COM4 = 3,
            PORT_COM5 = 4,
            PORT_COM6 = 5,
            PORT_COM7 = 6,
            PORT_COM8 = 7,
        }

    //************************
    //************************
    public class packet_protocol
    {
        // FOR DEBUG
        //protocol_test.Form1 debug_frm;

        // index into gen_buffer
        private int gen_buffer_index = 0;

        // buffer for incoming packet
        private char[] gen_buffer = new char[packet_types.MAX_TOTAL_PACKET_SZ];

        // for RS-232 buffer sizes
        public const int MAX_RS232_BUFFER_SZ = 512000;

        // for queue size
        public const int MAX_QUEUE_SZ = 128;

        // for the actual serial connection
        private SerialPort comport = new SerialPort();

        // storage for baud rates
        private int[] baud_rate_array;

        // storage for stop bits
        private int[] stop_bits_array;

        // storage for parity
        private int[] parity_array;

        // storage for port names
        private string[] portname_array;

        // storage for the total data bits
        private const int spp_DataBits = 8;

        // storage for incoming packets
        Queue rx_packet_queue;


        //*************************************
        //*************************************
        // Main routine to determine what to do
        // with incoming chars
        private void packet_rx_callback_latch(string data)
        {
            // traverse through all chars to extract a possible packet
            for (int data_index = 0; data_index < data.Length; data_index++)
            {

                // for each char, perform a switch
                switch (data[data_index])
                {
                    case packet_types.START_BYTE:
                        gen_buffer_index = 0;
                        break;

                    case packet_types.STOP_BYTE:

                        if (checksum_eval() == packet_types.CHECKSUM_OK)
                            process_packet();
                        else
                            //proc_req_resend();
                            //MessageBox.Show("Transmission ERROR 01");
                            throw new InvalidOperationException("Transmission ERROR 01");

                        gen_buffer_index = 0;
                        clean_gen_buffer();
                        break;

                    // simply store the char
                    default:
                        packet_store_char(data[data_index]);
                        break;

                }

            }//for (uint...

        }

        //*************************************
        //*************************************
        // Routine to store char in buffer
        private void packet_store_char(char c)
        {
            if (gen_buffer_index < packet_types.MAX_TOTAL_PACKET_SZ - 1)
                gen_buffer[gen_buffer_index++] = c;
        }

        //*************************************
        //*************************************
        // Cleans the buffer
        private void clean_gen_buffer()
        {
            for (int i = 0; i < packet_types.MAX_TOTAL_PACKET_SZ; i++)
                gen_buffer[i] = packet_types.NULL_CHAR;
        }


        //*************************************
        //*************************************
        // Extract Packet from Queue
        // Returns:
        // null if no packets exist
        // packet if packets exist
        public char[] extract_packet()
        {
            char[] packet = null;

            // ensure there are packets being stored
            if (rx_packet_queue.Count > 0)
                packet = (char[])rx_packet_queue.Dequeue();

            return packet;
        }

        //*************************************
        //*************************************
        // Returns the total number of packets
        public int total_packets()
        {
            return rx_packet_queue.Count;
        }

        //*************************************
        //*************************************
        // store packet into queue
        private void process_packet()
        {

            // make a copy of the buffer
            char[] buf_cpy = new char[packet_types.MAX_TOTAL_PACKET_SZ];
            for (int i = 0; i < packet_types.MAX_TOTAL_PACKET_SZ; i++)
                buf_cpy[i] = gen_buffer[i];

            // store a copy of the buffer into the queue.
            // the client program will have to extract the packets
            // from the queue
            rx_packet_queue.Enqueue(buf_cpy);

        }

        //*************************************
        //*************************************
        // performs a checksum on the current buffer
        private int checksum_eval()
        {
            // storage for checksum
            char[] check_sum_str = new char[5];
            check_sum_str[4] = packet_types.NULL_CHAR;

            char[] payload_str = new char[5];

            // for the integer checksum
            int checksum_int = 0;
            int payload_int = 0;
            int actual_checksum = 0;

            // first, copy the original buffer so it is not damaged
            char[] gen_buffer_cpy = new char[packet_types.MAX_TOTAL_PACKET_SZ];

            for (int i = 0; i < packet_types.MAX_TOTAL_PACKET_SZ; i++)
                gen_buffer_cpy[i] = gen_buffer[i];

            // extract the checksum
            check_sum_str[0] = gen_buffer_cpy[packet_types.CHECK_SUM_A];
            check_sum_str[1] = gen_buffer_cpy[packet_types.CHECK_SUM_B];
            check_sum_str[2] = gen_buffer_cpy[packet_types.CHECK_SUM_C];
            check_sum_str[3] = gen_buffer_cpy[packet_types.CHECK_SUM_D];
            check_sum_str[4] = packet_types.NULL_CHAR;

            // try to convert hex to string, if fails, the
            // checksum fails.

            string tmp_check_sum_str = new string(check_sum_str);

            try
            {
                // convert hex to int
                checksum_int =
                    int.Parse(tmp_check_sum_str, System.Globalization.NumberStyles.HexNumber);
            }
            catch (Exception ee)
            {
                // parsing failed.
                return packet_types.CHECKSUM_BAD;
            }

            // extract the payload size
            payload_str[0] = gen_buffer_cpy[packet_types.PAYLOAD_SIZE_A];
            payload_str[1] = gen_buffer_cpy[packet_types.PAYLOAD_SIZE_B];
            payload_str[2] = gen_buffer_cpy[packet_types.PAYLOAD_SIZE_C];
            payload_str[3] = gen_buffer_cpy[packet_types.PAYLOAD_SIZE_D];
            payload_str[4] = packet_types.NULL_CHAR;


            // try to convert hex to string, if fails, the
            // checksum fails.

            string payload_sum_str = new string(payload_str);

            try
            {
                // convert hex to int
                payload_int =
                    int.Parse(payload_sum_str, System.Globalization.NumberStyles.HexNumber);
            }
            catch (Exception ee)
            {
                // parsing failed.
                return packet_types.CHECKSUM_BAD;
            }


            // erase the checksum values in the packet so summation can work
            gen_buffer_cpy[packet_types.CHECK_SUM_A] = packet_types.NULL_CHAR; ;
            gen_buffer_cpy[packet_types.CHECK_SUM_B] = packet_types.NULL_CHAR; ;
            gen_buffer_cpy[packet_types.CHECK_SUM_C] = packet_types.NULL_CHAR; ;
            gen_buffer_cpy[packet_types.CHECK_SUM_D] = packet_types.NULL_CHAR; ;


            try
            {
                //perform acutal summation
                for (int i = 0; i < payload_int + packet_types.PACKET_HEADER_SZ; i++)
                    actual_checksum += gen_buffer_cpy[i];
            }
            catch (Exception ee)
            {
                // parsing failed.
                return packet_types.CHECKSUM_BAD;
            }


            // if the two checksums match, the packet is OK
            if (actual_checksum == checksum_int)
                return packet_types.CHECKSUM_OK;
            else
                return packet_types.CHECKSUM_BAD;
        }


        //************************
        //************************
        // routine called when data
        // is recieved
        private void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // init a temp string
            char[] tmp = new char[2];
            tmp[0] = packet_types.DONT_CARE_CHAR;
            tmp[1] = packet_types.NULL_CHAR;

            // init the string
            string data = new string(tmp);

            // attempt to extract the string from rs-232
            try
            {
                data = comport.ReadExisting();

                // DEBUG
                //debug_frm.display_string(data);
            }
            catch (Exception ee)
            {
                //unknown what to do yet.
            }

            // extract possible packet from 'data'
            packet_rx_callback_latch(data);
        }


        //************************
        //************************
        // create a new com connection
        private void connect(   spp_COMPorts cport,
                                spp_BaudRates brate)
        {
            comport.BaudRate = baud_rate_array[(int)brate];
            comport.DataBits = spp_DataBits;
            comport.StopBits = (StopBits)stop_bits_array[(int)spp_StopBits.STOPBIT_ONE];
            comport.Parity = (Parity)parity_array[(int)spp_Parity.PARITY_NONE];
            comport.PortName = portname_array[(int)cport];
            comport.ReadBufferSize = MAX_RS232_BUFFER_SZ;
            comport.WriteBufferSize = MAX_RS232_BUFFER_SZ;

            // set up the latch
            comport.DataReceived += new SerialDataReceivedEventHandler(port_DataReceived);
            try
            {
                comport.Open();
            }
            catch
            {
                // debug
                //MessageBox.Show("Cannot open comport.");
                throw new InvalidOperationException("Couldn't open the comport");
            }
                
           //debug_frm.display_string("CONNECT\n\r\n\r");
        }


        //************************
        //************************
        // default constructor
        // arguments:
        // PORT NAME: spp_COMPorts.PORT_COM3 (example)
        // BAUD RATE: spp_BaudRates.BAUD_9600 (example)
        public packet_protocol( spp_COMPorts cport,
                                spp_BaudRates brate)
                                //protocol_test.Form1 frm1)
        {

            //********************************
            // Queue to store incoming packets from XMega
            rx_packet_queue = new Queue(MAX_QUEUE_SZ);

            //********************************
            // set up for DEBUG
            //debug_frm = frm1;
            //debug_frm.display_string("DEBUG MODE.\n\r\n\r");

            //********************************
            // set up the baud rates
            int BaudRate_count = Enum.GetValues(typeof(spp_BaudRates)).Length;
            baud_rate_array = new int[BaudRate_count];
            
            //********************************
            // clean the buffer that stores the
            // incoming packet
            clean_gen_buffer();

            baud_rate_array[(int)spp_BaudRates.BAUD_2400] = 2400;
            baud_rate_array[(int)spp_BaudRates.BAUD_4800] = 4800;
            baud_rate_array[(int)spp_BaudRates.BAUD_9600] = 9600;
            baud_rate_array[(int)spp_BaudRates.BAUD_19200] = 19200;
            baud_rate_array[(int)spp_BaudRates.BAUD_28800] = 28800;
            baud_rate_array[(int)spp_BaudRates.BAUD_57600] = 57600;
            baud_rate_array[(int)spp_BaudRates.BAUD_115200] = 115200;
            baud_rate_array[(int)spp_BaudRates.BAUD_230400] = 230400;

            //********************************
            // set up the stop bits
            int StopBits_count = Enum.GetValues(typeof(spp_StopBits)).Length;
            stop_bits_array = new int[StopBits_count];

            stop_bits_array[(int)spp_StopBits.STOPBIT_NONE] = (int)spp_StopBits.STOPBIT_NONE;
            stop_bits_array[(int)spp_StopBits.STOPBIT_ONE] = (int)spp_StopBits.STOPBIT_ONE;
            stop_bits_array[(int)spp_StopBits.STOPBIT_TWO] = (int)spp_StopBits.STOPBIT_TWO;
            stop_bits_array[(int)spp_StopBits.STOPBIT_ONEPOINTFIVE] = (int)spp_StopBits.STOPBIT_ONEPOINTFIVE;

            //********************************
            // set up the parity
            int Parity_count = Enum.GetValues(typeof(spp_Parity)).Length;
            parity_array = new int[Parity_count];

            parity_array[(int)spp_Parity.PARITY_NONE] = (int)spp_Parity.PARITY_NONE;
            parity_array[(int)spp_Parity.PARITY_ODD] = (int)spp_Parity.PARITY_ODD;
            parity_array[(int)spp_Parity.PARITY_EVEN] = (int)spp_Parity.PARITY_EVEN;
            parity_array[(int)spp_Parity.PARITY_MARK] = (int)spp_Parity.PARITY_MARK;
            parity_array[(int)spp_Parity.PARITY_SPACE] = (int)spp_Parity.PARITY_SPACE;

            //********************************
            // set up the portname
            int PortName_count = Enum.GetValues(typeof(spp_COMPorts)).Length;
            portname_array = new string[PortName_count];

            portname_array[(int)spp_COMPorts.PORT_COM1] = "COM1";
            portname_array[(int)spp_COMPorts.PORT_COM2] = "COM2";
            portname_array[(int)spp_COMPorts.PORT_COM3] = "COM3";
            portname_array[(int)spp_COMPorts.PORT_COM4] = "COM4";
            portname_array[(int)spp_COMPorts.PORT_COM5] = "COM5";
            portname_array[(int)spp_COMPorts.PORT_COM6] = "COM6";
            portname_array[(int)spp_COMPorts.PORT_COM7] = "COM7";

            // Connect to the serial port
            connect(cport, brate);
        }

        //************************
        //************************
        // Write string
        public void write_string(string buffer)
        {
            comport.Write(buffer);
        }

        //************************************
        //************************************
        public int snd_ascii_hex(int packet_type,
                                 byte[] payload,
                                int payload_sz)
        {
            // first, convert to ascii hex
            string strBuffer = ByteArrayToString(payload);

            // convert string back to byte array
            payload = StrToByteArray(strBuffer);

            payload_sz = payload.Length;

            return snd_packet(packet_type,
                                payload,
                                payload_sz);
        }


        //*************************************
        //*************************************
        // Sends a packet off
        public int snd_packet(int packet_type,
                                byte[] payload,
                                int payload_sz)
        {

            // first, ensure comport is even open
            if (!comport.IsOpen)
            {
                //MessageBox.Show("NO COMPORTS OPEN");
                //throw new Exception("No comports open");
                return -1;
            }

            

            // for start
            byte[] start_byte = { (byte)packet_types.START_BYTE };
            byte[] stop_byte = { (byte)packet_types.STOP_BYTE };

            // storage for a complete packet
            int complete_packet_sz = packet_types.PACKET_HEADER_SZ + payload_sz;
            byte[] complete_packet = new byte[complete_packet_sz];

            // store the packet type
            string packet_type_string = packet_type.ToString(packet_types.DONT_CARE_STRING);
            if (packet_type_string.Length == 1)
            {
                // type is only one byte long
                complete_packet[packet_types.PACKET_TYPE_A] = packet_types.ZERO_CHAR;
                complete_packet[packet_types.PACKET_TYPE_B] = (byte)packet_type_string[0];
            }
            else
            {
                // type is two bytes long
                complete_packet[packet_types.PACKET_TYPE_A] = (byte)packet_type_string[0];
                complete_packet[packet_types.PACKET_TYPE_B] = (byte)packet_type_string[1];
            }

            // set checksum to null for right now
            // values will be replaced with checksum_apply()
            complete_packet[packet_types.CHECK_SUM_A] = 0x00;
            complete_packet[packet_types.CHECK_SUM_B] = 0x00;
            complete_packet[packet_types.CHECK_SUM_C] = 0x00;
            complete_packet[packet_types.CHECK_SUM_D] = 0x00;

            // copy over size of payload, but first set all to NULL
            for (int i = 0; i < packet_types.WORD_SZ; i++)
                complete_packet[packet_types.PAYLOAD_SIZE_A + i] = packet_types.ZERO_CHAR;

            // convert payload_sz to hex
            string payload_sz_string = payload_sz.ToString("X");

            // switch on the size of the payload to figure out which bytes
            // to set
            switch (payload_sz_string.Length)
            {
                // payload size is one byte
                case 1:
                    complete_packet[packet_types.PAYLOAD_SIZE_D] = (byte)payload_sz_string[0];
                    break;

                // payload size is two bytes
                case 2:
                    complete_packet[packet_types.PAYLOAD_SIZE_C] = (byte)payload_sz_string[0];
                    complete_packet[packet_types.PAYLOAD_SIZE_D] = (byte)payload_sz_string[1];
                    break;

                // payload size is three bytes
                case 3:
                    complete_packet[packet_types.PAYLOAD_SIZE_B] = (byte)payload_sz_string[0];
                    complete_packet[packet_types.PAYLOAD_SIZE_C] = (byte)payload_sz_string[1];
                    complete_packet[packet_types.PAYLOAD_SIZE_D] = (byte)payload_sz_string[2];
                    break;

                // payload size is four bytes
                case 4:
                    complete_packet[packet_types.PAYLOAD_SIZE_A] = (byte)payload_sz_string[0];
                    complete_packet[packet_types.PAYLOAD_SIZE_B] = (byte)payload_sz_string[1];
                    complete_packet[packet_types.PAYLOAD_SIZE_C] = (byte)payload_sz_string[2];
                    complete_packet[packet_types.PAYLOAD_SIZE_D] = (byte)payload_sz_string[3];
                    break;
            }

            // copy over the payload
            int payload_addr_start = packet_types.PACKET_HEADER_SZ;


            for (int i = payload_addr_start; i < complete_packet_sz; i++)
            {
                complete_packet[i] = payload[i - payload_addr_start];
            }


            // apply the checksum
            checksum_apply(complete_packet, complete_packet_sz);

            //DEBUG
            // Display packet on screen
            //string debugstr = ByteArrayToString(complete_packet, EncodingType.ASCII);
            //debug_frm.display_string(" --> " + debugstr + " <-- ");



            try
            {
                char[] tmp = new char[2];

                // send out the packet
                // signal start of packet
                comport.Write(start_byte, 0, 1);
                tmp[0] = (char)start_byte[0];
                tmp[1] = packet_types.NULL_CHAR;
                string tmp_str = new string(tmp);


                // send the actual packet
                comport.Write(complete_packet, 0, complete_packet_sz);
                char[] buf = new char[complete_packet.Length + 1];
                for (int i = 0; i < complete_packet.Length; i++)
                    buf[i] = (char)complete_packet[i];
                buf[complete_packet.Length] = packet_types.NULL_CHAR;
                string buf_str = new string(buf);

                
                // signal end of packet
                comport.Write(stop_byte, 0, 1);
                tmp[0] = (char)stop_byte[0];
                tmp[1] = packet_types.NULL_CHAR;
                string tmp_string = new string(tmp);

            }
            catch (Exception ee)
            {
                //throw new InvalidOperationException("Couldn't send the packet", ee);
                throw;
                return -1;
            }

            return 1;

        }


        //*************************************
        //*************************************
        // Apply the checksum
        public void checksum_apply(byte[] complete_packet,
                                    int packet_sz)
        {
            ushort checksum = 0;

            // summation of the entire packet
            for (int i = 0; i < packet_sz; i++)
                checksum += complete_packet[i];

            // convert checksum to hex
            string checksum_string = checksum.ToString(packet_types.DONT_CARE_STRING);

            // set all checksum bytes to zero
            switch (checksum_string.Length)
            {
                // payload size is one byte
                case 1:
                    complete_packet[packet_types.CHECK_SUM_A] = (byte)packet_types.ZERO_CHAR;
                    complete_packet[packet_types.CHECK_SUM_B] = (byte)packet_types.ZERO_CHAR;
                    complete_packet[packet_types.CHECK_SUM_C] = (byte)packet_types.ZERO_CHAR;
                    complete_packet[packet_types.CHECK_SUM_D] = (byte)checksum_string[0];
                    break;

                // payload size is two bytes
                case 2:
                    complete_packet[packet_types.CHECK_SUM_A] = (byte)packet_types.ZERO_CHAR;
                    complete_packet[packet_types.CHECK_SUM_B] = (byte)packet_types.ZERO_CHAR;
                    complete_packet[packet_types.CHECK_SUM_C] = (byte)checksum_string[0];
                    complete_packet[packet_types.CHECK_SUM_D] = (byte)checksum_string[1];
                    break;

                // payload size is three bytes
                case 3:
                    complete_packet[packet_types.CHECK_SUM_A] = (byte)packet_types.ZERO_CHAR;
                    complete_packet[packet_types.CHECK_SUM_B] = (byte)checksum_string[0];
                    complete_packet[packet_types.CHECK_SUM_C] = (byte)checksum_string[1];
                    complete_packet[packet_types.CHECK_SUM_D] = (byte)checksum_string[2];
                    break;

                // payload size is four bytes
                case 4:
                    complete_packet[packet_types.CHECK_SUM_A] = (byte)checksum_string[0];
                    complete_packet[packet_types.CHECK_SUM_B] = (byte)checksum_string[1];
                    complete_packet[packet_types.CHECK_SUM_C] = (byte)checksum_string[2];
                    complete_packet[packet_types.CHECK_SUM_D] = (byte)checksum_string[3];
                    break;
            }
        }

        //*************************************
        //*************************************
        // Send a byte buffer demo
        public void send_byte_buffer()
        {

            int total_bytes = 8;
            byte cnt = 136;

            byte[] buffer = new byte[total_bytes];
            for (int i = 0; i < total_bytes; i++)
            {
                buffer[i] = cnt;
                cnt += 17;
            }

            // first, convert to ascii hex
            string strBuffer = ByteArrayToString(buffer);

            // convert string back to byte array
            buffer = StrToByteArray(strBuffer);

            snd_packet(packet_types.SEND_BUFFER_TYPE, buffer, buffer.Length);
        }

        //*************************************
        //*************************************
        // Convert a string to array of bytes
        public static byte[] StrToByteArray(string str)
        {
            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
            return encoding.GetBytes(str);
        }

        //*************************************
        //*************************************
        // Convert a byte array to
        // a hex string.
        public static string ByteArrayToString(byte[] ba)
        {
            string tmp;
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);

            tmp = hex.ToString().ToUpper();
         
            return tmp;
        }


        //*************************************
        //*************************************
        // Convert a byte array to string
        public static string ByteArrayToString(byte[] bytes, EncodingType encodingType)
        {
            System.Text.Encoding encoding = null;
            switch (encodingType)
            {
                case EncodingType.ASCII:
                    encoding = new System.Text.ASCIIEncoding();
                    break;
                case EncodingType.Unicode:
                    encoding = new System.Text.UnicodeEncoding();
                    break;
                case EncodingType.UTF7:
                    encoding = new System.Text.UTF7Encoding();
                    break;
                case EncodingType.UTF8:
                    encoding = new System.Text.UTF8Encoding();
                    break;
            }
            return encoding.GetString(bytes);
        }

        //
        public void close()
        {
            comport.Close();
        }

    }
}
