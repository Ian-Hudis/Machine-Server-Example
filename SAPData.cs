using System.Net;
using System.Net.Security;
using System.Text;
using static BlackboxServer.UDP_Server;

namespace BlackboxServer
{
    public class SAP
    {
        public struct Data 
        {
            public string ProductOrder;
            public string material;
            public float OrderQuanity;
            public float IdealCycleTime;
            public float IdealLoadTime;

            public string confirmationorder;
            public string operationNumber;

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
            public string machid;


            // new content 7-16-25
            public float ConfirmedYield;
            public float ScrappedP;

        }

        public static Data GrabSAPData(string confirmationorder)
        {
            string prodorder = GetProdOrder(confirmationorder); // finds the production order
            Data sap = CallSap(prodorder, confirmationorder); // finds the rest of the sap data
            sap = GetYieldData(prodorder, sap);
            /*
            Console.WriteLine("\n" + sap.ProductOrder + "\n" + sap.confirmationorder + "\n" + sap.operationNumber + "\n" + sap.material+ "\n" + sap.OrderQuanity
            + "\n" + sap.machid+ "\n" + sap.IdealCycleTime+ "\n"+ sap.IdealLoadTime + "\n" + sap.UnitOfMeasure +
            "\n" + sap.StdValue1 + " " +sap.StdValue1Unit + "\n" + sap.StdValue2 + " " +sap.StdValue2Unit + "\n" + sap.StdValue3 + " " +sap.StdValue3Unit + "\n" + sap.StdValue4 + " " +sap.StdValue4Unit +
            "\n" + sap.ConfirmedYield + "\n" + sap.ScrappedP);
            */
            return sap;
        }

        // new content 7-16-25
        private static Data GetYieldData(string ProductionOrder, Data sapdata)
        {

#pragma warning disable SYSLIB0014 // Type or member is obsolete
            var request = (HttpWebRequest)WebRequest.Create("http://SAP_Address:8000/sap/zrfc?sap-language=&sap-client=010&class=ZCL_HTTP_READ_ORDER_CONFIRM");
#pragma warning restore SYSLIB0014 // Type or member is obsolete
            request.Credentials = new NetworkCredential("network-rfc", "rfc-Id");
            request.CookieContainer = new CookieContainer();
            request.Method = "POST";


#pragma warning disable CS8622
            ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback(ValidateServerCertificate);
#pragma warning restore CS8622
            string postBodyValue = "<?xml version=\"1.0\" encoding=\"utf-8\"?>";
            postBodyValue += "<import>";
            postBodyValue += "<para>";
            postBodyValue += "<AUFNR>"+ProductionOrder+"</AUFNR>"; //Parameter for searched Value, accepting serial number, order number or material number
            postBodyValue += "</para>";
            postBodyValue += "</import>";

            byte[] postBytes = Encoding.UTF8.GetBytes(postBodyValue);

            request.ContentType = "application/xml";
            request.ContentLength = postBytes.Length;

            (request).KeepAlive = true;
            (request).PreAuthenticate = true;

            Stream dataStream = request.GetRequestStream();
            dataStream.Write(postBytes, 0, postBytes.Length);
            dataStream.Close();

            WebResponse response = request.GetResponse();
            dataStream = response.GetResponseStream();
            StreamReader reader = new(dataStream);

            string xmlReturn = reader.ReadToEnd(); // grab the SAP data and put it into a string
                                                   // Console.WriteLine(xmlReturn);

            string[] result = xmlReturn.Split(new[] { '\r', '\n' }); // split string into lines

            char[] delimiterChars = { '<', '>' };


            string opnum = ""; // operation
            string ConfirmY = ""; // yield confirmed
            string Scrap = ""; // yield scrapped
            string BQM = ""; // base quanitity of measure
            string QM = ""; // quantity of measure

            // parse data
            foreach (string resultItem in result)
            {
                string[] text = resultItem.Split(delimiterChars);

                if (text.Length > 2)
                {
                    //Console.WriteLine(text[1] + ": " + text[2]);
                    switch (text[1])
                    {
                        case "vornr": // operation
                            opnum = text[2];
                            //  Console.WriteLine(opnum);
                            break;
                        case "lmnga": // yield confirmed
                            ConfirmY = text[2];
                            //  Console.WriteLine(ConfirmY);
                            break;
                        case "xmnga": //  yield scrapped
                            Scrap = text[2];
                            //  Console.WriteLine(Scrap);
                            break;
                        case "gmein":
                            BQM = text[2]; // base quanitity of measure
                                           //  Console.WriteLine(BQM);
                            break;
                        case "meinh":
                            QM = text[2];
                            //   Console.WriteLine(QM);
                            break;

                    }
                }
            }

            if (opnum == sapdata.operationNumber)
            {
                try
                {
                    sapdata.ConfirmedYield = float.Parse(ConfirmY);
                    sapdata.ScrappedP = float.Parse(Scrap);

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    sapdata.ConfirmedYield = 0;
                    sapdata.ScrappedP = 0;
                }

            }



            return sapdata;
        }


