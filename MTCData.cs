using System.Data.SqlTypes;
using System.Reflection.PortableExecutable;
using System.Xml;
using System.Xml.Linq;
using static BlackboxServer.UDP_Server;


namespace BlackboxServer
{
    public class MTCData
    {      
        public struct MTCdata
        {
            //data needed for query to get data from Mtconnect
            public string ServerIP { get; set; }
            public string MTConnectAddress { get; set; }
            public string MTConnectPath { get; set; }

            // data from mtconnect
            public string Mode { get; set; }
            public string Mode2 { get; set; } // head2
            public string Execution { get; set; }
            public string Execution2 { get; set; } // head2
            public string MachineState { get; set; } // found using mode and execution (head 1)
            public string MachineState2 { get; set; } // for head 2
            public string PartCount { get; set; } // counter for head 1
            public string PartCount2 { get; set; } // counter for head 2

            // data for sap
            public string ConfirmationNumber { get; set; } // the id 
            public string ProductionOrder { get; set; } 
            public string Operation { get; set; }
            public string Material { get; set; }
            public string PartCountTarget { get; set; }

            public float IdealCycleTime;
            public float IdealLoadTime;

            public float baseQuality;
            public string UnitOfMeasure;

            public float StdValue1;
            public string StdValue1Unit;
            public float StdValue2;
            public string StdValue2Unit;
            public float StdValue3;
            public string StdValue3Unit;
            public float StdValue4;
            public string StdValue4Unit;

            public Amadadata amada;
            public BDtronicdata bdtronicdata;
        }

        // data used to make sure 2105 is actually running and not just left on "run" mode
        public struct RunningCheckData
        {
                public string prevpart;
                public DateTime LastPartcompleted;
                public bool ActuallyRunning; 
        }

        private static RunningCheckData RunCheck(RunningCheckData runningCheckData, string partnumber)
        {
           if (partnumber != runningCheckData.prevpart) // a part has been completed
           {
               runningCheckData.LastPartcompleted = DateTime.Now;
               runningCheckData.prevpart= partnumber;
           }

           if (DateTime.Now < runningCheckData.LastPartcompleted.AddHours(1)) // its been an hour since the last part was completed
           {
               runningCheckData.ActuallyRunning = true;
           }
           else
           {
               runningCheckData.ActuallyRunning = false;
           }

           return runningCheckData;
        }
        

        public static RunningCheckData Runningcheckdata = new();

        public struct Amadadata
        {
            public string machinestatus;
        }

        public struct BDtronicdata
        {
            //MTConnect
            public short estop; // This is primarily for telling if the machine is down.

            public short Automatic;
            public short Manual;
            public short Red;
            public short Green;
            public short Yellow;

            public short H1Position;
            public short H2Position;

            public short H1PartComplete; // becomes 0 when a part is starting and becomes 1 when a part is finished
            public short H2PartComplete; // becomes 0 when a part is starting and becomes 1 when a part is finished

            public short prevH1PartComplete; // from tracking the change for a partcount
            public short prevH2PartComplete;

            public short H1ChamberState;
            public short H2ChamberState;

            public int ActSpeed; // The speed the machine is moving

            public short HandPress; // twoHandPressed is 1 when the operator is pressing it

            // variables for figuring out an internal partcount
          // public string prevConfNumber; // this is for getting an accurate part count
            //public int partcountInt; // for calculating the partcount
            public bool partstart;
        }

        // grabs part number from the mtconnect xml file for machines that do not have an actual part count in MTConnect
        /*private static int FindPartCount(string xmlname) 
        {
            int partcount;
            string URLString = "BlackBox_XML/"+xmlname;
            XmlTextReader reader = new XmlTextReader(URLString);
            string ItemId = "";
            try
            {
                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element: // The node is an element.
                                                   //Console.Write("<" + reader.Name);

                            while (reader.MoveToNextAttribute())
                            {
                                if (reader.Name=="PC1")
                                {
                                   // Console.Write(" " + reader.Name + "='" + reader.Value + "'");
                                    ItemId = reader.Value;
                                    
                                }
                            }
                             // Console.Write(" " + reader.Name + "='" + reader.Value + "'");
                            //  Console.Write(">");
                            // Console.WriteLine(">");
                            break;
                        case XmlNodeType.Text: //Display the text in each element.


                           // Console.WriteLine(reader.Value);
                            break;
                        case XmlNodeType.EndElement: //Display the end of the element.
                            //Console.Write("</" + reader.Name);
                           // Console.WriteLine(">");
                            break;
                    }
                }

                partcount = int.Parse(ItemId);
            }
            catch
            {
                Console.WriteLine("Reading XML Failed");
                partcount = 0;
            }
            reader.Close();
            return partcount;
        }
        */

