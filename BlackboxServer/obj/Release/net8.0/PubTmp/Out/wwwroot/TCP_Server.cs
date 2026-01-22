namespace BlackboxServer.wwwroot
{
    public class TCP_Server
    {

        public struct PLC_Data
        {
            public PLCrawInput RAWdata;

            public string Supervisor;
            public string Operator;
            public string ProductionOrder;
            public string Material;
            public string Sevent;
            public string Status; // Enable/Disable status
            public string Override;

            public string prevSupervisor;
            public string prevOperator;
            public string prevProductionOrder;
            public string prevMaterial;
            public string prevSevent;
            public string prevStatus;
            public string prevOverride;

            public bool DetectChange;
        }

    }
}
