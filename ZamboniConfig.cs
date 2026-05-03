namespace Zamboni14Legacy;

public class ZamboniConfig
{
    public string GameServerIp { get; set; } = "192.168.1.79";
    public ushort GameServerPort { get; set; } = 16767;
    public string LogLevel { get; set; } = "Debug";
    public string DatabaseConnectionString { get; set; } = "Host=localhost;Port=5432;Username=postgres;Password=password;Database=zamboni";
    public bool HostRedirectorInstance { get; set; } = true;
    public string ApiServerIdentifier { get; set; } = "nhl14";
    public string ApiServerPort { get; set; } = "8082";
    public string TargetProtocol { get; set; } = "NHL14_1.00";
    public bool UseRelayServerImplementation { get; set; } = false;
    public Dictionary<string, RelayInstanceConfig> Relays { get; set; } = new();
    public SortedDictionary<string, string> Config { get; set; } = new();
    public int TopologyOverride { get; set; } = 0;
}