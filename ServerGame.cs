using System.Collections.Concurrent;
using System.Text;
using Blaze3SDK.Blaze;
using Blaze3SDK.Blaze.GameManager;
using Blaze3SDK.Components;
using Zamboni14Legacy.Components.Blaze;

namespace Zamboni14Legacy;

public class ServerGame
{
    public ConcurrentDictionary<long, ServerPlayer> ServerPlayers { get; } = new();
    public ReplicatedGameData ReplicatedGameData { get; set; }
    public ConcurrentDictionary<long, ReplicatedGamePlayer> ReplicatedGamePlayers { get; set; } = new();
    private bool relayed;

    public static async Task<ServerGame> CreateAsync(ServerPlayer creator, CreateGameRequest request)
    {
        var game = new ServerGame(creator, request);
        return await ContactRelay(game, creator);
    }

    public static async Task<ServerGame> CreateAsync(ServerPlayer creator, StartMatchmakingRequest request)
    {
        var game = new ServerGame(creator, request);
        return await ContactRelay(game, creator);
    }

    private static async Task<ServerGame> ContactRelay(ServerGame game, ServerPlayer creator)
    {
        if (!Program.ZamboniConfig.UseRelayServerImplementation) return game;

        var relay = await RelayCommunicator.ReserveRelayInstance(creator, game.ReplicatedGameData.mGameProtocolVersionString);
        if (relay.Port == 0) return game;
        game.relayed = true;
        game.ReplicatedGameData.mHostNetworkAddressList.Add(new NetworkAddress
        {
            IpAddress = new IpAddress { mIp = relay.Ip, mPort = relay.Port }
        });
        return game;
    }

    private ServerGame(ServerPlayer host, StartMatchmakingRequest request)
    {
        var gameId = Program.Database.GetNextGameId();

        ReplicatedGameData = new ReplicatedGameData
        {
            mAdminPlayerList = new List<long>
            {
                host.UserIdentification.mAccountId
            },
            mGameAttribs = ToStringDictionary(request.mCriteriaData),
            mSlotCapacities = new List<ushort>
            {
                0, request.mCriteriaData.mGameSizeRulePrefs.mMaxPlayerCapacity
            },
            mEntryCriteriaMap = request.mEntryCriteriaMap,
            mGameId = gameId,
            mGameName = "game" + gameId,
            mGameSettings = request.mGameSettings,
            mGameReportingId = gameId,
            mGameState = GameState.INITIALIZING,
            mHostNetworkAddressList = new List<NetworkAddress>(),
            mTopologyHostSessionId = (uint)host.UserIdentification.mAccountId,
            mIgnoreEntryCriteriaWithInvite = true,
            mMeshAttribs = new SortedDictionary<string, string>(),
            mMaxPlayerCapacity = request.mCriteriaData.mGameSizeRulePrefs.mMaxPlayerCapacity,
            mNetworkQosData = host.ExtendedData.mQosData,
            mNetworkTopology = Program.ZamboniConfig.TopologyOverride < 0 ? request.mNetworkTopology : (GameNetworkTopology)Program.ZamboniConfig.TopologyOverride,
            mPlatformHostInfo = new HostInfo
            {
                mPlayerId = host.UserIdentification.mAccountId,
                mSlotId = 0
            },
            mPingSiteAlias = host.ExtendedData.mBestPingSiteAlias,
            mQueueCapacity = 0,
            mTopologyHostInfo = new HostInfo
            {
                mPlayerId = host.UserIdentification.mAccountId,
                mSlotId = 0
            },
            mUUID = "game" + gameId,
            mVoipNetwork = VoipTopology.VOIP_DISABLED,
            mGameProtocolVersionString = request.mGameProtocolVersionString,
            mXnetNonce = new byte[]
            {
            },
            mSharedSeed = (uint)gameId,
            mXnetSession = new byte[]
            {
            }
        };
        ServerManager.AddServerGame(gameId, this);
    }

