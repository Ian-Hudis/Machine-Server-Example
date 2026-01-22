using System.Data;
using System.Reflection.PortableExecutable;
using System.Xml;
using System.Xml.Linq;
using static BlackboxServer.MTCData;
using static BlackboxServer.SAP;
using static BlackboxServer.UDP_Server;

namespace BlackboxServer
{
    /*
        public class XMLWriter
        {
            // State information used in the task.
            private static DataSet? xml_data;
            private static DataSet? MT_XML;
            private static string? PLCID;

            // The constructor obtains the state information.
            public XMLWriter(DataSet Input1, DataSet Input2, string plcID)
            {
                xml_data = Input1;
                MT_XML = Input2;
                PLCID=plcID;
            }

            public static void Xportthread()
            {
                var builder = WebApplication.CreateBuilder();

                var app = builder.Build();

                // Configure the HTTP request pipeline.
                if (!app.Environment.IsDevelopment())
                {
                    app.UseExceptionHandler("/Error");
                    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                    app.UseHsts();
                }

                app.UseHttpsRedirection();
                app.UseStaticFiles();

                app.MapGet(PLCID +"/", () => xml_data.GetXml()); // advertise plc data
                app.MapGet("/", () =>  MT_XML.GetXml()); // advertise mt connect data
                app.RunAsync();
            }

        }
    */
        public class XML_Builder
        {
            public struct Blkbox
            {
                //public PLC_Listener.PLC_Data plc;
                public int port;
                public string Location;
                public string ip;
                public XmlDocument XMLdoc;
                public XmlDocument Mtdoc;
            }

           // private static DataSet MTdata = new();
           // private static DataSet PLCdata = new();

