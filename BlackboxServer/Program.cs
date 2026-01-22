/// Ian Hudis
/// KEB America
/// release 3.1 
/// 1/20/2024



using System.Net.Sockets;
using System.Net;
using static BlackboxServer.XML_Builder;
using static BlackboxServer.MTCData;
using static BlackboxServer.SAP;
using System.Text;
using System;
using System.Xml.Linq;
using System.Reflection.PortableExecutable;
//using static BlackboxServer.TCP_Server;
using static BlackboxServer.UDP_Server;
using System.Data;

namespace BlackboxServer
{
    public class Program
    {
        public const string version = "V3.4";

        static void Main()
        {
            //! Get Input data from Serverdata text file 
            // enter in MTConnect Data          
                string Type = "";
                string MTAddress = ""; //= "192.168.200.25:5111"; // I am going to use 2283 for the test server
                string PathAddress = ""; //= "/Path[@id='path1']/DataItems/DataItem[@id='execution'%20 or @id='mode' or @id='PartCountAct' or @id='PartCountTarget']"; // target path to get the desired data
                // Enter TCP Data
                string PLCIP = "";// "192.168.202.29"; // ip of the expected plc (security)
                int port = 0;  // port number of the server
                int machId = 0;//id of the machine
                int heads = 1; // number of heads the machine has 
                //Pass the file path and file name to the StreamReader constructor
                StreamReader sr = new("ServerData.txt");
                //Read the first line of text
                string line = sr.ReadLine();
                //Continue to read until you reach end of file
                while (line != null)
                {
                    if (line.Contains("Type = ")) // get the MtAddress
                    {
                        string[] s = line.Split("= ");
                        Type = s[1];
                    }
                    if (line.Contains("MTAddress = ")) // get the MtAddress
                    {
                        string[] s = line.Split("= ");
                        MTAddress = s[1];
                    }
                    else if(line.Contains("MTPathAddress = "))
                    {
                        string[] s = line.Split("= ");
                        PathAddress = s[1];
                    }
                    else if(line.Contains("PLC_IP = "))
                    {
                        string[] s = line.Split("= ");
                        PLCIP =  s[1];
                    }
                    else if(line.Contains("Port = "))
                    {
                        string[] s = line.Split("= ");
                        port=  int.Parse(s[1]);
                    }
                    else if (line.Contains("MachID ="))
                    {
                    string[] s = line.Split("= ");
                    machId =  int.Parse(s[1]);
                    }
                    else if (line.Contains("Heads = "))
                    {
                        string[] s = line.Split("= ");
                        heads =  int.Parse(s[1]);
                    }
                    //Read the next line
                    line = sr.ReadLine();
                }
                //close the file
                sr.Close();

                Console.WriteLine("Version = "+ version +"\nType = " + Type +"\nAddress = " + MTAddress + "\nPath = " + PathAddress + "\nPLC_IP = " + PLCIP + "\nPORT = " 
                    + port + "\nMachId = " + machId + "\nHEADS = " + heads);
            //! Get Input data from Serverdata text file 

            // make the datasets
            DataSet XML_data_output = new();
            if (File.Exists("BlackBox_XML/Machine" + port + ".xml")) // see if the xml file exists
            {
                XML_data_output.ReadXml("BlackBox_XML/Machine" + port + ".xml");
            }
            DataSet XML_data_input= new();
            if (File.Exists("BlackBox_XML/" + "MTConectData" + ".xml")) // see if the xml file exists
            {
                XML_data_input.ReadXml("BlackBox_XML/" + "MTConectData" + ".xml");
            }
            // Creating and initializing threads
            MachineServer machinetest = new(Type, PLCIP, port,/* machId,*/ MTAddress, PathAddress, heads, XML_data_output, XML_data_input);
            Thread MTConnectThread = new(MachineServer.MTConnectListener);
            MTConnectThread.Start();

            Thread UDPServer = new(MachineServer.UDPServer);
            UDPServer.Start();

            // make the executeable app
            var builder = WebApplication.CreateBuilder();
            
            builder.WebHost.ConfigureKestrel(serverOptions =>
            {
                serverOptions.ListenAnyIP(5321); // http
            });
            
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                //app.UseHsts();
            }