    private ServerGame(ServerPlayer host, CreateGameRequest request)
    {
        var gameId = Program.Database.GetNextGameId();

        ReplicatedGameData = new ReplicatedGameData
        {
            mAdminPlayerList = new List<long>
            {
                host.UserIdentification.mAccountId
            },
            mEntryCriteriaMap = request.mEntryCriteriaMap,
            mGameAttribs = request.mGameAttribs,
            mGameId = gameId,
            mGameName = "game" + gameId,
            mGameProtocolVersionHash = GetGameProtocolVersionHash(request.mGameProtocolVersionString),
            mGameProtocolVersionString = request.mGameProtocolVersionString,
            mGameReportingId = gameId,
            mGameSettings = request.mGameSettings,
            mGameState = GameState.INITIALIZING,
            mGameTypeName = request.mGameTypeName,
            mHostNetworkAddressList = new List<NetworkAddress>
            {
                // host.ExtendedData.mAddress
            },
            mIgnoreEntryCriteriaWithInvite = request.mIgnoreEntryCriteriaWithInvite,
            mMaxPlayerCapacity = 3,
            mMeshAttribs = request.mMeshAttribs,
            mNetworkQosData = host.ExtendedData.mQosData,
            mNetworkTopology = Program.ZamboniConfig.TopologyOverride < 0 ? request.mNetworkTopology : (GameNetworkTopology)Program.ZamboniConfig.TopologyOverride,
            mPersistedGameId = gameId.ToString(),
            mPersistedGameIdSecret = request.mPersistedGameIdSecret,
            mPingSiteAlias = host.ExtendedData.mBestPingSiteAlias,
            mPlatformHostInfo = new HostInfo
            {
                mPlayerId = host.UserIdentification.mAccountId,
                mSlotId = 0
            },
            mTopologyHostSessionId = (uint)host.UserIdentification.mAccountId,
            mPresenceMode = request.mPresenceMode,
            mQueueCapacity = 3,
            mServerNotResetable = request.mServerNotResetable,
            mSharedSeed = (uint)gameId,
            mSlotCapacities = new List<ushort>
            {
                3, 0
            },
            mTeamCapacity = 65535,
            mTeamIds = new List<ushort>
            {
                65535, 65535
            },
            mTopologyHostInfo = new HostInfo
            {
                mPlayerId = host.UserIdentification.mAccountId,
                mSlotId = 0
            },
            mUUID = gameId.ToString(),
            mVoipNetwork = VoipTopology.VOIP_DISABLED,
            mXnetNonce = new byte[]
            {
            },
            mXnetSession = new byte[]
            {
            }
        };
        ServerManager.AddServerGame(gameId, this);
    }

    public async Task AddGameParticipant(ServerPlayer serverPlayer, uint matchmakingSessionId = 0)
    {
        ServerPlayers.TryAdd(serverPlayer.UserIdentification.mAccountId, serverPlayer);
        ReplicatedGamePlayer replicatedGamePlayer;
        if (relayed)
        {
            await RelayCommunicator.AllowFrom(Util.GetUIntAsIPAddress(ReplicatedGameData.mHostNetworkAddressList[0].IpAddress.Value.mIp), ReplicatedGameData.mHostNetworkAddressList[0].IpAddress.Value.mPort, Util.GetUIntAsIPAddress(serverPlayer.ExtendedData.mAddress.IpPairAddress.Value.mExternalAddress.mIp));
            replicatedGamePlayer = serverPlayer.ToReplicatedGamePlayer((byte)(ServerPlayers.Count - 1), ReplicatedGameData.mGameId, new NetworkAddress
            {
                IpAddress = new IpAddress
                {
                    mIp = ReplicatedGameData.mHostNetworkAddressList[0].IpAddress.Value.mIp,
                    mPort = ReplicatedGameData.mHostNetworkAddressList[0].IpAddress.Value.mPort
                    // mInternalAddress = serverPlayer.ExtendedData.mAddress.IpPairAddress.Value.mInternalAddress
                },
            });
        }
        else
        {
            replicatedGamePlayer = serverPlayer.ToReplicatedGamePlayer((byte)(ServerPlayers.Count - 1), ReplicatedGameData.mGameId);
        }

        ReplicatedGamePlayers.TryAdd(serverPlayer.UserIdentification.mAccountId, replicatedGamePlayer);

        if (ServerPlayers.Count == 1)
        {
            GameManagerBase.Server.NotifyGameSetupAsync(serverPlayer.BlazeServerConnection, new NotifyGameSetup
            {
                mGameData = ReplicatedGameData,
                mGameRoster = ReplicatedGamePlayers.Values.ToList(),
                mGameSetupReason = new GameSetupReason
                {
                    MatchmakingSetupContext = new MatchmakingSetupContext
                    {
                        mFitScore = 10,
                        mMatchmakingResult = MatchmakingResult.SUCCESS_CREATED_GAME,
                        mMaxPossibleFitScore = 10,
                        mSessionId = matchmakingSessionId,
                        mUserSessionId = 0
                    }
                }
            }, true);
            GameManagerBase.Server.NotifySelectedAsHostAsync(serverPlayer.BlazeServerConnection, new NotifySelectedAsHost
            {
                mGameId = (uint)ReplicatedGameData.mGameId
            }, true);
        }
        else
        {
            GameManagerBase.Server.NotifyGameSetupAsync(serverPlayer.BlazeServerConnection, new NotifyGameSetup
            {
                mGameData = ReplicatedGameData,
                mGameRoster = ReplicatedGamePlayers.Values.ToList(),
                mGameSetupReason = new GameSetupReason
                {
                    MatchmakingSetupContext = new MatchmakingSetupContext
                    {
                        mFitScore = 10,
                        mMatchmakingResult = MatchmakingResult.SUCCESS_CREATED_GAME,
                        mMaxPossibleFitScore = 010,
                        mSessionId = matchmakingSessionId,
                        mUserSessionId = 0
                    }
                }
            }, true);
        }

        ServerPlayers.Values.ToList().ForEach(participant => GameManagerBase.Server.NotifyPlayerJoiningAsync(participant.BlazeServerConnection, new NotifyPlayerJoining
        {
            mGameId = ReplicatedGameData.mGameId,
            mJoiningPlayer = replicatedGamePlayer
        }, true));
    }

