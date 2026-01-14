using Microsoft.AspNetCore.Http;
using System;
using System.Collections;
using System.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading;

namespace BlackboxServer
{
    public class UDP_Server
    {
        public struct UDP_ServerData
        {
            public int Port; // tcp server port
            public string plcIP; // client ip address reference to protect against spoofing
            public string Location; // the port number as a string

            public bool UpdatePLC { get; set; } // server updates the plc when true
        }

        public struct UDPData
        {
            public string IPAddress;

            public string Raw_Line_ID;
            public string Raw_Line_Message;

            public string Supervisor;
            public string Operator;
            public string ProductionOrder;
            //public string Operation;
            public string Material;
            public string Sevent;
            public string Status; // Enable/Disable status
            public string Override;

            public string CT; // cycletime
            public string LT; // loadtime

            public string conf_number; // confirmation number
            public string oper_number; // operation number
            public string base_quantity; // base quantity

            public string SAP_Setup; // setup time from sap
            public string MachineCycletime; // ideal Machine cycle time from sap 

            public string Unit_Of_Measure;

            public string StdValue1;
            public string StdValue1Unit;
            public string StdValue2;
            public string StdValue2Unit;
            public string StdValue3;
            public string StdValue3Unit;
            public string StdValue4;
            public string StdValue4Unit;
            public string machid;

            // MtConnect Data reiteration
            public string MachineState;
            public string MachineState2;
            public string Partstring;
            public int Part;
            public string Partstring2;
            public int Part2;
            public string PartTargetstring;
            public int PartTarget;

            public string OPComment; // comment made by operator.
            public string prevOPComment;

            // for checking when data changes
            public string prev_Raw_Line_Message;
            public bool DetectChange;           
        }

        public static UDPData ListenForData(UDPData data, /*int port,*/ string ClientName, UdpClient Udpc, IPEndPoint IPEndPoint)
        {
            try
            {
                byte[] recieveddata = Udpc.Receive(ref IPEndPoint);

                // UInt16 packetFormat = System.BitConverter.ToUInt16(recieveddata, 0);
                //UInt64 sessionId = System.BitConverter.ToUInt64(recieveddata, 6);
                //float sessionTime = System.BitConverter.ToSingle(recieveddata, 14);

                // Store received data from client 
                //byte[] receivedData = udpc.Receive(ref ipEndPoint);

                string raw_msg = Encoding.UTF8.GetString(recieveddata);
                

                string[] message = raw_msg.Split('<', '>');

                string ID = message[1];
                string Message = message[3];
                // Console.WriteLine("ID= " + message[1] +"  Message= "+ message[3]);

                if (ID == ClientName) // spoof protection
                {
                    data.Raw_Line_ID = ID;
                    data.Raw_Line_Message = Message;
                    Console.WriteLine(data.Raw_Line_ID + " => " + data.Raw_Line_Message);
                }
                else if(ID.Contains("_Comment"))
                {
                    data.OPComment = Message;
                    Console.WriteLine(ID + " => " + Message);
                }
               
                Thread.Sleep(250);
            }
            catch
            {
                Udpc.Close();
                Thread.Sleep(1000);
               // Udpc.Connect(IPEndPoint);
            }

                return data;
        }

        public static UDPData ChangeCheck(UDPData data)
        {
            if (data.Raw_Line_Message != data.prev_Raw_Line_Message && (data.Raw_Line_Message != null || data.Raw_Line_Message != "") && data.Raw_Line_Message.Length>5)
            {
                string[] plc_data = data.Raw_Line_Message.Split(":");

                //Console.WriteLine(plc_data.Length);

                data.Supervisor = plc_data[0];
                data.Operator = plc_data[1];
                data.conf_number = plc_data[2];
                data.ProductionOrder = plc_data[3];
                data.oper_number = plc_data[4];
                data.Material = plc_data[5];
                data.Status = plc_data[6];
                data.Sevent = plc_data[7];
                data.Override = plc_data[8];
                data.MachineState = plc_data[9];
                data.MachineState2 = plc_data[10];
                data.Partstring = plc_data[11];
                data.Partstring2 = plc_data[12];
                data.PartTargetstring = plc_data[13];
                data.CT =  plc_data[14];
                data.LT = plc_data[15];

                data.base_quantity = plc_data[16];
                data.SAP_Setup = plc_data[17];
                data.MachineCycletime = plc_data[18];

                // convert the part to a integer
                try
                {
                    data.Part = int.Parse(data.Partstring);
                }
                catch
                {
                    data.Part = 0;
                }
                // convert the part2 to a integer
                try
                {
                    data.Part2 = int.Parse(data.Partstring2);
                }
                catch
                {
                    data.Part2 = 0;
                }
                // convert the part target to a integer
                try
                {
                    data.PartTarget = int.Parse(data.PartTargetstring);
                }
                catch
                {
                    data.PartTarget = 0;
                }

                data.DetectChange = true;
                data.prev_Raw_Line_Message = data.Raw_Line_Message;
            }
            else
            {
                data.DetectChange = false;
            }

            return data;
        }

    }
}
