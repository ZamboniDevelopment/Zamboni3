using System.Collections.Concurrent;
using Blaze3SDK.Blaze;
using Blaze3SDK.Components;

namespace Zamboni14Legacy;

public static class ServerManager
{
    private static readonly ConcurrentDictionary<long, ServerPlayer> ServerPlayers = new();
    private static readonly ConcurrentDictionary<long, QueuedPlayer> QueuedPlayers = new();
    private static readonly ConcurrentDictionary<ulong, ServerGame> ServerGames = new();

    public static void AddServerPlayer(long userId, ServerPlayer serverPlayer)
    {
        var existing = GetServerPlayerByName(serverPlayer.UserIdentification.mName);
        if (existing != null) RemoveServerPlayerByUserId(existing.UserIdentification.mAccountId);
        ServerPlayers.TryAdd(userId, serverPlayer);

        UserSessionsBase.Server.NotifyUserAuthenticated(serverPlayer.BlazeServerConnection, new NotifyUserAuthenticated
        {
            mSUBS = true,
            mBlazeUserId = serverPlayer.UserIdentification.mAccountId
        },true);

        UserSessionsBase.Server.NotifyUserAddedAsync(serverPlayer.BlazeServerConnection, new NotifyUserAdded
        {
            mUserInfo = serverPlayer.UserIdentification
        },true);
        
        UserSessionsBase.Server.NotifyUserSessionExtendedDataUpdateAsync(serverPlayer.BlazeServerConnection, new UserSessionExtendedDataUpdate()
        {
            mExtendedData = new UserSessionExtendedData(),
            mUserId =  serverPlayer.UserIdentification.mAccountId
        },true);
    }

    public static void AddQueuedPlayer(long userId, QueuedPlayer queuedPlayer)
    {
        QueuedPlayers.TryAdd(userId, queuedPlayer);
    }

    public static void AddServerGame(ulong gameId, ServerGame serverGame)
    {
        ServerGames.TryAdd(gameId, serverGame);
    }

    public static bool RemoveServerPlayerByUserId(long userId)
    {
        return ServerPlayers.TryRemove(userId, out _);
    }

    public static bool RemoveQueuedPlayerByUserId(long userId)
    {
        return QueuedPlayers.TryRemove(userId, out _);
    }

    public static bool RemoveServerGame(ulong gameId)
    {
        return ServerGames.TryRemove(gameId, out _);
    }

    public static ConcurrentDictionary<long, ServerPlayer> GetServerPlayers()
    {
        return ServerPlayers;
    }

    public static ConcurrentDictionary<long, QueuedPlayer> GetQueuedPlayers()
    {
        return QueuedPlayers;
    }

    public static ConcurrentDictionary<ulong, ServerGame> GetServerGames()
    {
        return ServerGames;
    }

    public static ServerPlayer? GetServerPlayerByConnectionId(long connectionId)
    {
        return ServerPlayers.Values.FirstOrDefault(serverPlayer => serverPlayer.BlazeServerConnection.ProtoFireConnection.ID == connectionId);
    }
    
    public static ServerPlayer? GetServerPlayerByUserId(long userId)
    {
        return ServerPlayers[userId];
    }

    public static ServerPlayer? GetServerPlayerByName(string name)
    {
        return ServerPlayers.Values.FirstOrDefault(p => p.UserIdentification.mName.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public static ServerGame? GetServerGame(ulong gameId)
    {
        return ServerGames.GetValueOrDefault(gameId);
    }

    public static ServerGame? GetServerGame(ServerPlayer serverPlayer)
    {
        return ServerGames.Values.FirstOrDefault(serverGame => serverGame.ServerPlayers.Values.Contains(serverPlayer));
    }

    public static QueuedPlayer? GetQueuedPlayer(ServerPlayer serverPlayer)
    {
        return QueuedPlayers.Values.FirstOrDefault(queuedPlayer => queuedPlayer.ServerPlayer.Equals(serverPlayer));
    }
}