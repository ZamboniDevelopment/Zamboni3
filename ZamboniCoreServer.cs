using BlazeCommon;

namespace Zamboni14Legacy;

public class ZamboniCoreServer(BlazeServerConfiguration settings) : BlazeServer(settings)
{
    public override Task OnProtoFireDisconnectAsync(ProtoFireConnection connection)
    {
        var serverPlayer = ServerManager.GetServerPlayerByConnectionId(connection.ID);
        if (serverPlayer == null) return base.OnProtoFireDisconnectAsync(connection);
        ServerManager.RemoveServerPlayerByUserId(serverPlayer.UserIdentification.mAccountId);

        var queuedPlayer = ServerManager.GetQueuedPlayer(serverPlayer);
        if (queuedPlayer != null) ServerManager.RemoveQueuedPlayerByUserId(queuedPlayer.ServerPlayer.UserIdentification.mAccountId);

        var serverGame = ServerManager.GetServerGame(serverPlayer);
        if (serverGame != null) _ = serverGame.RemoveGame();

        return base.OnProtoFireDisconnectAsync(connection);
    }
}