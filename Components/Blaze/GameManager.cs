using System.Collections.Concurrent;
using System.Timers;
using Blaze3SDK;
using Blaze3SDK.Blaze;
using Blaze3SDK.Blaze.GameManager;
using Blaze3SDK.Components;
using BlazeCommon;
using NLog;
using Timer = System.Timers.Timer;

namespace Zamboni14Legacy.Components.Blaze;

internal class GameManager : GameManagerBase.Server
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private static readonly Timer Timer;
    public static ConcurrentQueue<ulong> StaleGames = new();

    static GameManager()
    {
        Timer = new Timer(5000);
        Timer.Elapsed += OnTimedEvent;
        Timer.AutoReset = true;
        Timer.Enabled = true;
    }

    private static void OnTimedEvent(object sender, ElapsedEventArgs e)
    {
        if (ServerManager.GetQueuedPlayers().Count <= 1) return;

        var grouped = ServerManager.GetQueuedPlayers().Values.GroupBy(u => u.StartMatchmakingRequest.mCriteriaData.mGenericRulePrefsList.Find(prefs => prefs.mRuleName.Equals("OSDK_gameMode")).mDesiredValues[0]);

        foreach (var group in grouped)
        {
            var users = group.ToList();

            while (users.Count >= 2)
            {
                var queuedPlayerA = users[0];
                var queuedPlayerB = users[1];

                users.RemoveRange(0, 2);
                ServerManager.RemoveQueuedPlayerByUserId(queuedPlayerA.ServerPlayer.UserIdentification.mAccountId);
                ServerManager.RemoveQueuedPlayerByUserId(queuedPlayerB.ServerPlayer.UserIdentification.mAccountId);

                _ = SendToMatchMakingGame(queuedPlayerA, queuedPlayerB, queuedPlayerA.StartMatchmakingRequest);
            }
        }
    }

    public override async Task<JoinGameResponse> ResetDedicatedServerAsync(CreateGameRequest request, BlazeRpcContext context)
    {
        var host = ServerManager.GetServerPlayerByConnectionId(context.Connection.ID)!;

        var serverGame = await ServerGame.CreateAsync(host, request);

        await serverGame.AddGameParticipant(host);
        UpdateGameLobbies();

        return new JoinGameResponse
        {
            mGameId = serverGame.ReplicatedGameData.mGameId
        };
    }

    private static async Task SendToMatchMakingGame(QueuedPlayer host, QueuedPlayer notHost, StartMatchmakingRequest startMatchmakingRequest)
    {
        var serverGame = await ServerGame.CreateAsync(host.ServerPlayer, startMatchmakingRequest);

        await serverGame.AddGameParticipant(host.ServerPlayer, host.MatchmakingSessionId);
        await Task.Delay(2000);
        await serverGame.AddGameParticipant(notHost.ServerPlayer, notHost.MatchmakingSessionId);
    }

    public override Task<StartMatchmakingResponse> StartMatchmakingAsync(StartMatchmakingRequest request, BlazeRpcContext context)
    {
        GenericRulePrefs genericRulePrefs = request.mCriteriaData.mGenericRulePrefsList.Find(prefs => prefs.mRuleName.Equals("OSDK_gameMode"));
        string targetGameMode = genericRulePrefs.mDesiredValues[0];
        if (!targetGameMode.Equals("6"))
        {
            throw new BlazeRpcException(Blaze3RpcError.ERR_COMMAND_NOT_FOUND);
        }

        var serverPlayer = ServerManager.GetServerPlayerByConnectionId(context.Connection.ID);

        var queuedPlayer = new QueuedPlayer(serverPlayer, request);

        return Task.FromResult(new StartMatchmakingResponse
        {
            mSessionId = queuedPlayer.MatchmakingSessionId
        });
    }

    public override Task<NullStruct> CancelMatchmakingAsync(CancelMatchmakingRequest request, BlazeRpcContext context)
    {
        var serverPlayer = ServerManager.GetServerPlayerByConnectionId(context.Connection.ID);
        var queuedPlayer = ServerManager.GetQueuedPlayer(serverPlayer);
        if (queuedPlayer != null) ServerManager.RemoveQueuedPlayerByUserId(queuedPlayer.ServerPlayer.UserIdentification.mAccountId);
        NotifyMatchmakingFailedAsync(context.BlazeConnection, new NotifyMatchmakingFailed
            {
                mMatchmakingResult = MatchmakingResult.SESSION_TERMINATED,
                mMaxPossibleFitScore = 0,
                mSessionId = queuedPlayer.MatchmakingSessionId,
                // mUserSessionId = (uint)serverPlayer.SessionInfo.mBlazeUserId
            }, true
        );
        return Task.FromResult(new NullStruct());
    }

    public override async Task<JoinGameResponse> JoinGameAsync(JoinGameRequest request, BlazeRpcContext context)
    {
        var serverGame = ServerManager.GetServerGame(request.mGameId);
        if (serverGame == null) throw new BlazeRpcException(Blaze3RpcError.GAMEMANAGER_ERR_INVALID_GAME_ID);
        if (!request.mGameProtocolVersionString.Equals(serverGame.ReplicatedGameData.mGameProtocolVersionString)) throw new BlazeRpcException(Blaze3RpcError.GAMEMANAGER_ERR_GAME_PROTOCOL_VERSION_MISMATCH);

        var serverPlayer = ServerManager.GetServerPlayerByConnectionId(context.Connection.ID);

        if (!serverGame.HasSpaceForPlayer()) throw new BlazeRpcException(Blaze3RpcError.GAMEMANAGER_ERR_GAME_FULL);

        await serverGame.AddGameParticipant(serverPlayer);

        return new JoinGameResponse
        {
            mGameId = request.mGameId,
            mJoinState = JoinState.JOINED_GAME
        };
    }

    public override Task<GetGameListResponse> GetGameListSubscriptionAsync(GetGameListRequest request, BlazeRpcContext context)
    {
        UpdateGameLobbies();

        return Task.FromResult(new GetGameListResponse
        {
            mListId = 1,
            mMaxPossibleFitScore = 10
        });
    }

    public override Task<NullStruct> DestroyGameListAsync(DestroyGameListRequest request, BlazeRpcContext context)
    {
        return Task.FromResult(new NullStruct());
    }

    public override Task<NullStruct> FinalizeGameCreationAsync(UpdateGameSessionRequest request, BlazeRpcContext context)
    {
        var serverGame = ServerManager.GetServerGame(request.mGameId);
        if (serverGame == null) return Task.FromResult(new NullStruct());

        var replicatedGameData = serverGame.ReplicatedGameData;
        replicatedGameData.mXnetNonce = request.mXnetNonce;
        replicatedGameData.mXnetSession = request.mXnetSession;

        serverGame.ReplicatedGameData = replicatedGameData;

        foreach (var serverPlayer in serverGame.ServerPlayers.Values)
            NotifyGameSessionUpdatedAsync(serverPlayer.BlazeServerConnection, new GameSessionUpdatedNotification
            {
                mGameId = request.mGameId,
                mXnetNonce = request.mXnetNonce,
                mXnetSession = request.mXnetSession
            });
        return Task.FromResult(new NullStruct());
    }

    public override Task<NullStruct> AdvanceGameStateAsync(AdvanceGameStateRequest request, BlazeRpcContext context)
    {
        var serverGame = ServerManager.GetServerGame(request.mGameId);
        if (serverGame == null) return Task.FromResult(new NullStruct());

        var replicatedGameData = serverGame.ReplicatedGameData;
        replicatedGameData.mGameState = request.mNewGameState;

        serverGame.ReplicatedGameData = replicatedGameData;

        foreach (var serverPlayer in serverGame.ServerPlayers.Values)
        {
            NotifyGameStateChangeAsync(serverPlayer.BlazeServerConnection, new NotifyGameStateChange
            {
                mGameId = request.mGameId,
                mNewGameState = request.mNewGameState
            }, true);
        }

        return Task.FromResult(new NullStruct());
    }


    public override Task<NullStruct> SetPlayerAttributesAsync(SetPlayerAttributesRequest request, BlazeRpcContext context)
    {
        var serverGame = ServerManager.GetServerGame(request.mGameId);
        if (serverGame == null) throw new BlazeRpcException(Blaze3RpcError.GAMEMANAGER_ERR_INVALID_GAME_ID);
        var serverGameReplicatedGamePlayer = serverGame.ReplicatedGamePlayers[request.mPlayerId];
        serverGameReplicatedGamePlayer.mPlayerAttribs = request.mPlayerAttributes;
        serverGame.ReplicatedGamePlayers[request.mPlayerId] = serverGameReplicatedGamePlayer;

        foreach (var participant in serverGame.ServerPlayers.Values)
            NotifyPlayerAttribChangeAsync(participant.BlazeServerConnection, new NotifyPlayerAttribChange
            {
                mGameId = serverGame.ReplicatedGameData.mGameId,
                mPlayerAttribs = request.mPlayerAttributes,
                mPlayerId = request.mPlayerId
            });

        return Task.FromResult(new NullStruct());
    }

    public override async Task<NullStruct> UpdateMeshConnectionAsync(UpdateMeshConnectionRequest request, BlazeRpcContext context)
    {
        var serverGame = ServerManager.GetServerGame(request.mGameId);
        if (serverGame == null) throw new BlazeRpcException(Blaze3RpcError.GAMEMANAGER_ERR_INVALID_GAME_ID);

        foreach (var playerConnectionStatus in request.mMeshConnectionStatusList)
            switch (playerConnectionStatus.mPlayerNetConnectionStatus)
            {
                case PlayerNetConnectionStatus.CONNECTED:
                {
                    var statePacket = new NotifyGamePlayerStateChange
                    {
                        mGameId = request.mGameId,
                        mPlayerId = playerConnectionStatus.mTargetPlayer,
                        mPlayerState = PlayerState.ACTIVE_CONNECTED
                    };
                    serverGame.ServerPlayers.Values.ToList().ForEach(participant => NotifyGamePlayerStateChangeAsync(participant.BlazeServerConnection, statePacket));

                    var joinCompletedPacket = new NotifyPlayerJoinCompleted
                    {
                        mGameId = request.mGameId,
                        mPlayerId = playerConnectionStatus.mTargetPlayer
                    };
                    serverGame.ServerPlayers.Values.ToList().ForEach(participant => NotifyPlayerJoinCompletedAsync(participant.BlazeServerConnection, joinCompletedPacket));
                    break;
                }
                case PlayerNetConnectionStatus.ESTABLISHING_CONNECTION:
                {
                    var statePacket = new NotifyGamePlayerStateChange
                    {
                        mGameId = request.mGameId,
                        mPlayerId = playerConnectionStatus.mTargetPlayer,
                        mPlayerState = PlayerState.ACTIVE_CONNECTING
                    };
                    serverGame.ServerPlayers.Values.ToList().ForEach(participant => NotifyGamePlayerStateChangeAsync(participant.BlazeServerConnection, statePacket));
                    break;
                }
                case PlayerNetConnectionStatus.DISCONNECTED:
                {
                    //TODO
                    await serverGame.RemoveGame();
                    break;
                }
                default:
                    Logger.Debug("Unknown player connection status: " + playerConnectionStatus.mPlayerNetConnectionStatus);
                    break;
            }

        return new NullStruct();
    }

    public override async Task<NullStruct> RemovePlayerAsync(RemovePlayerRequest request, BlazeRpcContext context)
    {
        var serverGame = ServerManager.GetServerGame(request.mGameId);

        if (serverGame == null) throw new BlazeRpcException(Blaze3RpcError.GAMEMANAGER_ERR_INVALID_GAME_ID);

        await serverGame.RemoveGame();

        UpdateGameLobbies();
        return new NullStruct();
    }

    public override Task<NullStruct> UpdateGameSessionAsync(UpdateGameSessionRequest request, BlazeRpcContext context)
    {
        var serverGame = ServerManager.GetServerGame(request.mGameId);
        if (serverGame == null) return Task.FromResult(new NullStruct());

        var replicatedGameData = serverGame.ReplicatedGameData;
        replicatedGameData.mXnetNonce = request.mXnetNonce;
        replicatedGameData.mXnetSession = request.mXnetSession;

        serverGame.ReplicatedGameData = replicatedGameData;

        foreach (var serverPlayer in serverGame.ServerPlayers.Values)
            NotifyGameSessionUpdatedAsync(serverPlayer.BlazeServerConnection, new GameSessionUpdatedNotification
            {
                mGameId = request.mGameId,
                mXnetNonce = request.mXnetNonce,
                mXnetSession = request.mXnetSession
            });
        return Task.FromResult(new NullStruct());
    }


    public override Task<NullStruct> SetGameSettingsAsync(SetGameSettingsRequest request, BlazeRpcContext context)
    {
        var serverGame = ServerManager.GetServerGame(request.mGameId);
        if (serverGame == null) return Task.FromResult(new NullStruct());

        var replicatedGameData = serverGame.ReplicatedGameData;
        replicatedGameData.mGameSettings = request.mGameSettings;

        serverGame.ReplicatedGameData = replicatedGameData;

        foreach (var serverPlayer in serverGame.ServerPlayers.Values)
            NotifyGameSettingsChangeAsync(serverPlayer.BlazeServerConnection, new NotifyGameSettingsChange
            {
                mGameSettings = request.mGameSettings,
                mGameId = request.mGameId
            });
        return Task.FromResult(new NullStruct());
    }

    public override async Task<CreateGameResponse> CreateGameAsync(CreateGameRequest request, BlazeRpcContext context)
    {
        var host = ServerManager.GetServerPlayerByConnectionId(context.Connection.ID);
        var serverGame = await ServerGame.CreateAsync(host, request);

        await serverGame.AddGameParticipant(host);

        UpdateGameLobbies();
        return new CreateGameResponse
        {
            mGameId = serverGame.ReplicatedGameData.mGameId
        };
    }
    // mFitScore = (uint)(serverGame.ReplicatedGameData.mPingSiteAlias == searcher.ExtendedData.mBestPingSiteAlias ? 10 : 1),

    private static void UpdateGameLobbies()
    {
        ServerManager.GetServerPlayers().Values.ToList().ForEach(sp => NotifyGameListUpdateAsync(sp.BlazeServerConnection, new NotifyGameListUpdate
        {
            mIsFinalUpdate = 1,
            mListId = 1,
            mRemovedGameList = StaleGames.ToList(),
            mUpdatedGames = GetLobbies(sp)
        }, true));
    }
    
    private static List<GameBrowserMatchData> GetLobbies(ServerPlayer searcher)
    {
        var lobbies = new List<GameBrowserMatchData>();
        foreach (var serverGame in ServerManager.GetServerGames().Values)
        {
            if (serverGame.ReplicatedGameData.mGameState != GameState.PRE_GAME &&
                serverGame.ReplicatedGameData.mGameState != GameState.INITIALIZING) continue;

            if (!serverGame.HasSpaceForPlayer()) continue;

            var participants = new List<GameBrowserPlayerData>();
            foreach (var gamePlayer in serverGame.ReplicatedGamePlayers.Values)
                participants.Add(new GameBrowserPlayerData
                {
                    mAccountLocale = gamePlayer.mAccountLocale,
                    mExternalId = gamePlayer.mExternalId,
                    mPlayerAttribs = gamePlayer.mPlayerAttribs,
                    mPlayerId = gamePlayer.mPlayerId,
                    mPlayerName = gamePlayer.mPlayerName,
                    mPlayerState = gamePlayer.mPlayerState,
                    mTeamIndex = gamePlayer.mTeamIndex
                });

            var teamInfo = new List<GameBrowserTeamInfo>();

            // foreach (var teamCapacity in serverGame.ReplicatedGameData.mTeamCapacity)
            // {
            //     teamInfo.Add(new GameBrowserTeamInfo
            //     {
            //         mTeamId = teamCapacity.mTeamId,
            //         mTeamSize = teamCapacity.mTeamCapacity
            //     });
            // }
            lobbies.Add(new GameBrowserMatchData
            {
                mFitScore = (uint)(serverGame.ReplicatedGameData.mPingSiteAlias == searcher.ExtendedData.mBestPingSiteAlias ? 10 : 1),
                mGameData = new GameBrowserGameData
                {
                    mAdminPlayerList = serverGame.ReplicatedGameData.mAdminPlayerList,
                    mEntryCriteriaMap = serverGame.ReplicatedGameData.mEntryCriteriaMap,
                    mExternalSessionId = 1,
                    mGameAttribs = serverGame.ReplicatedGameData.mGameAttribs,
                    mGameBrowserTeamInfoVector = teamInfo,
                    mGameId = serverGame.ReplicatedGameData.mGameId,
                    mGameName = serverGame.ReplicatedGameData.mGameName,
                    mGameProtocolVersionString = serverGame.ReplicatedGameData.mGameProtocolVersionString,
                    mGameRoster = participants,
                    mGameSettings = serverGame.ReplicatedGameData.mGameSettings,
                    mGameState = serverGame.ReplicatedGameData.mGameState,
                    mHostId = serverGame.ReplicatedGameData.mTopologyHostInfo.mPlayerId,
                    mHostNetworkAddressList = serverGame.ReplicatedGameData.mHostNetworkAddressList,
                    mNetworkTopology = serverGame.ReplicatedGameData.mNetworkTopology,
                    mPersistedGameId = serverGame.ReplicatedGameData.mPersistedGameId,
                    mPingSiteAlias = serverGame.ReplicatedGameData.mPingSiteAlias,
                    mPlayerCounts = new List<ushort>
                    {
                        0,0
                    },
                    mPresenceMode = serverGame.ReplicatedGameData.mPresenceMode,
                    mQueueCapacity = serverGame.ReplicatedGameData.mQueueCapacity,
                    mQueueCount = serverGame.ReplicatedGameData.mQueueCapacity,
                    mSlotCapacities = new List<ushort>
                    {
                        1,1
                    },
                    mTeamCapacity = 5,
                    mVoipTopology = VoipTopology.VOIP_DISABLED
                }
            });
        }

        return lobbies;
    }
}