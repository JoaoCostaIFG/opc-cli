using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using InfluxDB.Client.Writes;
using OPC;
using OPC.Common;
using OPC.Data;
using OPC.Data.Interface;

namespace opc_cli
{
    internal class OpcServerConn
    {
        private OpcServer server;
        private OpcGroup grp;
        public List<string> items { get; }

        public Action<OpcServerConn, object, DataChangeEventArgs> onDataChange; 

        public OpcServerConn(string progId, Action<OpcServerConn, object, DataChangeEventArgs> onDataChange)
        {
            this.onDataChange = onDataChange;
            this.items = new List<string>();

            this.server = new OpcServer();
            try
            {
                server.Connect(progId);
                Thread.Sleep(1000);
                server.SetClientName("OPC-cli " + Process.GetCurrentProcess().Id);

                Console.WriteLine(this.serverStatusStr());
            }
            catch (COMException e)
            {
                Console.WriteLine("Connection error! Exception: " + e.Message);
                throw e;
            }

            try
            {
                this.grp = server.AddGroup("OPC-cli group", true, 500);
                grp.DataChanged += new DataChangeEventHandler(this.onDataChangeWrapper);
            }
            catch (COMException e)
            {
                Console.WriteLine("Failed to create group! Exception: " + e.Message);
                throw e;
            }

            int maxWorkerThreads, maxCompletionPortThreads;
            ThreadPool.GetMaxThreads(out maxWorkerThreads, out maxCompletionPortThreads);
            Console.WriteLine("\nMaximum worker threads: \t{0}" +
                "\nMaximum completion port threads: {1}",
                maxWorkerThreads, maxCompletionPortThreads);

            this.treePrint();
        }

        private void onDataChangeWrapper(object sender, DataChangeEventArgs e)
        {
            this.onDataChange(this, sender, e);
        }

        public int addItem(OpcItem opcItem)
        {
            OPCItemDef[] aD = new OPCItemDef[1];
            try
            {
                aD[0] = new OPCItemDef(opcItem.node, true, this.items.Count, VarEnum.VT_EMPTY);

                OPCItemResult[] arrRes;
                grp.AddItems(aD, out arrRes);
                if (arrRes == null || arrRes[0].Error != HRESULTS.S_OK)
                {
                    Console.WriteLine("Failed to add item to group");
                    return 1;
                }

                OPCACCESSRIGHTS itmAccessRights = arrRes[0].AccessRights;
                bool isReadble = (itmAccessRights & OPCACCESSRIGHTS.OPC_READABLE) != 0;
                bool isWritable = (itmAccessRights & OPCACCESSRIGHTS.OPC_WRITEABLE) != 0;

                Console.WriteLine("Added: " + opcItem.name);
                Console.WriteLine("\tHandleServer: " + arrRes[0].HandleServer);
                Console.WriteLine("\tCanonicalDataType: " + arrRes[0].CanonicalDataType);
                Console.WriteLine("\tReadable: " + isReadble);
                Console.WriteLine("\tWritable: " + isWritable);

                if (isReadble)
                {
                    int cancelID;
                    grp.Refresh2(OPCDATASOURCE.OPC_DS_DEVICE, 7788, out cancelID);
                }
            }
            catch (COMException e)
            {
                int[] arrErr;
                grp.RemoveItems(new int[] {this.items.Count}, out arrErr);
                Console.WriteLine("Failed to add item! Exception: " + e.Message);
                return 1;
            }

            this.items.Add(opcItem.name);
            return 0;
        }

        public int addItem(string opcId, string itemName)
        {
            return this.addItem(new OpcItem(opcId, itemName));
        }

        public int addItem(string opcId)
        {
            return this.addItem(opcId, opcId);
        }

        private void treePrint(int depth)
        {
            if (depth == 0)
            {
                Console.WriteLine(".");
            }

            ArrayList lst = new ArrayList();
            server.Browse(OPCBROWSETYPE.OPC_BRANCH, out lst);

            int i = 0;
            foreach (string s in lst)
            {
                StringBuilder sb = new StringBuilder();
                for (int j = 0; j < depth; ++j)
                {
                    sb.Append("│   ");
                }
                sb.Append((i++ == lst.Count - 1) ? "└" : "├");
                sb.Append("── ");
                sb.Append(s);
                Console.WriteLine(sb.ToString());

                server.ChangeBrowsePosition(OPCBROWSEDIRECTION.OPC_BROWSE_DOWN, s);
                this.treePrint(depth + 1);
                server.ChangeBrowsePosition(OPCBROWSEDIRECTION.OPC_BROWSE_UP, "");
            }

            lst = new ArrayList();
            server.Browse(OPCBROWSETYPE.OPC_LEAF, out lst);

            i = 0;
            foreach (string s in lst)
            {
                StringBuilder sb = new StringBuilder();
                for (int j = 0; j < depth; ++j)
                {
                    sb.Append("│   ");
                }
                sb.Append((i++ == lst.Count - 1) ? "└" : "├");
                sb.Append("── ");
                sb.Append(s);
                Console.WriteLine(sb.ToString());
            }
        }

        public void treePrint()
        {
            treePrint(0);
        }

        public string serverStatusStr()
        {
            SERVERSTATUS status = null;
            server.GetStatus(out status);

            StringBuilder sb = new StringBuilder(status.szVendorInfo, 256);
            sb.AppendFormat("status: {0}\nstart time: {1}\nver:{2}.{3}.{4}",
                status.eServerState.ToString(),
                DateTime.FromFileTime(status.ftStartTime).ToString(),
                status.wMajorVersion, status.wMinorVersion, status.wBuildNumber);

            return sb.ToString();
        }

        public static int listServers()
        {
            OpcServers[] serverList = null;
            try
            {
                 new OpcServerList().ListAllData20(out serverList);
            }
            catch (COMException)
            {
                Console.WriteLine("Enum OPC servers failed! Exitting...");
                return 1;
            }

            if (serverList == null)
            {
                Console.WriteLine("Enum OPC servers failed! Exitting...");
                return 1;
            }

            foreach (OpcServers server in serverList)
            {
                Console.WriteLine(server.ServerName + " - " + server.ProgID + " - " + server.ClsID.ToString());
            }

            return 0;
        }
    }
}