    public bool HasSpaceForPlayer()
    {
        return ReplicatedGameData.mSlotCapacities.Sum(x => x) > ReplicatedGamePlayers.Count;
    }

    public void RemoveGameParticipant(ServerPlayer serverPlayer, PlayerRemovedReason reason)
    {
        ServerPlayers.TryRemove(serverPlayer.UserIdentification.mAccountId, out _);
        ReplicatedGamePlayers.Remove(serverPlayer.UserIdentification.mAccountId, out _);

        ServerPlayers.Values.ToList().ForEach(participant => GameManagerBase.Server.NotifyPlayerRemovedAsync(participant.BlazeServerConnection, new NotifyPlayerRemoved
        {
            mPlayerRemovedTitleContext = 0,
            mGameId = ReplicatedGameData.mGameId,
            mPlayerId = serverPlayer.UserIdentification.mBlazeId,
            mPlayerRemovedReason = reason
        }));
    }

    public async Task RemoveGame()
    {
        GameManager.StaleGames.Enqueue(ReplicatedGameData.mGameId);

        while (GameManager.StaleGames.Count > 20)
        {
            GameManager.StaleGames.TryDequeue(out _);
        }

        if (relayed)
        {
            await RelayCommunicator.DestroyRelayInstance(this);
        }

        ServerPlayers.Values.ToList().ForEach(participant => GameManagerBase.Server.NotifyPlayerRemovedAsync(participant.BlazeServerConnection, new NotifyPlayerRemoved
        {
            mGameId = ReplicatedGameData.mGameId,
            mPlayerId = participant.UserIdentification.mBlazeId,
            mPlayerRemovedReason = PlayerRemovedReason.GAME_DESTROYED,
            mPlayerRemovedTitleContext = 0
        }, true));
        ServerPlayers.Values.ToList().ForEach(participant => GameManagerBase.Server.NotifyGameRemovedAsync(participant.BlazeServerConnection, new NotifyGameRemoved()
        {
            mGameId = ReplicatedGameData.mGameId,
            mDestructionReason = GameDestructionReason.SYS_GAME_ENDING
        }, true));
        ServerManager.RemoveServerGame(ReplicatedGameData.mGameId);
    }

    private static SortedDictionary<string, string> ToStringDictionary(MatchmakingCriteriaData matchmakingCriteriaData)
    {
        SortedDictionary<string, string> returningList = new();
        foreach (var variable in matchmakingCriteriaData.mGenericRulePrefsList)
        {
            returningList.Add(variable.mRuleName, variable.mDesiredValues[0]);
        }

        return returningList;
    }

    public static ulong GetGameProtocolVersionHash(string protocolVersion)
    {
        protocolVersion ??= string.Empty;
        //FNV1 HASH - the same hashing logic is used in ea blaze for game protocol versions
        var buf = Encoding.UTF8.GetBytes(protocolVersion);
        var hash = 2166136261UL;
        foreach (var c in buf)
            hash = (hash * 16777619) ^ c;
        return hash;
    }

    public override string ToString()
    {
        return "Players: " +
               string.Join(", ", ServerPlayers.Values.Select(serverPlayer => serverPlayer.UserIdentification.mName)) +
               " gameId:" + ReplicatedGameData.mGameId +
               " state: " + ReplicatedGameData.mGameState +
               " OSDK_gameMode: " + ReplicatedGameData.mGameAttribs["OSDK_gameMode"] +
               " Relayed: " + relayed;
    }
}