        /*
            public static void UploadPage(DataSet mtData, DataSet kioskdata, string ID)
            {
             
                    MTdata = mtData;
                    PLCdata = kioskdata;
               
                XMLWriter xmlWriter = new(PLCdata, MTdata, ID);
                Thread xThread = new(new ThreadStart(XMLWriter.Xportthread)); // setup the posting thread
                xThread.Start(); // posts the xml file to the server
            }
        */
            public static void UpdateXML(/*TCPData Data*/ UDPData Data, string XML_Title, XmlDocument xmlDoc, string version)
            {
                xmlDoc.RemoveAll(); // clear the xml file
                XmlNode rootNode = xmlDoc.CreateElement(XML_Title);
                xmlDoc.AppendChild(rootNode);

                XmlNode timeNode = xmlDoc.CreateElement("Time");
                XmlAttribute time_attribute = xmlDoc.CreateAttribute("Update");
                time_attribute.Value = version + ": " +  DateTime.Now.ToString();
                timeNode.Attributes.Append(time_attribute);
                timeNode.InnerText = "Last_Updated";
                rootNode.AppendChild(timeNode);

                XmlNode supNode = xmlDoc.CreateElement("PLC_Entry");
                XmlAttribute Sup_attribute = xmlDoc.CreateAttribute("SUP");
                Sup_attribute.Value = Data.Supervisor;
                supNode.Attributes.Append(Sup_attribute);
                supNode.InnerText = "supervisor";
                rootNode.AppendChild(supNode);

                XmlNode operNode = xmlDoc.CreateElement("PLC_Entry");
                XmlAttribute Operator_attribute = xmlDoc.CreateAttribute("Oper");
                Operator_attribute.Value = Data.Operator;
                operNode.Attributes.Append(Operator_attribute);
                operNode.InnerText = "operator";
                rootNode.AppendChild(operNode);

                XmlNode supNodeCN = xmlDoc.CreateElement("PLC_Entry");
                XmlAttribute Sup_attributeCN = xmlDoc.CreateAttribute("CN");
                Sup_attributeCN.Value = Data.conf_number;
                supNodeCN.Attributes.Append(Sup_attributeCN);
                supNodeCN.InnerText = "ConfirmationNumber";
                rootNode.AppendChild(supNodeCN);

                XmlNode Prod_Order_Node = xmlDoc.CreateElement("SAP");
                XmlAttribute Prod_Order_attribute = xmlDoc.CreateAttribute("Prod_Order");
                Prod_Order_attribute.Value = Data.ProductionOrder;
                Prod_Order_Node.Attributes.Append(Prod_Order_attribute);
                Prod_Order_Node.InnerText = "production_order";
                rootNode.AppendChild(Prod_Order_Node);

                XmlNode Operation_Node = xmlDoc.CreateElement("SAP");
                XmlAttribute Operation_attribute = xmlDoc.CreateAttribute("Operation");
                Operation_attribute.Value = Data.oper_number;
                Operation_Node.Attributes.Append(Operation_attribute);
                Operation_Node.InnerText = "Operation";
                rootNode.AppendChild(Operation_Node);

                XmlNode Material_Node = xmlDoc.CreateElement("SAP");
                XmlAttribute Material_attribute = xmlDoc.CreateAttribute("Material");
                Material_attribute.Value = Data.Material;
                Material_Node.Attributes.Append(Material_attribute);
                Material_Node.InnerText = "material";
                rootNode.AppendChild(Material_Node);

                XmlNode MT_PartTarget_Node = xmlDoc.CreateElement("SAP");
                XmlAttribute MT_PartTarget_attribute = xmlDoc.CreateAttribute("TargetPartCount");
                MT_PartTarget_attribute.Value = Data.PartTargetstring;
                MT_PartTarget_Node.Attributes.Append(MT_PartTarget_attribute);
                MT_PartTarget_Node.InnerText = "targetpartcount";
                rootNode.AppendChild(MT_PartTarget_Node);

                XmlNode supNodeCT = xmlDoc.CreateElement("SAP");
                XmlAttribute Sup_attributeCT = xmlDoc.CreateAttribute("CT");
                Sup_attributeCT.Value = Data.CT;
                supNodeCT.Attributes.Append(Sup_attributeCT);
                supNodeCT.InnerText = "IdealCycleTime";
                rootNode.AppendChild(supNodeCT);

                XmlNode supNodeLT = xmlDoc.CreateElement("SAP");
                XmlAttribute Sup_attributeLT = xmlDoc.CreateAttribute("LT");
                Sup_attributeLT.Value = Data.LT;
                supNodeLT.Attributes.Append(Sup_attributeLT);
                supNodeLT.InnerText = "IdealLoadTime";
                rootNode.AppendChild(supNodeLT);

                XmlNode supNodeBQ = xmlDoc.CreateElement("SAP");
                XmlAttribute Sup_attributeBQ = xmlDoc.CreateAttribute("BQ");
                Sup_attributeBQ.Value = Data.base_quantity;
                supNodeBQ.Attributes.Append(Sup_attributeBQ);
                supNodeBQ.InnerText = "BaseQuantity";
                rootNode.AppendChild(supNodeBQ);

                XmlNode supNodeSetup = xmlDoc.CreateElement("SAP");
                XmlAttribute Sup_attributeSetup = xmlDoc.CreateAttribute("Setup");
                Sup_attributeSetup.Value = Data.SAP_Setup;
                supNodeSetup.Attributes.Append(Sup_attributeSetup);
                supNodeSetup.InnerText = "SAP_Setup_Time";
                rootNode.AppendChild(supNodeSetup);

                XmlNode supNodeMCT = xmlDoc.CreateElement("SAP");
                XmlAttribute Sup_attributeMCT = xmlDoc.CreateAttribute("MCT");
                Sup_attributeMCT.Value = Data.MachineCycletime;
                supNodeMCT.Attributes.Append(Sup_attributeMCT);
                supNodeMCT.InnerText = "MachineCycletime";
                rootNode.AppendChild(supNodeMCT);

                XmlNode Status_Node = xmlDoc.CreateElement("Mode");
                XmlAttribute Status_attribute = xmlDoc.CreateAttribute("Status");
                Status_attribute.Value = Data.Status;
                Status_Node.Attributes.Append(Status_attribute);
                Status_Node.InnerText = "mode";
                rootNode.AppendChild(Status_Node);

                XmlNode Event_Node = xmlDoc.CreateElement("Event");
                XmlAttribute Event_attribute = xmlDoc.CreateAttribute("Event");
                Event_attribute.Value = Data.Sevent;
                Event_Node.Attributes.Append(Event_attribute);
                Event_Node.InnerText = "event";
                rootNode.AppendChild(Event_Node);

                XmlNode Override_Node = xmlDoc.CreateElement("Override");
                XmlAttribute Override_attribute = xmlDoc.CreateAttribute("Override");
                Override_attribute.Value = Data.Override;
                Override_Node.Attributes.Append(Override_attribute);
                Override_Node.InnerText = "override";
                rootNode.AppendChild(Override_Node);

                XmlNode MT_State_Node = xmlDoc.CreateElement("MT_Connect");
                XmlAttribute MT_State_attribute = xmlDoc.CreateAttribute("MachineState");
                MT_State_attribute.Value = Data.MachineState;
                MT_State_Node.Attributes.Append(MT_State_attribute);
                MT_State_Node.InnerText = "CurrentState";
                rootNode.AppendChild(MT_State_Node);

                XmlNode MT_State_Node2 = xmlDoc.CreateElement("MT_Connect");
                XmlAttribute MT_State_attribute2 = xmlDoc.CreateAttribute("MachineState2");
                MT_State_attribute2.Value = Data.MachineState2;
                MT_State_Node2.Attributes.Append(MT_State_attribute2);
                MT_State_Node2.InnerText = "CurrentState";
                rootNode.AppendChild(MT_State_Node2);

                XmlNode MT_Part_Node = xmlDoc.CreateElement("MT_Connect");
                XmlAttribute MT_Part_attribute = xmlDoc.CreateAttribute("PartCount");
                MT_Part_attribute.Value = Data.Partstring;
                MT_Part_Node.Attributes.Append(MT_Part_attribute);
                MT_Part_Node.InnerText = "partcount";
                rootNode.AppendChild(MT_Part_Node);

                XmlNode MT_Part2_Node = xmlDoc.CreateElement("MT_Connect");
                XmlAttribute MT_Part2_attribute = xmlDoc.CreateAttribute("PartCount2");
                MT_Part2_attribute.Value = Data.Partstring2;
                MT_Part2_Node.Attributes.Append(MT_Part2_attribute);
                MT_Part2_Node.InnerText = "partcount2";
                rootNode.AppendChild(MT_Part2_Node);

                XmlNode opcommentnode = xmlDoc.CreateElement("PLC_SpecialEntry");
                XmlAttribute opcomment_attribute = xmlDoc.CreateAttribute("Comment");
                opcomment_attribute.Value = Data.OPComment;
                opcommentnode.Attributes.Append(opcomment_attribute);
                opcommentnode.InnerText = "comment";
                rootNode.AppendChild(opcommentnode);

                xmlDoc.Save("BlackBox_XML/"+ XML_Title +".xml"); // save the xml file

            }

