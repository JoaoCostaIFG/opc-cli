using InfluxDB.Client;
using System;
using System.Threading.Tasks;

namespace opc_cli
{
    internal class InfluxDBService
    {
        private readonly string _url;
        private readonly string _token;

        public InfluxDBService(string url, string token)
        {
            _url = url;
            _token = token;
        }

        public void Write(Action<WriteApi> action)
        {
            var client = new InfluxDBClient(_url, _token);
            var write = client.GetWriteApi();
            action(write);
            client.Dispose();
        }
    }
}
