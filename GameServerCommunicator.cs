using System.Net.Sockets;
using NLog;
using ZProtocol;
using Protocol = ZProtocol.ZProtocol;

namespace Zamboni3;

public static class GameServerCommunicator
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private static async Task<ResponsePacket?> SendAsync(string ip, ushort port, CommandPacket command)
    {
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(ip, port);
            await using var stream = client.GetStream();
            await Protocol.SendCommandAsync(stream, command);
            return await Protocol.ReadResponseAsync(stream);
        }
        catch (Exception e)
        {
            Logger.Warn(e);
            Logger.Warn("Failed to contact GameServerProvider " + ip);
        }

        return null;
    }

    public static async Task<ReserveInstanceResponse> ReserveInstance(ServerPlayer creator, ReserveInstanceCommand command)
    {
        var target = Program.ZamboniConfig.GameServerProviders[creator.ExtendedData.mBestPingSiteAlias];
        var response = await SendAsync(target.ResolveIp(), target.ZProtocolPort, command);

        if (response is ReserveInstanceResponse r)
        {
            return r;
        }

        Logger.Debug("GameServerProvider might have blocked request");
        return null;
    }

    public static async Task DestroyInstance(ServerGame serverGame)
    {
        var ipAddress = serverGame.ReplicatedGameData.mHostNetworkAddressList[0].IpAddress;
        ushort zProtocolPort = 3737;
        var gameServerProvider = Program.ZamboniConfig.GameServerProviders[serverGame.ReplicatedGameData.mPingSiteAlias];
        if (gameServerProvider != null) zProtocolPort = gameServerProvider.ZProtocolPort;

        if (ipAddress == null) return;

        var ip = Util.GetUIntAsIPAddress(ipAddress.Value.mIp);
        await SendAsync(ip, zProtocolPort, new DestroyInstanceCommand(Guid.Parse(serverGame.ReplicatedGameData.mUUID)));
    }

    public static async Task ResetAllInstances(string[] gameVersionProtocols)
    {
        foreach (var gameServerProvider in Program.ZamboniConfig.GameServerProviders.Values)
        {
            await SendAsync(gameServerProvider.ResolveIp(), gameServerProvider.ZProtocolPort, new ResetAllInstancesCommand(gameVersionProtocols));
        }
    }
}