            //app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.MapGet(port +"/", () => XML_data_output.GetXml()); // advertise plc data
            app.MapGet("/", () => XML_data_input.GetXml()); // advertise mt connect data
            app.Run();
        }
    }

    public class MachineServer
    {

        // page datasets
        private static DataSet? XML_output { get; set; }
        private static DataSet? XML_input { get; set; }

        // UDP information
        private static UDP_ServerData UDP = new(); // data needed for the UDP Server
        private static UDPData UDPData = new(); // database the comes from the kiosk

     // tcp information
      //  private static TCP_Server.TCP_ServerData TCP = new(); // data needed to use the TCP Server
      //  private static TCP_Server.TCPData TCPData = new(); // database the comes from the blackbox

     // information for mtconnect data extraction
        private static MTCdata MTdata = new(); // database in which the mtconnect info is stored
        private static MTCdata PrevMTdata = new(); // for detecting mtconnect changes

     // info coming from sap
        private static Data sapdata = new(); // create object for sap data

       // private static string? Machid;
        private static int Heads;
        private static string Type = "";

     // for xml making
        private static Blkbox BlackBox = new();  // xml object used to upload the xml
        private static Blkbox MtConnectXML = new(); // for debugging relevent mtconnect data

        // input machine context for both MTconect data and blackbox into the server
        public MachineServer(string type, string plcip, int port, /*int machid,*/ string mTConnectAddress, string mTConnectPort, int heads, DataSet Do, DataSet Di)
        {
            UDP.plcIP = plcip;
            UDP.Port = port;
            //Machid = machid.ToString();
            UDP.Location = port.ToString();
            //TCP.plcIP = plcip; // insert expected plc ip address (security)
            //TCP.Port = port;    // server port being used
            //Machid = machid.ToString(); // machine identity
            //TCP.Location = port.ToString();
            Heads = heads; // number of heads the machine has
            Type = type; //how to read the machine data

            // datasets
            XML_output = Do;
            XML_input = Di;

            MTdata.ServerIP = mTConnectAddress;

            if (type == "haas")
            {
                MTdata.MTConnectAddress = "http://" + mTConnectAddress + "/current?path=//MTConnectDevices/Devices/Device/Components/Controller"; // haas version
            }
            else if(type == "amada" || type == "direct")
            {
                MTdata.MTConnectAddress = "http://" + mTConnectAddress;

            }
            else if (type == "bdtronic")
            {
                MTdata.MTConnectAddress = "http://" + mTConnectAddress + "/current"; // basic current mtconnect query for the glue machine
            }
            else
            {
                MTdata.MTConnectAddress = "http://" + mTConnectAddress + "/current?path=//MTConnectDevices/Devices/Device/Components/Controller/Components"; // mazak version
            }
            
            MTdata.MTConnectPath = mTConnectPort;
        }

     // listens to mtconnect data
        public static void MTConnectListener() // MTConnect Thread
        {
            Console.WriteLine("Grabbing data from " + MTdata.ServerIP + ";\n");
           
            MtConnectXML.XMLdoc = new();  // for writing the mtconnect xml
            MtConnectXML.Mtdoc = new(); // for reading the mtconnect xml
            BlackBox.XMLdoc = new(); // for writing the kiosk xml

            //! post the xml data
            // MTConnect and SAP from server

            if (File.Exists("BlackBox_XML/" + "MTConectData" + ".xml") != true || new FileInfo("BlackBox_XML/" + "MTConectData" + ".xml").Length ==0) // see if the xml file exists
            {
                //UpdateMTConnect_XML(TCPData, MTdata, sapdata, "MTConectData", MtConnectXML.Mtdoc); // update the mt connect xml file
                UpdateMTConnect_XML(UDPData, MTdata, sapdata, "MTConectData", MtConnectXML.Mtdoc); // make a new mtconnectxml
            }
            else // read the file from the last session if it exists
            {
                //Pass the file path and file name to the StreamReader constructor
                StreamReader sr = new("BlackBox_XML/" + "MTConectData" + ".xml");
                string datafromprevrun = sr.ReadToEnd();
                string[] lines = datafromprevrun.Split('<', '>', ' ', '\"');

                // grab data from previous session
                for (int i = 0; i<lines.Length; i++)
                {
                    switch (lines[i])
                    {
                        case "CN=":
                            sapdata.confirmationorder = lines[i+1];
                            break;
                        case "ID=":
                            sapdata.machid = lines[i+1];
                            break;
                        case "PO=":
                            sapdata.ProductOrder = lines[i+1];
                            break;
                        case "OP=":
                            sapdata.operationNumber = lines[i+1];
                            break;
                        case "Mat=":
                            sapdata.material = lines[i+1];
                            break;
                    }
                }
                //close the file
                sr.Close();
            }
            XML_input.Clear();
            XML_input.ReadXml("BlackBox_XML/" + "MTConectData" + ".xml"); // puts the xml file into a dataset


            // Data from the Kiosk
            string title = "Machine" +  UDP.Location;
            //string title = "Machine" +  TCP.Location;
            if (File.Exists("BlackBox_XML/" + title + ".xml") != true) // see if the xml file exists
            {
                UpdateXML(UDPData, title, BlackBox.XMLdoc, Program.version); //create the kiosk xml file if it doesnt exist
                //UpdateXML(TCPData, title, BlackBox.XMLdoc, Program.version); //create the kiosk xml file if it doesnt exist
            }
            else // read from the existing xml if it does exist
            {
                //Pass the file path and file name to the StreamReader constructor
                StreamReader sr = new("BlackBox_XML/" + title + ".xml");
                string datafromprevrun = sr.ReadToEnd();
                string[] lines = datafromprevrun.Split('<', '>', ' ', '\"');

                // grab data from previous session
                for (int i = 0; i<lines.Length; i++)
                {
                    switch (lines[i])
                    {
                        case "Update=":
                         //   Console.WriteLine(lines[i]+ ": " + lines[i+1]);
                            break;
                        case "SUP=":
                            //TCPData.Supervisor = lines[i+1];
                          //  Console.WriteLine("Supervisor: " + TCPData.Supervisor);
                            UDPData.Supervisor = lines[i+1];
                            break;
                        case "Oper=":
                            //TCPData.Operator = lines[i+1];
                            // Console.WriteLine("Operator: " + TCPData.Operator);
                            UDPData.Operator = lines[i+1];
                            break;
                        case "CN=":
                            //TCPData.conf_number = lines[i+1];
                            //Console.WriteLine("ConfirmNumber: " + TCPData.conf_number);
                            UDPData.conf_number = lines[i+1];
                            break;
                        case "Prod_Order=":
                           // TCPData.ProductionOrder = lines[i+1];
                            //Console.WriteLine("Prod. Order: " + TCPData.ProductionOrder);
                            UDPData.ProductionOrder = lines[i+1];
                            break;
                        case "Operation=":
                            //TCPData.oper_number = lines[i+1];
                            //Console.WriteLine("Prod. Order: " + TCPData.ProductionOrder);
                            UDPData.oper_number = lines[i+1];
                            break;
                        case "Material=":
                            //TCPData.Material = lines[i+1];
                            UDPData.Material = lines[i+1];
                            break;
                        case "TargetPartCount=":
                            //TCPData.PartTargetstring = lines[i+1];
                            UDPData.PartTargetstring = lines[i+1];
                            try
                            {
                                //Data.PartTarget = int.Parse(TCPData.PartTargetstring);
                                UDPData.PartTarget = int.Parse(UDPData.PartTargetstring);
                            }
                            catch
                            {

                            }
                            break;
                        case "CT=":
                            //TCPData.CT = lines[i+1];
                            UDPData.CT = lines[i+1];
                            break;
                        case "LT=":
                            //TCPData.LT = lines[i+1];
                            UDPData.LT = lines[i+1];
                            break;
                        case "BQ=":
                            //TCPData.base_quantity = lines[i+1];
                            UDPData.base_quantity = lines[i+1];
                            break;
                        case "Setup=":
                            //TCPData.SAP_Setup = lines[i+1];
                            UDPData.SAP_Setup = lines[i+1];
                            break;
                        case "MCT=":
                            //TCPData.MachineCycletime = lines[i+1];
                            UDPData.MachineCycletime = lines[i+1];
                            break;
                        case "Status=":
                            //TCPData.Status = lines[i+1];
                            UDPData.Status = lines[i+1];
                            break;
                        case "Event=":
                            //TCPData.Sevent = lines[i+1];
                            UDPData.Sevent = lines[i+1];
                            break;
                        case "Override=":
                            //TCPData.Override = lines[i+1];
                            //Console.WriteLine("Override: " + TCPData.oper_number);
                            UDPData.Override = lines[i+1];
                            break;
                        case "MachineState=":
                            //TCPData.MachineState = lines[i+1];
                            UDPData.MachineState = lines[i+1];
                            break;
                        case "MachineState2=":
                            //TCPData.MachineState = lines[i+1];
                            UDPData.MachineState2 = lines[i+1];
                            break;
                        case "PartCount=":
                            //TCPData.Partstring = lines[i+1];
                            UDPData.Partstring = lines[i+1];
                            try
                            {
                                //TCPData.Part = int.Parse (TCPData.Partstring);
                                UDPData.Part = int.Parse(UDPData.Partstring);
                            }
                            catch
                            {

                            }
                            break;
                        case "PartCount2=":
                            //TCPData.Partstring2 = lines[i+1];
                            UDPData.Partstring2 = lines[i+1];
                            try
                            {
                                //TCPData.Part2 = int.Parse (TCPData.Partstring2);
                                UDPData.Part2 = int.Parse(UDPData.Partstring2);
                            }
                            catch
                            { }
                            break;
                    }                                                   
                }

                //close the file
                sr.Close();
                 //UpdateXML(TCPData, title, BlackBox.XMLdoc, Program.version); //create the kiosk xml file if it doesnt exist
                UpdateXML(UDPData, title, BlackBox.XMLdoc, Program.version); //create the kiosk xml file if it doesnt exist
            }

            XML_output.Clear();
            XML_output.ReadXml("BlackBox_XML/" + title + ".xml"); // puts the xml file into a dataset
            //UploadPage(BlackBox.MtData, BlackBox.XMLData, UDP.Location /*TCP.Location*/);
            //! post the xml data

            while (true)
            {
                //actual program
                try
                {
                    MTdata = HTTPget(MTdata, Heads, Type, sapdata.confirmationorder); // grabs the mt connect data


                    if (MTdata.MachineState != PrevMTdata.MachineState || MTdata.MachineState2 != PrevMTdata.MachineState2  || MTdata.PartCount != PrevMTdata.PartCount || MTdata.PartCount2 != PrevMTdata.PartCount2
                        || MTdata.PartCountTarget != PrevMTdata.PartCountTarget || MTdata.Material != PrevMTdata.Material || UDPData.OPComment != UDPData.prevOPComment)                       //! mtconnect change
                    {
                        UDPData.prevOPComment = UDPData.OPComment;
                        //  Console.WriteLine(MTdata.MachineState + " " + MTdata.PartCount + " " + MTdata.PartCount2 + " "+ MTdata.PartCountTarget + ";"); // debug
                        PrevMTdata = MTdata;
                        UpdateMTConnect_XML(/*TCPData,*/ UDPData, MTdata, sapdata, "MTConectData", MtConnectXML.Mtdoc); // update the mt connect xml file
                        //TCPData.DetectChange = true; // update the server
                        UDPData.DetectChange = true; // update the server
                    }
                    //else if (MTdata.ConfirmationNumber != TCPData.conf_number && TCPData.conf_number != "" && TCPData.DetectChange == false)  //! sap call
                    else if (MTdata.ConfirmationNumber != UDPData.conf_number && UDPData.conf_number != "" && UDPData.DetectChange == false)  //! sap call
                    {
                        // Console.WriteLine("Start SAP Search for " + TCPData.conf_number); // debug
                        Console.WriteLine("Start SAP Search for " + UDPData.conf_number); // debug
                        //sapdata = GrabSAPData(TCPData.conf_number); // grab the sap data needed
                        sapdata = GrabSAPData(UDPData.conf_number); // grab the sap data needed
                        Console.WriteLine("   " + sapdata.ProductOrder +" "+ sapdata.operationNumber +" " +sapdata.material + " "+ sapdata.OrderQuanity.ToString() + "\n"+
                            "   " + sapdata.OrderQuanity.ToString() +" "+ sapdata.IdealLoadTime.ToString() + " " + sapdata.baseQuality + " "+ sapdata.UnitOfMeasure + "\n" +
                            "   " + sapdata.StdValue1 + sapdata.StdValue1Unit + " " + sapdata.StdValue2 + sapdata.StdValue2Unit + " " + sapdata.StdValue3 + sapdata.StdValue3Unit + " " + sapdata.StdValue4 + sapdata.StdValue4Unit);  // debug
                        //TCPData.conf_number = sapdata.confirmationorder;
                        UDPData.conf_number = sapdata.confirmationorder;
                        MTdata.ConfirmationNumber = sapdata.confirmationorder;
                        //MTdata.ConfirmationNumber = TCPData.conf_number;


                        //! sending the sap data to the plc
                        // total part count
                            //TCPData.PartTargetstring = sapdata.OrderQuanity.ToString(); // grab the data from sap
                            UDPData.PartTargetstring = sapdata.OrderQuanity.ToString(); // grab the data from sap
                            //MTdata.PartCountTarget = TCPData.PartTargetstring;
                            MTdata.PartCountTarget = UDPData.PartTargetstring;

                            // production order
                            MTdata.ProductionOrder = sapdata.ProductOrder; // grab the production order from sap
                            // operation
                            MTdata.Operation = sapdata.operationNumber; // grab the operation from sap
                            // material 
                            MTdata.Material = sapdata.material; // grab the material data from sap
                            // Base Qualitity 
                            MTdata.baseQuality = sapdata.baseQuality;
                            // info 1
                            MTdata.IdealCycleTime = sapdata.IdealCycleTime;
                            // info 2 IIOT
                            MTdata.IdealLoadTime = sapdata.IdealLoadTime;
                            // Setup time in minutes
                            MTdata.StdValue1 = sapdata.StdValue1;
                            // Machine time in minutes
                            MTdata.StdValue2 = sapdata.StdValue2;
                            // machine calc in minutes  `
                            MTdata.StdValue3 = sapdata.StdValue3;
                            // fix costs
                            MTdata.StdValue4 = sapdata.StdValue4;
                        //! sending the sap data to the plc

                        //UpdateMTConnect_XML(TCPData, MTdata, sapdata, "MTConectData", MtConnectXML.Mtdoc); // update the mt connect xml file
                        UpdateMTConnect_XML(UDPData, MTdata, sapdata, "MTConectData", MtConnectXML.Mtdoc);
                        //TCPData.DetectChange = true; // update the server
                        UDPData.DetectChange = true; // update the server
                    }
                    else    //! post the data if anything had changed
                    {

                        if (/*TCPData.DetectChange*/ UDPData.DetectChange)
                        {
                            //string title = "Machine" +  TCP.Location;
                            //UpdateXML(TCPData, title, BlackBox.XMLdoc, Program.version); // update the kiosk xml file
                            UpdateXML(UDPData, title, BlackBox.XMLdoc, Program.version); // update the kiosk xml file
                            // this is for data reiteration
                            //UpdateMTConnect_XML(TCPData, MTdata, sapdata, "MTConectData", MtConnectXML.Mtdoc); // update the mt connect xml file
                            UpdateMTConnect_XML(UDPData, MTdata, sapdata, "MTConectData", MtConnectXML.Mtdoc); // update the mt connect xml file

                            //! post the xml data
                            // MTConnect and SAP from server
                            XML_input.Clear();
                            XML_input.ReadXml("BlackBox_XML/" + "MTConectData" + ".xml"); // puts the xml file into a dataset
                            // Data from the Kiosk
                            XML_output.Clear();
                            XML_output.ReadXml("BlackBox_XML/" + title + ".xml"); // puts the xml file into a dataset
                            //UploadPage(BlackBox.MtData, BlackBox.XMLData, TCP.Location);
                           // UploadPage(BlackBox.MtData, BlackBox.XMLData, UDP.Location);
                            //! post the xml data
                            //TCPData.DetectChange = false; // reset the trigger
                            UDPData.DetectChange = false; // reset the trigger
                        }

                        Thread.Sleep(400); // pause for 0.5 seconds   
                    }
                }
                catch (Exception E)
                {
                    Console.WriteLine("Something went wrong with MTConnect! \n" + E);
                    Thread.Sleep(2500); // 2.5 second pause
                }

                // patch for windows error

            }
        }

        public static void UDPServer()
        {
            UdpClient udpc = new(UDP.Port);
            Console.WriteLine("Server Started, servicing on port no. " + UDP.Port.ToString());
            IPEndPoint? ipEndPoint = new (IPAddress.Any, 0);
            //UDPData.IPAddress = Dns.GetHostEntry(UDP.plcIP).AddressList[0].ToString();

            while (true)
            {
                UDPData = ListenForData(UDPData, /*UDP.Port,*/ UDP.plcIP, udpc, ipEndPoint);
                UDPData = ChangeCheck(UDPData);
            }

        }

        /*
     // Sends and Collects data from the kiosk
        public static void TCPServer()
        {
            Console.WriteLine("Starting TCP Server on port " + TCP.Port + ";\n");

           // string CurrentIPaddress = Dns.GetHostEntry(TCP.plcIP).AddressList[0].ToString(); // tranlsate the plc name to an ip address
            TCPData.IPAddress = Dns.GetHostEntry(TCP.plcIP).AddressList[0].ToString();

            while (true)
            {
					TCPData = TCP_Server.ListenForData(TCPData, TCP.Port, TCP.plcIP);
					TCPData = TCP_Server.ChangeCheck(TCPData);
            }
        } // TCP Server Thread
        */
        
    }
}