        public static MTCdata HTTPget(MTCdata data, int Heads, string type, string ConformationNumber)
        {
            string URLaddress = data.MTConnectAddress + data.MTConnectPath;
            string ItemId = "";
            XmlTextReader reader;
            
            bool offline = false;

            try
                {
                    reader = new(URLaddress);
                    //Console.WriteLine(URLaddress);
                   while (reader.Read())
                   {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:

                            
                            if (type == "amada" || type == "direct") // with the bandpi we cheat and use the tag instead of the dataID
                            {
                                //Console.WriteLine(reader.Name + " " + reader.Value);
                                switch (reader.Name)
                                {
                                    case "MachineStatus":
                                        ItemId = "machinestatus";
                                        break;
                                    case "ControllerMode":
                                        ItemId = "mode";
                                        break;
                                    case "Execution":
                                        ItemId = "execution";
                                        break;
                                    case "PartCount":
                                        ItemId = "PartCountAct";
                                        break;
                                    default:
                                        ItemId = "";
                                        break;
                                }

                            }
                            else
                            {
                                while (reader.MoveToNextAttribute())
                                {
                                    if (reader.Name=="dataItemId")
                                    {
                                        ItemId = reader.Value;
                                    }
                                }
                            }
                                break;
                            case XmlNodeType.Text:
                            
                            // Console.WriteLine(ItemId + "   " + reader.Value); // for debug
                                switch (type)
                                {
                                    case "amada":
                                    
                                        switch (ItemId)
                                        {
                                            case "machinestatus":
                                                data.amada.machinestatus = reader.Value;
                                                break;
                                            case "mode":
                                                data.Mode = reader.Value;
                                                break;
                                            case "execution":
                                                data.Execution = reader.Value;
                                                break;
                                            case "PartCountAct":
                                                data.PartCount = reader.Value;
                                                data.PartCount2 = reader.Value;
                                                break;
                                            
                                        }
                                        break;
                                case "direct":

                                    switch (ItemId)
                                    {
                                        case "machinestatus":
                                            data.amada.machinestatus = reader.Value;
                                            break;
                                        case "mode":
                                            data.Mode = reader.Value;
                                            break;
                                        case "execution":
                                            data.Execution = reader.Value;
                                            break;
                                        case "PartCountAct":
                                            data.PartCount = reader.Value;
                                            data.PartCount2 = reader.Value;
                                            break;
                                    }
                                    break;
                                case "doosan": // doosan syntax
                                        switch (ItemId)
                                        {
                                            case "mode":
                                                data.Mode = reader.Value;
                                                data.Mode2= reader.Value;
                                                break;
                                           // case "mode2":
                                            //    data.Mode2 = reader.Value;
                                           //     break;
                                            case "path1_execution":
                                                data.Execution = reader.Value;
                                                break;
                                            case "path2_execution":
                                                data.Execution2 = reader.Value;
                                                break;
                                            case "path1_part_count":
                                                data.PartCount = reader.Value;
                                                break;
                                            case "path2_part_count":
                                                data.PartCount2 = reader.Value;
                                                break;
                                        }
                                        break;
                                    case "haas": // haas syntax
                                        switch (ItemId)
                                        {
                                            case "mode":
                                                data.Mode = reader.Value;
                                                break;
                                            case "execution":
                                                data.Execution = reader.Value;
                                                break;
                                            case "Count1":
                                                data.PartCount = reader.Value;
                                                break;
                                            case "Count2":
                                                data.PartCount2 = reader.Value;
                                                break;
                                            case "PartCountTarget":
                                                data.PartCountTarget = reader.Value;
                                                break;
                                        }
                                        break;
                                    case "smooth": // mazak smooth 
                                        switch (ItemId)
                                        {
                                            case "mode":
                                                data.Mode = reader.Value;
                                                break;
                                            case "execution":
                                                data.Execution = reader.Value;
                                                break;
                                            case "PartCountAct":
                                                data.PartCount = reader.Value;
                                                break;
                                            case "PartCountAct2":
                                                data.PartCount2 = reader.Value;
                                                break;
                                            case "PartCountTarget":
                                                data.PartCountTarget = reader.Value;
                                                break;
                                            case "mode2":
                                                data.Mode2 = reader.Value;
                                                break;
                                            case "execution2":
                                                data.Execution2 = reader.Value;
                                                break;
                                        }
                                        break;
                                    case "matrix": // 2280
                                        switch (ItemId)
                                        {
                                            case "mode":
                                                data.Mode = reader.Value;
                                                break;
                                            case "exec":
                                                data.Execution = reader.Value;
                                                break;
                                            case "mode2":
                                                data.Mode2 = reader.Value;
                                                break;
                                            case "exec2":
                                                data.Execution2 = reader.Value;
                                                break;
                                            case "pc":
                                                data.PartCount = reader.Value;
                                                break;
                                            case "pc2":
                                                data.PartCount2 = reader.Value;
                                                break;
                                            case "pct":
                                                data.PartCountTarget = reader.Value;
                                                break;
                                        }

                                        break;
                                    case "bdtronic": // glue machine

                                        switch(ItemId)
                                        {
                                            case "ifHW_eStopOk":
                                                try
                                                {
                                                    data.bdtronicdata.estop = short.Parse(reader.Value);
                                                }
                                                catch
                                                {
                                                    offline = true;
                                                }
                                                break;
                                            case "Automatic":
                                                try
                                                {
                                                    data.bdtronicdata.Automatic = short.Parse(reader.Value);
                                                }
                                                catch 
                                                { 
                                                    offline = true;
                                                }
                                                break;
                                            case "Manual":
                                                try
                                                {
                                                    data.bdtronicdata.Manual = short.Parse(reader.Value);
                                                }
                                                catch 
                                                {
                                                    offline = true;
                                                }
                                                break;
                                            case "Green":
                                                try
                                                {
                                                    data.bdtronicdata.Green = short.Parse(reader.Value);
                                                }
                                                catch
                                                {
                                                    offline = true;
                                                }
                                                break;
                                            case "Yellow":
                                                try
                                                {
                                                    data.bdtronicdata.Yellow = short.Parse(reader.Value);
                                                }
                                                catch
                                                {
                                                    offline = true;
                                                }
                                                break;
                                            case "Red":
                                                try
                                                {
                                                    data.bdtronicdata.Red = short.Parse(reader.Value);
                                                }
                                                catch
                                                {
                                                    offline = true;
                                                }
                                                break;
                                            case "twoHandPressed":
                                                try
                                                {
                                                    data.bdtronicdata.HandPress = short.Parse(reader.Value);
                                                }
                                                catch { }
                                                break;
                                            case "actspeed":
                                                try
                                                {
                                                    data.bdtronicdata.ActSpeed = int.Parse(reader.Value);
                                                }
                                                catch { }
                                                break;
                                            case "FinishPartIfMatNOK_H1":
                                                try
                                                {
                                                    data.bdtronicdata.H1PartComplete = short.Parse(reader.Value);
                                                }
                                                catch { }
                                                break;
                                            case "FinishPartIfMatNOK_H2":
                                                try
                                                {
                                                    data.bdtronicdata.H2PartComplete = short.Parse(reader.Value);
                                                }
                                                catch { }
                                                break;
                                            case "ActualPosition_H1":
                                                try
                                                {
                                                    data.bdtronicdata.H1Position = short.Parse(reader.Value);
                                                }
                                                catch { }
                                                break;
                                            case "ActualPosition_H2":
                                                try
                                                {
                                                    data.bdtronicdata.H2Position = short.Parse(reader.Value);
                                                }
                                                catch { }
                                                break;
                                            case "Head1_ChamberState":
                                                try
                                                {
                                                    data.bdtronicdata.H1ChamberState = short.Parse(reader.Value);
                                                }
                                                catch { }
                                                break;
                                            case "Head2_ChamberState":
                                                try
                                                {
                                                    data.bdtronicdata.H2ChamberState = short.Parse(reader.Value);
                                                }
                                                catch { }
                                                break;
                                        }

                                        break;

                                    default: 
                                        switch (ItemId)
                                        {
                                            case "mode":
                                                data.Mode = reader.Value;
                                                break;
                                            case "mode2":
                                                data.Mode2 = reader.Value;
                                                break;
                                            case "exec":
                                                data.Execution = reader.Value;
                                                break;
                                            case "exec2":
                                                data.Execution2 = reader.Value;
                                                break;
                                            case "pc":
                                                data.PartCount = reader.Value;
                                                break;
                                            case "pc2":
                                                data.PartCount2 = reader.Value;
                                                break;
                                            case "pct":
                                                data.PartCountTarget = reader.Value;
                                                break;
                                        }
                                        break;
                                }


                                break;
                            case XmlNodeType.EndElement: //Display the end of the element.
                                break;

                            default:

                                break;

                    }
                   }
                }
                catch
                {
                    data.MachineState = "OFFLINE";
                    offline = true;
                }


