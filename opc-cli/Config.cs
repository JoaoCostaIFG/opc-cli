using System.Collections.Generic;

namespace opc_cli
{
    internal class Config
    {
        public string progId { get; set; }
        public bool debug { get; set; } = false;
        public InfluxConf influx { get; set; }
        public IList<OpcItem> items { get; set; }
    }

    public class InfluxConf
    {
        public string url { get; set; }
        public string token { get; set; }
        public string org { get; set; }
        public string bucket { get; set; }
    }
}