        private static string GetProdOrder(string ConfN)
        {
            string ProdOrder = "";

            var request = (HttpWebRequest)WebRequest.Create("http://as-dehq-erp-p.prod.local:8000/sap/zrfc?sap-client=010&class=ZCL_HTTP_GET_ORDER_FROM_CONFI");
            request.Credentials = new NetworkCredential("5010-rfc", "rfc5010KebAm");
            request.CookieContainer = new CookieContainer();
            request.Method = "POST";

            ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback(ValidateServerCertificate);

            string postBodyValue = "<?xml version=\"1.0\" encoding=\"utf-8\"?>";
            postBodyValue += "<import>";
            postBodyValue += "<para>";
            postBodyValue += "<CONFIRMATION>" + ConfN + "</CONFIRMATION>"; //The Confirmation Number of the Order you want to find
            postBodyValue += "</para>";
            postBodyValue += "</import>";

            byte[] postBytes = Encoding.UTF8.GetBytes(postBodyValue);

            request.ContentType = "application/xml";
            request.ContentLength = postBytes.Length;

            ((HttpWebRequest)(request)).KeepAlive = true;
            ((HttpWebRequest)(request)).PreAuthenticate = true;

            Stream dataStream = request.GetRequestStream();
            dataStream.Write(postBytes, 0, postBytes.Length);
            dataStream.Close();

            WebResponse response = request.GetResponse();
            dataStream = response.GetResponseStream();
            StreamReader reader = new(dataStream);

            var xmlReturn = reader.ReadToEnd();

            // Console.WriteLine(xmlReturn);

            string[] result = xmlReturn.Split(['\r', '\n']); // split string into lines

            char[] delimiterChars = [ '<', '>' ];
            if (xmlReturn.Length>198)
            {
                foreach (string resultItem in result)
                {
                    string[] text = resultItem.Split(delimiterChars);

                    if (text.Length > 2)
                    {
                        switch (text[1])
                        {
                            case "aufnr":
                                ProdOrder = text[2].Remove(0, 5);
                                break;
                        }
                        // Console.WriteLine(text[1] + ": " + text[2]);
                    }
                }
            }
            //Console.WriteLine(xmlReturn);

            return ProdOrder;
        }