            public static string FindConfirmationNum(string XML_Title , string confirmNumber)
            {
                string URLString = "BlackBox_XML/"+ XML_Title + ".xml";
                XmlTextReader reader = new (URLString);
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
                                    if (reader.Name=="CN")
                                    {
                                        // Console.Write(" " + reader.Name + "='" + reader.Value + "'");
                                        confirmNumber = reader.Value;
                                    }
                                }
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

                    
                }
                catch
                {
                    Console.WriteLine("Reading XML Failed");
                }
                    return confirmNumber;
            }

            public static void UpdateMTConnect_XML(/*TCPData TCPData,*/ UDPData UDPdata, MTCdata Data, Data SAPdata, string XML_Title, XmlDocument xmlDoc)
            {
                xmlDoc.RemoveAll(); // clear the xml file
                XmlNode rootNode = xmlDoc.CreateElement(XML_Title);
                xmlDoc.AppendChild(rootNode);

                XmlNode timeNode = xmlDoc.CreateElement("Time");
                XmlAttribute time_attribute = xmlDoc.CreateAttribute("Update");
                time_attribute.Value = DateTime.Now.ToString();
                timeNode.Attributes.Append(time_attribute);
                timeNode.InnerText = "Last_Updated";
                rootNode.AppendChild(timeNode);

                XmlNode supNode1 = xmlDoc.CreateElement("MTConnect");
                XmlAttribute Sup_attribute1 = xmlDoc.CreateAttribute("State");
                Sup_attribute1.Value = Data.MachineState;
                supNode1.Attributes.Append(Sup_attribute1);
                supNode1.InnerText = "state";
                rootNode.AppendChild(supNode1);

                XmlNode supNode1_2 = xmlDoc.CreateElement("MTConnect");
                XmlAttribute Sup_attribute1_2 = xmlDoc.CreateAttribute("State2");
                Sup_attribute1_2.Value = Data.MachineState2;
                supNode1_2.Attributes.Append(Sup_attribute1_2);
                supNode1_2.InnerText = "state";
                rootNode.AppendChild(supNode1_2);

                XmlNode supNode2 = xmlDoc.CreateElement("MTConnect");
                XmlAttribute Sup_attribute2 = xmlDoc.CreateAttribute("PC1");
                Sup_attribute2.Value = Data.PartCount;
                supNode2.Attributes.Append(Sup_attribute2);
                supNode2.InnerText = "PC1";
                rootNode.AppendChild(supNode2);

                XmlNode supNode3 = xmlDoc.CreateElement("MTConnect");
                XmlAttribute Sup_attribute3 = xmlDoc.CreateAttribute("PC2");
                Sup_attribute3.Value = Data.PartCount2;
                supNode3.Attributes.Append(Sup_attribute3);
                supNode3.InnerText = "PC2";
                rootNode.AppendChild(supNode3);
                // confirmation number
                XmlNode supNodeCN = xmlDoc.CreateElement("SAP");
                XmlAttribute Sup_attributeCN = xmlDoc.CreateAttribute("CN");
                Sup_attributeCN.Value = SAPdata.confirmationorder;
                supNodeCN.Attributes.Append(Sup_attributeCN);
                supNodeCN.InnerText = "ConfNumber";
                rootNode.AppendChild(supNodeCN);
                // machine id
                XmlNode supNodeLocation = xmlDoc.CreateElement("SAP");
                XmlAttribute Sup_attributeLocation = xmlDoc.CreateAttribute("ID");
                Sup_attributeLocation.Value = SAPdata.machid;
                supNodeLocation.Attributes.Append(Sup_attributeLocation);
                supNodeLocation.InnerText = "Location";
                rootNode.AppendChild(supNodeLocation);
                // production order
                XmlNode supNode4 = xmlDoc.CreateElement("SAP");
                XmlAttribute Sup_attribute4 = xmlDoc.CreateAttribute("PO");
                Sup_attribute4.Value = SAPdata.ProductOrder;
                supNode4.Attributes.Append(Sup_attribute4);
                supNode4.InnerText = "ProdOrder";
                rootNode.AppendChild(supNode4);
                //operation
                XmlNode supNodeOP = xmlDoc.CreateElement("SAP");
                XmlAttribute Sup_attributeOP = xmlDoc.CreateAttribute("OP");
                Sup_attributeOP.Value = SAPdata.operationNumber;
                supNodeOP.Attributes.Append(Sup_attributeOP);
                supNodeOP.InnerText = "Operation";
                rootNode.AppendChild(supNodeOP);
                //material
                XmlNode supNode5 = xmlDoc.CreateElement("SAP");
                XmlAttribute Sup_attribute5 = xmlDoc.CreateAttribute("Mat");
                Sup_attribute5.Value = SAPdata.material;
                supNode5.Attributes.Append(Sup_attribute5);
                supNode5.InnerText = "material";
                rootNode.AppendChild(supNode5);

                XmlNode supNode6 = xmlDoc.CreateElement("SAP");
                XmlAttribute Sup_attribute6 = xmlDoc.CreateAttribute("TPC");
                Sup_attribute6.Value = SAPdata.OrderQuanity.ToString();
                supNode6.Attributes.Append(Sup_attribute6);
                supNode6.InnerText = "TotalPartCount";
                rootNode.AppendChild(supNode6);
                // info 1
                XmlNode supNodeCT = xmlDoc.CreateElement("SAP");
                XmlAttribute Sup_attributeCT = xmlDoc.CreateAttribute("CT");
                Sup_attributeCT.Value = SAPdata.IdealCycleTime.ToString();
                supNodeCT.Attributes.Append(Sup_attributeCT);
                supNodeCT.InnerText = "IdealCycleTime";
                rootNode.AppendChild(supNodeCT);
                // info 2 IIOT
                XmlNode supNodeLT = xmlDoc.CreateElement("SAP");
                XmlAttribute Sup_attributeLT = xmlDoc.CreateAttribute("LT");
                Sup_attributeLT.Value = SAPdata.IdealLoadTime.ToString();
                supNodeLT.Attributes.Append(Sup_attributeLT);
                supNodeLT.InnerText = "IdealLoadTime";
                rootNode.AppendChild(supNodeLT);
                //Base Quantity
                XmlNode supNodeBQ = xmlDoc.CreateElement("SAP");
                XmlAttribute Sup_attributeBQ = xmlDoc.CreateAttribute("BQ");
                Sup_attributeBQ.Value = SAPdata.baseQuality.ToString();
                supNodeBQ.Attributes.Append(Sup_attributeBQ);
                supNodeBQ.InnerText = "BaseQuantity";
                rootNode.AppendChild(supNodeBQ);
                //Setup time
                XmlNode supNodeST = xmlDoc.CreateElement("SAP");
                XmlAttribute Sup_attributeST = xmlDoc.CreateAttribute("ST");
                Sup_attributeST.Value = SAPdata.StdValue1.ToString();
                supNodeST.Attributes.Append(Sup_attributeST);
                supNodeST.InnerText = "SetupTime";
                rootNode.AppendChild(supNodeST);
                // machinetime
                XmlNode supNodeMach = xmlDoc.CreateElement("SAP");
                XmlAttribute Sup_attributeMach = xmlDoc.CreateAttribute("Mach");
                Sup_attributeMach.Value = SAPdata.StdValue2.ToString();
                supNodeMach.Attributes.Append(Sup_attributeMach);
                supNodeMach.InnerText = "Machine";
                rootNode.AppendChild(supNodeMach);
                //machine calc
                XmlNode supNodeMC = xmlDoc.CreateElement("SAP");
                XmlAttribute Sup_attributeMC = xmlDoc.CreateAttribute("MC");
                Sup_attributeMC.Value = SAPdata.StdValue3.ToString();
                supNodeMC.Attributes.Append(Sup_attributeMC);
                supNodeMC.InnerText = "Machine_Calc";
                rootNode.AppendChild(supNodeMC);
                // fix costs
                XmlNode supNodefc = xmlDoc.CreateElement("SAP");
                XmlAttribute Sup_attributefc = xmlDoc.CreateAttribute("FC");
                Sup_attributefc.Value = SAPdata.StdValue4.ToString();
                supNodefc.Attributes.Append(Sup_attributefc);
                supNodefc.InnerText = "FixCosts";
                rootNode.AppendChild(supNodefc);

                XmlNode supNode7 = xmlDoc.CreateElement("EVENT");
                XmlAttribute Sup_attribute7 = xmlDoc.CreateAttribute("Event");
                // Sup_attribute7.Value = TCPData.Sevent;
                Sup_attribute7.Value = UDPdata.Sevent;
                supNode7.Attributes.Append(Sup_attribute7);
                supNode7.InnerText = "event";
                rootNode.AppendChild(supNode7);

                XmlNode Override_Node = xmlDoc.CreateElement("Override");
                XmlAttribute Override_attribute = xmlDoc.CreateAttribute("Override");
                //Override_attribute.Value = TCPData.Override;
                Override_attribute.Value = UDPdata.Override;
                Override_Node.Attributes.Append(Override_attribute);
                Override_Node.InnerText = "override";
                rootNode.AppendChild(Override_Node);

                // fix costs
                XmlNode partsyieldNode = xmlDoc.CreateElement("SAP");
                XmlAttribute Sup_partsyield = xmlDoc.CreateAttribute("PYield");
                Sup_partsyield.Value = SAPdata.ConfirmedYield.ToString();
                partsyieldNode.Attributes.Append(Sup_partsyield);
                partsyieldNode.InnerText = "PartsYield";
                rootNode.AppendChild(partsyieldNode);

            xmlDoc.Save("BlackBox_XML/" + XML_Title + ".xml"); // save the xml file
            }

            // for the glue machine
            public static ValueTuple<int, string> FindPartCount(string xmlname)
            {
            int partcount;
            string URLString = "BlackBox_XML/"+xmlname + ".xml";
            XmlTextReader reader = new(URLString);
            string partcountId = "";
            string confirmNumber = "";
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
                                    partcountId = reader.Value;

                                }
                                if (reader.Name=="CN")
                                {
                                    // Console.Write(" " + reader.Name + "='" + reader.Value + "'");
                                    confirmNumber = reader.Value;

                                }

                                if (reader.Name=="PartCount")
                                {
                                    // Console.Write(" " + reader.Name + "='" + reader.Value + "'");
                                    partcountId = reader.Value;

                                }
                                if (reader.Name=="ConfNumber")
                                {
                                    // Console.Write(" " + reader.Name + "='" + reader.Value + "'");
                                    confirmNumber = reader.Value;

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

                partcount = int.Parse(partcountId);
            }
            catch
            {
                Console.WriteLine("Reading XML Failed");
                partcount = 0;
            }
            reader.Close();
            return (partcount, confirmNumber);
        }
        // for the glue machine
            public static void PartCountAssistXML(string XML_Title, string PartCount, string prevConfNumber)
            {
                XmlDocument xmlDoc = new();

                xmlDoc.RemoveAll(); // clear the xml file
                XmlNode rootNode = xmlDoc.CreateElement("XML_Title");
                xmlDoc.AppendChild(rootNode);

                XmlNode PC_NodeLocation = xmlDoc.CreateElement("PartCount");
                XmlAttribute PC_attributeLocation = xmlDoc.CreateAttribute("PC1");
                PC_attributeLocation.Value = PartCount.ToString();
                PC_NodeLocation.Attributes.Append(PC_attributeLocation);
                PC_NodeLocation.InnerText = "PartCountReiterator";
                rootNode.AppendChild(PC_NodeLocation);

                XmlNode CN_NodeLocation = xmlDoc.CreateElement("ConfNumber");
                XmlAttribute CN_attributeLocation = xmlDoc.CreateAttribute("CN");
                CN_attributeLocation.Value = prevConfNumber;
                CN_NodeLocation.Attributes.Append(CN_attributeLocation);
                CN_NodeLocation.InnerText = "ConfirmNumbReiterator";
                rootNode.AppendChild(CN_NodeLocation);

                xmlDoc.Save("BlackBox_XML/" + XML_Title + ".xml"); // save the xml file

            }


        }
        
}



