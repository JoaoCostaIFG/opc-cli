# opc-cli

This is a simple command line tool to fetch streamed data from an OPC UA server.
It uses the [OPCDotNetLib](https://github.com/chmstimoteo/OPCDotNetLib) library to connect to the server and fetch the data.

The included application watches a series of devices and stores the information on a [InfluxDB](https://www.influxdata.com/) database. It comes with 3 commands:

- `list` - lists available OPC servers;
- `tree <./config.json>` - draws a tree for the nodes in the given OPC server;
- `connect <./config.json>` - connects to the server to track the data and save it to the database.

## Config

The application reads a configuration from a provided config file (see [example](./conf.json)).

## Usage

```bash
opc-cli <command> <config-file>
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details
