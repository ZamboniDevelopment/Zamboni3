namespace Zamboni14Legacy;

public class GameServerProviderConfig
{
    public string Ip { get; set; } = "";
    public ushort PingSitePort { get; set; } = 17502;
    public ushort ZProtocolPort { get; set; } = 3737;
    
    public string ResolveIp()
    {
        return Ip.ToLower().Equals("auto") ? Program.PublicIp : Ip;
    }
}