namespace Zamboni3;

public class ZamboniConfig
{
    public string CoreServerIp { get; set; } = "auto";
    public ushort CoreServerPort { get; set; } = 16767;
    public string LogLevel { get; set; } = "Debug";
    public string DatabaseConnectionString { get; set; } = "Host=localhost;Port=5432;Username=postgres;Password=password;Database=zamboni";
    public bool HostRedirectorInstance { get; set; } = true;
    public string ApiServerIdentifier { get; set; } = "nhl14";
    public string ApiServerPort { get; set; } = "8082";
    public string CertPath { get; set; } = "gosredirector_mod.pfx";
    public string CertPassword { get; set; } = "123456";
    public string TargetProtocol { get; set; } = "NHL14_1.00";
    public bool StartLocalGameServerProvider { get; set; } = true;
    public Dictionary<string, GameServerProviderConfig> GameServerProviders { get; set; } = new()
    {
        { "this", new GameServerProviderConfig
            {
                Ip = "auto",
                PingSitePort = 17502,
                ZProtocolPort = 3737
            }
        },
    };

    public SortedDictionary<string, string> Config { get; set; } = new()
    {
        {
            "OSDK_MAX_PER_OTP_SIDE","6"
        }
    };
    public string ZamboniTopology { get; set; } = "Relayed";
}