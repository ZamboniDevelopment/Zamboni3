using Blaze3SDK.Blaze.GameManager;
using BlazeCommon;

namespace Zamboni3;

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
        if (serverGame != null) serverGame.RemoveGameParticipant(serverPlayer, PlayerRemovedReason.BLAZESERVER_CONN_LOST);

        return base.OnProtoFireDisconnectAsync(connection);
    }
}