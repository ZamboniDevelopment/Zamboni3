using System.Net.Sockets;
using NLog;
using RelayProtocol;
using Protocol = RelayProtocol.RelayProtocol;

namespace Zamboni14Legacy;

public static class RelayCommunicator
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private static async Task<ResponsePacket?> SendAsync(string ip, CommandPacket command)
    {
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(ip, 4000);
            await using var stream = client.GetStream();
            await Protocol.SendCommandAsync(stream, command);
            return await Protocol.ReadResponseAsync(stream);
        }
        catch (Exception e)
        {
            Logger.Warn("Failed to contact relay " + ip);
        }

        return null;
    }

    public static async Task<(uint Ip, ushort Port)> ReserveRelayInstance(ServerPlayer creator, string gameVersionProtocol)
    {
        var ip = Program.ZamboniConfig.Relays[creator.ExtendedData.mBestPingSiteAlias].Ip;
        var response = await SendAsync(ip, new ReserveInstanceCommand(gameVersionProtocol));

        if (response is ReserveInstanceResponse r)
        {
            if (response.Status == Protocol.RelayStatus.Ok)
            {
                return (Util.GetIPAddressAsUInt(ip), r.Port);
            }
        }

        Logger.Debug("Relay might have blocked request");
        return (0, 0);
    }

    public static async Task DestroyRelayInstance(ServerGame serverGame)
    {
        var ipAddress = serverGame.ReplicatedGameData.mHostNetworkAddressList[0].IpAddress;
        if (ipAddress == null) throw new Exception("ServerGame is not even relayed");

        var ip = Util.GetUIntAsIPAddress(ipAddress.Value.mIp);
        await SendAsync(ip, new DestroyInstanceCommand(ipAddress.Value.mPort));
    }

    public static async Task AllowFrom(string relayIp, ushort relayPort, string allowedIp)
    {
        await SendAsync(relayIp, new AllowFromCommand(relayPort, allowedIp));
    }

    public static async Task ResetAllInstances(string[] gameVersionProtocols)
    {
        foreach (var relayConfig in Program.ZamboniConfig.Relays.Values)
        {
            await SendAsync(relayConfig.Ip, new ResetAllInstancesCommand(gameVersionProtocols));
        }
    }
}