        private static Data CallSap(string ProductionOrder, string confirmationOrder)
        {
            Data sapdata = new()
            {
                ProductOrder = ProductionOrder
            };

            var request = (HttpWebRequest)WebRequest.Create("http://as-dehq-erp-p.prod.local:8000/sap/zrfc?sap-client=010&class=ZCL_TSYSTEM_INTERFACE");
            request.Credentials = new NetworkCredential("5010-rfc", "rfc5010KebAm");
            request.CookieContainer = new CookieContainer();
            request.Method = "POST";

            #pragma warning disable CS8622
            ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback(ValidateServerCertificate);
            #pragma warning restore CS8622
            string postBodyValue = "<?xml version=\"1.0\" encoding=\"utf-8\"?>";
            postBodyValue += "<import>";
            postBodyValue += "<para>";
            postBodyValue += "<VALUE>"+ProductionOrder+"</VALUE>"; //Parameter for searched Value, accepting serial number, order number or material number
            postBodyValue += "<PLANT>5010</PLANT>";      //Plant where the information should be searched
            postBodyValue += "<LANG>EN</LANG>";          //Language for response texts
            postBodyValue += "<IGNORE_SOFTWARE>X</IGNORE_SOFTWARE>"; // Defines if software information is ignored. ATTENTION! SET TO "X" if you search for a magnet tech. order or serial number
            postBodyValue += "<IV_GET_BOM>X</IV_GET_BOM>";  //OPTIONAL: If set, the complete BOM will included to the answer XML
            postBodyValue += "<IV_GET_OPERATION_LTXT>X</IV_GET_OPERATION_LTXT>"; //OPTIONAL:If set, all operations + the texts will be included to the answer XML
            postBodyValue += "<IV_GET_MAINT_WORK_INST>X</IV_GET_MAINT_WORK_INST>"; // OPTIONAL:If set, maintenance work instructions will be included to the answer XML
            postBodyValue += "</para>";
            postBodyValue += "</import>";

            byte[] postBytes = Encoding.UTF8.GetBytes(postBodyValue);

            request.ContentType = "application/xml";
            request.ContentLength = postBytes.Length;

            (request).KeepAlive = true;
            (request).PreAuthenticate = true;

            Stream dataStream = request.GetRequestStream();
            dataStream.Write(postBytes, 0, postBytes.Length);
            dataStream.Close();

            WebResponse response = request.GetResponse();
            dataStream = response.GetResponseStream();
            StreamReader reader = new(dataStream);

            string xmlReturn = reader.ReadToEnd(); // grab the SAP data and put it into a string

            //Console.WriteLine(xmlReturn);

            string[] result = xmlReturn.Split(['\r', '\n']); // split string into lines

            char[] delimiterChars = [ '<', '>' ];

            string confnumber = "";
            string operation = "";

            // reafirm the confirmation number 
            if (ProductionOrder != "")
            {
                foreach (string resultItem in result)
                {
                    string[] text = resultItem.Split(delimiterChars);
                    if (text.Length > 2)
                    {
                        switch (text[1])
                        {
                            case "VORNR": // operation
                                operation = text[2];
                                // Console.WriteLine(operation);
                                break;

                            case "RUECK": // confirmation number
                                confnumber = text[2].Remove(0, 3);
                                //  Console.WriteLine(confnumber);

                                if (confirmationOrder == confnumber)
                                {
                                    sapdata.confirmationorder = confnumber;
                                    sapdata.operationNumber = operation;
                                }
                                break;

                        }
                    }
                }
            }

            // data parse            
            if (confirmationOrder == sapdata.confirmationorder) // check to make sure the scanned confirmation number exists
            {
                foreach (string resultItem in result)
                {
                    string[] text = resultItem.Split(delimiterChars);

                    if (text.Length > 2)
                    {
                        switch (text[1])
                        {
                            case "VORNR": // operation
                                operation = text[2];
                                break;
                            case "MATNR": // material number
                                sapdata.material = text[2];
                                break;
                            case "GAMNG": // order quantity
                                sapdata.OrderQuanity = float.Parse(text[2]);
                                break;
                            case "ARBPL": // machine id
                                if (operation == sapdata.operationNumber)
                                {
                                    sapdata.machid = text[2];
                                }
                                break;
                            case "VGW05": // info 1
                                if (operation == sapdata.operationNumber)
                                {
                                    sapdata.IdealCycleTime = float.Parse(text[2]);
                                }
                                break;
                            case "VGW06": // info 2
                                if (operation == sapdata.operationNumber)
                                {
                                    sapdata.IdealLoadTime = float.Parse(text[2]);
                                }
                                break;
                            case "BMSCH": // base qualtity
                                if (operation == sapdata.operationNumber)
                                {
                                    sapdata.baseQuality = float.Parse(text[2].Trim(' '));
                                }
                                break;
                            case "MEINH": // unit of measure for operation
                                if (operation == sapdata.operationNumber)
                                {
                                    sapdata.UnitOfMeasure = text[2];
                                }
                                break;
                            case "VGW01": // std unit1
                                if (operation == sapdata.operationNumber)
                                {
                                    sapdata.StdValue1 = float.Parse(text[2].Trim(' '));
                                }
                                break;
                            case "VGW02": // std unit2
                                if (operation == sapdata.operationNumber)
                                {
                                    sapdata.StdValue2 = float.Parse(text[2].Trim(' '));
                                }
                                break;
                            case "VGW03": // std unit3
                                if (operation == sapdata.operationNumber)
                                {
                                    sapdata.StdValue3 = float.Parse(text[2].Trim(' '));
                                }
                                break;
                            case "VGW04": // std unit4
                                if (operation == sapdata.operationNumber)
                                {
                                    sapdata.StdValue4 = float.Parse(text[2].Trim(' '));
                                }
                                break;
                            case "VGE01": // std unit1 units
                                if (operation == sapdata.operationNumber)
                                {
                                    sapdata.StdValue1Unit = text[2];
                                }
                                break;
                            case "VGE02": // std unit1 units
                                if (operation == sapdata.operationNumber)
                                {
                                    sapdata.StdValue2Unit = text[2];
                                }
                                break;
                            case "VGE03": // std unit1 units
                                if (operation == sapdata.operationNumber)
                                {
                                    sapdata.StdValue3Unit = text[2];
                                }
                                break;
                            case "VGE04": // std unit1 units
                                if (operation == sapdata.operationNumber)
                                {
                                    sapdata.StdValue4Unit = text[2];
                                }
                                break;
                        }
                         //Console.WriteLine(text[1] + ": " + text[2]);
                    }
                }
            }

            reader.Close();
            return sapdata;
        }

        private static bool ValidateServerCertificate(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate, System.Security.Cryptography.X509Certificates.X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }
}

