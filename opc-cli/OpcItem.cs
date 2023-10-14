using System;

namespace opc_cli
{
    public class OpcItem
    {
        public String node { get; set; }
        public String name { get; set; }

        public OpcItem(string node, string name)
        {
            this.node = node;
            this.name = name;
        }
    }
}