           if (type == "bdtronic")
           {
               //! Machine state
                    if (data.bdtronicdata.Manual == 1)
                    {
                        data.MachineState = "MANUAL";
                    }
                    else if (data.bdtronicdata.Red == 1 || data.bdtronicdata.estop == 0)
                    {
                        data.MachineState = "OFFLINE";
                    }
                    else if (data.bdtronicdata.H1Position > 2 && data.bdtronicdata.H2Position > 2) // The machine is Glueing
                    {
                        data.MachineState = "RUNNING";
                    }
                    else if(offline)
                    {
                        data.MachineState = "OFFLINE";
                    }
                    else // Machine is either idle or being loaded
                    {
                        data.MachineState = "IDLE"; // should be replaced with the idle timer function
                    }
                //! Machine state

                //! partcount

                    string xml_name = "PCAssist";

                    // creates the xml to prevent an error 
                    if (File.Exists("BlackBox_XML/" + xml_name + ".xml") != true || new FileInfo("BlackBox_XML/" + xml_name + ".xml").Length ==0) // see if the xml file exists
                    {
                        XML_Builder.PartCountAssistXML(xml_name, "0", "init"); // save the partcount and prev confirmation number data.
                    }

                    (int part, string prevConfNumber)= XML_Builder.FindPartCount(xml_name); // grab the previous part value from the xml file

               // Console.WriteLine(ConformationNumber +": " +  prevConfNumber);

                    if (ConformationNumber  != prevConfNumber && ConformationNumber != "" && ConformationNumber != null) // a new job has been started on the machine
                    {
                        part = 0; // reset part count
                        prevConfNumber = ConformationNumber;
                    }

                    if (data.MachineState == "RUNNING" && data.bdtronicdata.partstart == false) // get ready to count the part once the machine is running
                    {
                        data.bdtronicdata.partstart = true;
                    }
                    else if(data.bdtronicdata.partstart && data.MachineState != "RUNNING") // count the part once it is completed
                    {
                       part++;
                       data.bdtronicdata.partstart = false;
                    }
               
                data.PartCount = part.ToString();
                data.PartCount2 = data.PartCount; // the glue machine doesnt do multiple parts at the same time.

                XML_Builder.PartCountAssistXML(xml_name, data.PartCount, prevConfNumber); // save the partcount and prev confirmation number data.
                //! partcount
            }
            else if (type == "amada" || type == "direct")
            {
                
                if (data.amada.machinestatus == "OFF")
                {
                    data.MachineState = "OFFLINE";
                }
                else
                {
                    if (data.Mode == "MANUAL" || data.Mode =="MANUAL_DATA_INPUT")
                    {
                        data.MachineState = "MANUAL";
                    }
                    else // mode is automatic, semiautomatic or other 
                    {
                        switch (data.Execution)
                        {
                            case "ACTIVE":
                                data.MachineState = "RUNNING";
                                break;
                            case "READY":
                                data.MachineState = "IDLE";
                                break;
                            default:
                                data.MachineState = "IDLE";
                                break;
                        }
                    }
                }

            }
            else
            {
                // read head 1
                if (Heads == 1)
                {
                    if (data.Mode == "MANUAL" || data.Mode =="MANUAL_DATA_INPUT")
                    {
                        data.MachineState = "MANUAL";
                    }
                    else // mode is automatic, semiautomatic or other 
                    {
                        switch (data.Execution)
                        {
                            case "ACTIVE":
                                if (type == "haas")
                                {
                                    Runningcheckdata = RunCheck(Runningcheckdata, data.PartCount);
                                    if (Runningcheckdata.ActuallyRunning)
                                    {
                                        data.MachineState = "RUNNING";
                                    }
                                    else
                                    {
                                        data.MachineState = "IDLE"; // the machine is in the run state but not parts are being completed
                                    }
                                }
                                else
                                {
                                    data.MachineState = "RUNNING";
                                }
                                break;
                            case "READY":
                                data.MachineState = "IDLE";
                                break;
                            case "WAIT":
                                data.MachineState = "IDLE";
                                break;
                            case "FEED_HOLD":
                                data.MachineState = "FEED_HOLD";
                                break;
                            case "STOPPED":
                                data.MachineState = "PGM_STOP";
                                break;
                            case "INTERRUPTED":
                                data.MachineState = "INTERUPT";
                                break;
                            case "PROGRAM_STOPPED":
                                data.MachineState = "PGM_STOP";
                                break;
                            case "OPTIONAL_STOP":
                                data.MachineState = "PGM_STOP";
                                break;
                            default: // machine down
                                data.MachineState = "OFFLINE";
                                break;
                        }
                    }

                }
                else if (Heads == 2)
                {
                    if (data.Mode == "MANUAL" || data.Mode =="MANUAL_DATA_INPUT")
                    {
                        data.MachineState = "MANUAL";
                    }
                    else // mode is automatic, semiautomatic or other 
                    {
                        //Console.WriteLine(data.Execution);
                        switch (data.Execution)
                        {
                            case "ACTIVE":
                                data.MachineState = "RUNNING";
                                break;
                            case "READY":
                                data.MachineState = "IDLE";
                                break;
                            case "WAIT":
                                data.MachineState = "IDLE";
                                break;
                            case "FEED_HOLD":
                                data.MachineState = "FEED_HOLD";
                                break;
                            case "STOPPED":
                                data.MachineState = "PGM_STOP";
                                break;
                            case "INTERRUPTED":
                                data.MachineState = "INTERUPT";
                                break;
                            case "PROGRAM_STOPPED":
                                data.MachineState = "PGM_STOP";
                                break;
                            case "OPTIONAL_STOP":
                                data.MachineState = "PGM_STOP";
                                break;
                            default: // machine down
                                data.MachineState = "OFFLINE";
                                break;

                        }
                    }
                    // read head 2
                    if (data.Mode2 == "MANUAL" || data.Mode2 =="MANUAL_DATA_INPUT")
                    {
                            data.MachineState2 = "MANUAL";
                    }
                    else // mode is automatic, semiautomatic or other 
                    {
                            //Console.WriteLine(data.Execution);
                        switch (data.Execution2)
                        {
                            case "ACTIVE":
                                data.MachineState2 = "RUNNING";
                                break;
                            case "READY":
                                data.MachineState2 = "IDLE";
                                break;
                            case "WAIT":
                                data.MachineState2 = "IDLE";
                                break;
                            case "FEED_HOLD":
                                data.MachineState2 = "FEED_HOLD";
                                break;
                            case "STOPPED":
                                data.MachineState2 = "PGM_STOP";
                                break;
                            case "INTERRUPTED":
                                data.MachineState2 = "INTERUPT";
                                break;
                            case "PROGRAM_STOPPED":
                                data.MachineState2 = "PGM_STOP";
                                break;
                            case "OPTIONAL_STOP":
                                data.MachineState2 = "PGM_STOP";
                                break;
                            default: // machine down
                                data.MachineState2 = "OFFLINE";
                                break;
                            
                        }
                    }
                }
            }

            return data; 
        }

    }
}
