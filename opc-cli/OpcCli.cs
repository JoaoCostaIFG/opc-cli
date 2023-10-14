using InfluxDB.Client.Writes;
using OPC.Common;
using OPC.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace opc_cli
{
    internal class OpcCli
    {
        static private InfluxDBService client;
        static private Config conf;

        static private void onDataChange(OpcServerConn server, object sender, DataChangeEventArgs e)
        {
            List<PointData> points = new List<PointData>(e.sts.Length);

            foreach (OPCItemState s in e.sts)
            {
                if (HRESULTS.Failed(s.Error))
                {
                    Console.WriteLine("\tFailed read");
                    continue;
                }

                var itemName = server.items[s.HandleClient];
                var value = s.DataValue;
                var time = DateTime.FromFileTime(s.TimeStamp);

                if (conf.debug)
                {
                    Console.WriteLine("Item read: " + itemName);
                    Console.WriteLine("\tVal: " + value.ToString());
                    Console.WriteLine("\tQual: " + OpcGroup.QualityToString(s.Quality));
                    Console.WriteLine("\tTime: " + time.ToString());
                }

                var point = PointData.Measurement(itemName)
                    .Field("value", value)
                    .Timestamp(time, InfluxDB.Client.Api.Domain.WritePrecision.Ns);
                points.Add(point);
            }

            ThreadPool.QueueUserWorkItem(writePoints, points);
        }

        private static void writePoints(object points)
        {
            client.Write(write => write.WritePoints((List<PointData>)points, conf.influx.bucket, conf.influx.org));
        }

        private static int treeCmd(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Missing config file path for connect command");
                return 1;
            }
            string confPath = args[1];
            string confString = File.ReadAllText(confPath);
            conf = JsonSerializer.Deserialize<Config>(confString);
            if (conf == null)
            {
                Console.WriteLine("Config parse error");
                return 1;
            }

            // just print the tree
            new OpcServerConn(conf.progId, onDataChange);
            return 0;
        }

        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                return OpcServerConn.listServers();
            }

            string command = args[0];
            switch (command)
            {
                case "list":
                    return OpcServerConn.listServers();
                case "tree":
                    return treeCmd(args);
                case "connect":
                    if (args.Length != 2)
                    {
                        Console.WriteLine("Missing config file path for connect command");
                        return 1;
                    }
                    string confPath = args[1];
                    string confString = File.ReadAllText(confPath);
                    conf = JsonSerializer.Deserialize<Config>(confString);
                    if (conf == null)
                    {
                        Console.WriteLine("Config parse error");
                        return 1;
                    }

                    client = new InfluxDBService(conf.influx.url, conf.influx.token);

                    OpcServerConn server = new OpcServerConn(conf.progId, onDataChange);
                    foreach (OpcItem oi in conf.items)
                    {
                        server.addItem(oi);
                    }

                    // spin forever
                    while (true)
                    {
                        Thread.Sleep(int.MaxValue);
                    }
                    break;
                default:
                    Console.WriteLine("Unknown command: " + command);
                    return 1;
            }
            return 0;
        }
    }
}
