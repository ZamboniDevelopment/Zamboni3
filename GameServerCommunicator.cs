using System.Net;
using System.Net.Sockets;
using Blaze3SDK.Blaze.GameManager;
using NLog;
using ZamboniGameServerProvider;
using ZProtocol;
using Protocol = ZProtocol.ZProtocol;

namespace Zamboni3;

public class GameServerCommunicator
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static async Task<ReserveInstanceResponse> ReserveInstance(ServerPlayer creator, ReserveInstanceCommand command)
    {
        var target = Program.ZamboniConfig.GameServerProviders[creator.ExtendedData.mBestPingSiteAlias];
        var response = await GameServerProvider.SendAsync(target.ResolveIp(), target.ZProtocolPort, command);

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
        await GameServerProvider.SendAsync(ip, zProtocolPort, new DestroyInstanceCommand(Guid.Parse(serverGame.ReplicatedGameData.mUUID)));
    }

    public static async Task ResetAllInstances(string[] gameVersionProtocols)
    {
        foreach (var gameServerProvider in Program.ZamboniConfig.GameServerProviders.Values)
        {
            await GameServerProvider.SendAsync(gameServerProvider.ResolveIp(), gameServerProvider.ZProtocolPort, new ResetAllInstancesCommand(gameVersionProtocols));
        }
    }

    public static async Task<ResponsePacket> InformPlayerJoining(ServerGame serverGame, ServerPlayer serverPlayer)
    {
        var ipAddress = serverGame.ReplicatedGameData.mHostNetworkAddressList[0].IpAddress;
        ushort zProtocolPort = 3737;
        var gameServerProvider = Program.ZamboniConfig.GameServerProviders[serverGame.ReplicatedGameData.mPingSiteAlias];
        if (gameServerProvider != null) zProtocolPort = gameServerProvider.ZProtocolPort;

        if (ipAddress == null)
            return new GenericResponse
            {
                Status = Status.Error
            };

        var serverIp = Util.GetUIntAsIPAddress(ipAddress.Value.mIp);
        var playerIp = ((IPEndPoint)serverPlayer.BlazeServerConnection.ProtoFireConnection.Socket.RemoteEndPoint)!.Address.ToString();
        return await GameServerProvider.SendAsync(serverIp, zProtocolPort, new PlayerJoiningCommand(playerIp, serverGame.ReplicatedGamePlayers[serverPlayer.UserIdentification.mAccountId].mSlotId, Guid.Parse(serverGame.ReplicatedGameData.mUUID)));
    }

    public async Task Listen()
    {
        var listener = new TcpListener(IPAddress.Any, Program.ZamboniConfig.CoreServerZProtocolPort);
        listener.Start();

        while (true)
        {
            var client = await listener.AcceptTcpClientAsync();

            _ = Task.Run(async () =>
            {
                using (client)
                {
                    var remoteIp = ((IPEndPoint)client.Client.RemoteEndPoint!).Address;

                    if (Program.ZamboniConfig.GameServerProviders.Values.Any(config => config.ResolveIp() == remoteIp.ToString()))
                    {
                        try
                        {
                            await using var stream = client.GetStream();

                            var command = await Protocol.ReadCommandAsync(stream);

                            if (command == null)
                            {
                                Logger.Warn("Packet is null");
                                return;
                            }

                            if (command.Version != Protocol.ProtocolVersion)
                            {
                                Logger.Warn($"Version mismatch: {command.Version}");
                                return;
                            }

                            switch (command)
                            {
                                case PlayerLeavingCommand playerLeaving:

                                    var game = ServerManager.GetServerGames().Values.FirstOrDefault(game => Guid.Parse(game.ReplicatedGameData.mUUID).Equals(playerLeaving.Guid));
                                    if (game == null)
                                    {
                                        await Protocol.SendResponseAsync(stream, new GenericResponse
                                        {
                                            Status = Status.Error
                                        });
                                        break;
                                    }

                                    var playerId = game.ReplicatedGamePlayers.Values.ToList().FirstOrDefault(player => player.mSlotId == playerLeaving.Slot).mPlayerId;
                                    var serverPlayer = ServerManager.GetServerPlayerByUserId(playerId);
                                    if (serverPlayer == null)
                                    {
                                        await Protocol.SendResponseAsync(stream, new GenericResponse
                                        {
                                            Status = Status.Error
                                        });
                                        break;
                                    }

                                    game.RemoveGameParticipant(serverPlayer, PlayerRemovedReason.PLAYER_LEFT);
                                    await Protocol.SendResponseAsync(stream, new GenericResponse
                                    {
                                        Status = Status.Ok
                                    });
                                    break;

                                default:
                                    Logger.Warn($"Unknown command type: {command.GetType().Name}");
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex, "Error processing request");
                        }
                    }
                    else
                    {
                        Logger.Warn("Blocked packet from: " + remoteIp);
                    }
                }
            });
        }
    }
}